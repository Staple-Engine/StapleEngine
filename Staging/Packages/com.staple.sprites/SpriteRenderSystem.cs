using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Sprite Render System
/// </summary>
public class SpriteRenderSystem : IRenderSystem
{
    public const int NinePatchVertexCount = 54;

    private const int MaxNinePatchCacheFrames = 10;

    /// <summary>
    /// Contains render information for a sprite
    /// </summary>
    private struct SpriteRenderInfo
    {
        public Transform transform;
        public Vector3 scale;
        public Material material;
        public Color color;
        public Texture texture;
        public Rect textureRect;
        public int sortingOrder;
        public uint layer;
        public SpriteRenderMode renderMode;
        public Rect border;
        public Vector3 localScale;
    }

    /// <summary>
    /// Contains the geometry for a nine patch sprite
    /// </summary>
    private class NinePatchCacheItem
    {
        public SpriteVertex[] vertices;
        public ushort[] indices;
        public int framesSinceUse;
    }

    /// <summary>
    /// A vertex structure for a sprite vertex
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct SpriteVertex
    {
        public Vector3 position;
        public Vector2 uv;
    }

    internal static Lazy<VertexLayout> vertexLayout = new(() =>
    {
        return VertexLayoutBuilder.CreateNew()
            .Add(VertexAttribute.Position, VertexAttributeType.Float3)
            .Add(VertexAttribute.TexCoord0, VertexAttributeType.Float2)
            .Build();
    });

    private static readonly Vector3[] vertices =
    [
        new Vector3(-0.5f, -0.5f, 0),
        new Vector3(-0.5f, 0.5f, 0),
        new Vector3(0.5f, 0.5f, 0),
        new Vector3(0.5f, -0.5f, 0),
    ];

    private static readonly ushort[] indices =
    [
        0, 1, 2, 2, 3, 0
    ];

    private static readonly SpriteVertex[] spriteVertices =
    [
        new SpriteVertex() { position = vertices[0] },
        new SpriteVertex() { position = vertices[1] },
        new SpriteVertex() { position = vertices[2] },
        new SpriteVertex() { position = vertices[3] },
    ];

    /// <summary>
    /// Contains a list of all sprites queued for rendering
    /// </summary>
    private readonly List<SpriteRenderInfo> sprites = [];

    /// <summary>
    /// Contains a list of all nine patch sprites' geometry data
    /// Key: HashCode for the sprite texture Guid and the size of the sprite
    /// </summary>
    private readonly Dictionary<int, NinePatchCacheItem> cachedNinePatchGeometries = [];

    /// <summary>
    /// List of nine patch cache items to remove this frame
    /// </summary>
    private readonly HashSet<int> ninePatchItemsToRemove = [];

    /// <summary>
    /// List of materials we can modify at will
    /// </summary>
    private readonly Dictionary<int, Material> mutableMaterials = [];

    public bool UsesOwnRenderProcess => false;

    public Type RelatedComponent => typeof(SpriteRenderer);

    /// <summary>
    /// Calculates nine patch geometry for a part of the sprite 
    /// </summary>
    /// <param name="textureSize">The size of the texture</param>
    /// <param name="position">The position in the texture space</param>
    /// <param name="size">The size of the area we're generating</param>
    /// <param name="uvSize">The size of the area in UV coordinates</param>
    /// <param name="offset">The 3D offset for the piece</param>
    /// <param name="sizeOverride">If we're overriding the size, we can do so here</param>
    /// <param name="vertices">The vertices to fill in</param>
    internal static void MakeNinePatchGeometrySlice(Vector2 textureSize, Vector2 position, Vector2 size, Vector2 uvSize, Vector2 offset, Vector2 sizeOverride, Span<SpriteVertex> vertices)
    {
        if(vertices.IsEmpty ||
            vertices.Length != 6)
        {
            return;
        }

        var rect = new RectFloat(position.X, position.X + uvSize.X, position.Y, position.Y + uvSize.Y);

        var actualSize = sizeOverride.X != -1 ? sizeOverride : size;

        vertices[0].position = vertices[5].position = new Vector3(offset.X, offset.Y + actualSize.Y, 0);
        vertices[1].position = new Vector3(offset, 0);
        vertices[2].position = vertices[3].position = new Vector3(offset.X + actualSize.X, offset.Y, 0);
        vertices[4].position = new Vector3(offset + actualSize, 0);

        vertices[0].uv = vertices[5].uv = rect.Position / textureSize;
        vertices[1].uv = new Vector2(rect.left, rect.bottom) / textureSize;
        vertices[2].uv = vertices[3].uv = new Vector2(rect.right, rect.bottom) / textureSize;
        vertices[4].uv = new Vector2(rect.right, rect.top) / textureSize;
    }

    /// <summary>
    /// Calculates nine patch geometry for a sprite
    /// </summary>
    /// <param name="vertices">The vertices to update. They should have <see cref="NinePatchVertexCount"/> elements</param>
    /// <param name="indices">The indices to update. They should have <see cref="NinePatchVertexCount"/> elements</param>
    /// <param name="texture">The texture to use</param>
    /// <param name="size">The size of the sprite in world space</param>
    /// <param name="border">The nine patch border, in pixels</param>
    /// <param name="pixelCoordinates">Whether we're using world or pixel coordinates</param>
    internal static void MakeNinePatchGeometry(Span<SpriteVertex> vertices, Span<ushort> indices, Texture texture, Vector2 size,
        Rect border, bool pixelCoordinates)
    {
        if((texture?.Disposed ?? true) ||
            vertices.IsEmpty ||
            vertices.Length != NinePatchVertexCount ||
            indices.IsEmpty ||
            indices.Length != NinePatchVertexCount)
        {
            return;
        }

        var textureSize = texture.Size;
        var localScale = Vector2.Abs(size);
        var invertedLocal = new Vector2(1 / localScale.X, 1 / localScale.Y);

        if(pixelCoordinates)
        {
            invertedLocal = Vector2.One;
        }

        var fragmentPositions = new Vector2[9]
        {
            Vector2.Zero,
            new(textureSize.X - border.right, 0),
            new(0, textureSize.Y - border.bottom),
            new(textureSize.X - border.right, textureSize.Y - border.bottom),
            new(border.left, border.top),
            new(border.left, 0),
            new(border.left, textureSize.Y - border.bottom),
            new(0, border.top),
            new(textureSize.X - border.right, border.top),
        };

        var fragmentSizes = new Vector2[9]
        {
            new(border.left, border.top),
            new(border.right, border.top),
            new(border.left, border.bottom),
            new(border.right, border.bottom),
            new(textureSize.X - border.left - border.right, textureSize.Y - border.top - border.bottom),
            new(textureSize.X - border.left - border.right, border.top),
            new(textureSize.X - border.left - border.right, border.bottom),
            new(border.left, textureSize.Y - border.top - border.bottom),
            new(border.right, textureSize.Y - border.top - border.bottom),
        };

        var fragmentOffsets = new Vector2[9]
        {
            new(-border.left * invertedLocal.X, localScale.Y),
            localScale,
            new(-border.left * invertedLocal.X, -border.top * invertedLocal.Y),
            new(localScale.X, -border.top * invertedLocal.Y),
            Vector2.Zero,
            new(0, localScale.Y),
            new(0, -border.top * invertedLocal.Y),
            new(-border.left * invertedLocal.X, 0),
            new(localScale.X, 0),
        };

        var fragmentSizeOverrides = new Vector2[9]
        {
            new(border.left * invertedLocal.X, border.bottom * invertedLocal.Y),
            new(border.right * invertedLocal.X, border.bottom * invertedLocal.Y),
            new(border.left * invertedLocal.X, border.top * invertedLocal.Y),
            new(border.right * invertedLocal.X, border.top * invertedLocal.Y),
            localScale,
            new(localScale.X, border.bottom * invertedLocal.Y),
            new(localScale.X, border.top * invertedLocal.Y),
            new(border.left * invertedLocal.X, localScale.Y),
            new(border.right * invertedLocal.X, localScale.Y),
        };

        for (int j = 0, index = 0; j < 9; j++, index += 6)
        {
            var position = fragmentPositions[j];
            var fragmentSize = fragmentSizes[j];
            var offset = fragmentOffsets[j];
            var sizeOverride = fragmentSizeOverrides[j];

            MakeNinePatchGeometrySlice(textureSize, position, fragmentSize * texture.SpriteScale, fragmentSize, offset, sizeOverride,
                vertices.Slice(index, 6));
        }

        for (var j = 0; j < indices.Length; j++)
        {
            indices[j] = (ushort)j;
        }
    }

    public void Startup()
    {
    }

    public void Shutdown()
    {
        cachedNinePatchGeometries.Clear();
    }

    public void Prepare()
    {
        sprites.Clear();
    }

    public void Preprocess(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var (_, transform, relatedComponent) in entities)
        {
            var r = relatedComponent as SpriteRenderer;

            var hasValidAnimation = r.animation != null &&
                r.animation.texture != null &&
                r.animation.texture.Disposed == false &&
                r.animation.frames.Count > 0;

            var hasValidTexture = (r.sprite?.IsValid ?? false) ||
                hasValidAnimation;

            if (hasValidTexture == false ||
                (r.material?.IsValid ?? false) == false)
            {
                continue;
            }

            Sprite sprite = null;

            if (hasValidAnimation)
            {
                if (Platform.IsPlaying)
                {
                    r.timer += Time.deltaTime;

                    var timeStep = r.animation.frameRateIsMilliseconds ? 1000.0f / r.animation.frameRate : 1000.0f / r.animation.frameRate / 1000.0f;

                    while (r.timer >= timeStep && timeStep > 0)
                    {
                        r.timer -= timeStep;

                        r.currentFrame++;

                        if (r.currentFrame >= r.animation.frames.Count)
                        {
                            r.currentFrame = 0;
                        }
                    }
                }

                if (r.currentFrame < 0 || r.currentFrame >= r.animation.frames.Count)
                {
                    continue;
                }

                var frame = r.animation.frames[r.currentFrame];

                if (frame < 0 || frame >= r.animation.texture.Sprites.Length)
                {
                    continue;
                }

                sprite = (r.animation.texture?.Sprites?.Length ?? 0) > 0 && frame < r.animation.texture.Sprites.Length ?
                    r.animation.texture.Sprites[frame] : null;
            }
            else
            {
                sprite = r.sprite;
            }

            if(sprite == null)
            {
                return;
            }

            var spriteSize = r.renderMode switch
            {
                SpriteRenderMode.Sliced => transform.LocalScale.ToVector2(),
                _ => (Vector2)sprite.Rect.Size,
            };

            var size = new Vector3(spriteSize * sprite.texture.SpriteScale, 0);

            r.localBounds = new AABB(Vector3.Zero, size);

            r.bounds = new AABB(transform.Position, size);
        }
    }

    public void Process(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var (_, transform, relatedComponent) in entities)
        {
            var r = relatedComponent as SpriteRenderer;

            if(r.isVisible == false)
            {
                continue;
            }

            var hasValidAnimation = r.animation != null &&
                r.animation.texture != null &&
                r.animation.texture.Disposed == false &&
                r.animation.frames.Count > 0;

            var hasValidTexture = (r.sprite?.IsValid ?? false) ||
                hasValidAnimation;

            if (hasValidTexture == false ||
                (r.material?.IsValid ?? false) == false)
            {
                continue;
            }

            Sprite sprite = null;

            if (hasValidAnimation)
            {
                if (r.currentFrame < 0 || r.currentFrame >= r.animation.frames.Count)
                {
                    continue;
                }

                var frame = r.animation.frames[r.currentFrame];

                if (frame < 0 || frame >= r.animation.texture.Sprites.Length)
                {
                    continue;
                }

                sprite = (r.animation.texture?.Sprites?.Length ?? 0) > 0 && frame < r.animation.texture.Sprites.Length ?
                    r.animation.texture.Sprites[frame] : null;
            }
            else
            {
                sprite = r.sprite;
            }

            if (sprite == null)
            {
                return;
            }

            var spriteSize = r.renderMode switch
            {
                SpriteRenderMode.Sliced => Vector2.One,
                _ => (Vector2)sprite.Rect.Size,
            };

            var scale = Vector3.Zero;

            if (sprite != null)
            {
                scale = new(spriteSize * sprite.texture.SpriteScale, 1);
            }

            var localScale = r.renderMode switch
            {
                SpriteRenderMode.Sliced => transform.LocalScale,
                _ => scale,
            };

            switch (sprite.Rotation)
            {
                case TextureSpriteRotation.FlipY:

                    scale.Y *= -1;

                    break;

                case TextureSpriteRotation.FlipX:

                    scale.X *= -1;

                    break;
            }

            if (r.flipX)
            {
                scale.X *= -1;
            }

            if (r.flipY)
            {
                scale.Y *= -1;
            }

            if(mutableMaterials.TryGetValue(r.material.Guid.GuidHash, out var mutableMaterial) == false)
            {
                mutableMaterial = new(r.material);

                mutableMaterial.DisableShaderKeyword(Shader.SkinningKeyword);
                mutableMaterial.DisableShaderKeyword(Shader.InstancingKeyword);

                mutableMaterials.Add(r.material.Guid.GuidHash, mutableMaterial);
            }

            sprites.Add(new SpriteRenderInfo()
            {
                color = r.color,
                material = mutableMaterial,
                texture = sprite.texture,
                textureRect = sprite.Rect,
                transform = transform,
                scale = scale,
                sortingOrder = r.sortingOrder,
                layer = r.sortingLayer,
                border = sprite.Border,
                renderMode = r.renderMode,
                localScale = localScale,
            });
        }
    }

    public void Submit()
    {
        var orderedSprites = sprites
            .OrderBy(x => x.layer)
            .ThenBy(x => x.sortingOrder)
            .ToList();

        foreach(var pair in cachedNinePatchGeometries)
        {
            if(pair.Value is NinePatchCacheItem item)
            {
                item.framesSinceUse++;

                if(item.framesSinceUse >= MaxNinePatchCacheFrames)
                {
                    ninePatchItemsToRemove.Add(pair.Key);
                }
            }
        }

        if(ninePatchItemsToRemove.Count > 0)
        {
            foreach(var key in ninePatchItemsToRemove)
            {
                cachedNinePatchGeometries.Remove(key);
            }

            ninePatchItemsToRemove.Clear();
        }

        for (var i = 0; i < orderedSprites.Count; i++)
        {
            var s = orderedSprites[i];

            var vertexCount = 4;
            var indexCount = 6;
            var vertices = spriteVertices;
            var indices = SpriteRenderSystem.indices;

            switch(s.renderMode)
            {
                case SpriteRenderMode.Sliced:

                    {
                        var key = HashCode.Combine(s.texture.Guid.GuidHash, s.border, s.localScale);

                        if (cachedNinePatchGeometries.TryGetValue(key, out var cache) == false)
                        {
                            cache = new()
                            {
                                vertices = new SpriteVertex[NinePatchVertexCount],
                                indices = new ushort[NinePatchVertexCount],
                            };

                            MakeNinePatchGeometry(cache.vertices, cache.indices, s.texture, s.localScale.ToVector2(), s.border, false);

                            cachedNinePatchGeometries.Add(key, cache);
                        }

                        cache.framesSinceUse = 0;

                        vertices = cache.vertices;
                        indices = cache.indices;
                        vertexCount = cache.vertices.Length;
                        indexCount = cache.indices.Length;
                    }

                    break;

                case SpriteRenderMode.Normal:

                    {
                        spriteVertices[0].uv.X = s.textureRect.left / (float)s.texture.Width;
                        spriteVertices[0].uv.Y = s.textureRect.bottom / (float)s.texture.Height;

                        spriteVertices[1].uv.X = s.textureRect.left / (float)s.texture.Width;
                        spriteVertices[1].uv.Y = s.textureRect.top / (float)s.texture.Height;

                        spriteVertices[2].uv.X = s.textureRect.right / (float)s.texture.Width;
                        spriteVertices[2].uv.Y = s.textureRect.top / (float)s.texture.Height;

                        spriteVertices[3].uv.X = s.textureRect.right / (float)s.texture.Width;
                        spriteVertices[3].uv.Y = s.textureRect.bottom / (float)s.texture.Height;
                    }

                    break;
            }

            s.material.MainColor = s.color;
            s.material.MainTexture = s.texture;

            Graphics.RenderSimple(vertices, vertexLayout.Value, indices, s.material, Vector3.Zero,
                Matrix4x4.CreateScale(s.scale) * s.transform.Matrix, MeshTopology.Triangles, MaterialLighting.Unlit);
        }
    }
}
