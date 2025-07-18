﻿using Bgfx;
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
        public uint[] indices;
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
        return new VertexLayoutBuilder()
            .Add(VertexAttribute.Position, 3, VertexAttributeType.Float)
            .Add(VertexAttribute.TexCoord0, 2, VertexAttributeType.Float)
            .Build();
    });

    /// <summary>
    /// Gets the default material for sprites
    /// </summary>
    public static Lazy<Material> DefaultMaterial = new(() =>
    {
        var material = ResourceManager.instance.LoadMaterial($"Hidden/Materials/Sprite.{AssetSerialization.MaterialExtension}");

        if (material != null)
        {
            ResourceManager.instance.LockAsset(material.Guid.Guid);

            if (material.MainTexture is Texture t)
            {
                ResourceManager.instance.LockAsset(t.Guid.Guid);
            }

            if (material.shader is Shader s)
            {
                ResourceManager.instance.LockAsset(s.Guid.Guid);
            }
        }

        return material;
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
    private readonly Dictionary<ushort, List<SpriteRenderInfo>> sprites = [];

    /// <summary>
    /// Contains a list of all nine patch sprites' geometry data
    /// Key: HashCode for the sprite texture Guid and the size of the sprite
    /// </summary>
    private readonly Dictionary<int, NinePatchCacheItem> cachedNinePatchGeometries = [];

    /// <summary>
    /// List of nine patch cache items to remove this frame
    /// </summary>
    private readonly HashSet<int> ninePatchItemsToRemove = [];

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
    internal static void MakeNinePatchGeometry(Span<SpriteVertex> vertices, Span<uint> indices, Texture texture, Vector2 size, Rect border, bool pixelCoordinates)
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
            new(border.left * invertedLocal.X, border.top * invertedLocal.Y),
            new(border.right * invertedLocal.X, border.top * invertedLocal.Y),
            new(border.left * invertedLocal.X, border.bottom * invertedLocal.Y),
            new(border.right * invertedLocal.X, border.bottom * invertedLocal.Y),
            localScale,
            new(localScale.X, border.top * invertedLocal.Y),
            new(localScale.X, border.bottom * invertedLocal.Y),
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
            indices[j] = (uint)j;
        }
    }

    public void Startup()
    {
    }

    public void Shutdown()
    {
        cachedNinePatchGeometries.Clear();
    }

    public void ClearRenderData(ushort viewID)
    {
        sprites.Remove(viewID);
    }

    public void Prepare()
    {
        sprites.Clear();
    }

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
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
                r.material == null ||
                r.material.shader == null ||
                r.material.Disposed ||
                r.material.shader.Disposed)
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

                if (frame < 0 || frame >= r.animation.texture.metadata.sprites.Count)
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

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
        if(sprites.TryGetValue(viewID, out var container) == false)
        {
            container = [];

            sprites.Add(viewID, container);
        }
        else
        {
            container.Clear();
        }

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
                r.material == null ||
                r.material.shader == null ||
                r.material.Disposed ||
                r.material.shader.Disposed)
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

                if (frame < 0 || frame >= r.animation.texture.metadata.sprites.Count)
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

            container.Add(new SpriteRenderInfo()
            {
                color = r.color,
                material = r.material,
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

    public void Submit(ushort viewID)
    {
        if(sprites.TryGetValue(viewID, out var container) == false)
        {
            return;
        }

        if (container.Count == 0)
        {
            return;
        }

        var state = bgfx.StateFlags.WriteRgb |
            bgfx.StateFlags.WriteA |
            bgfx.StateFlags.DepthTestLequal;

        var orderedSprites = container
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

            VertexBuffer vertexBuffer = null;
            IndexBuffer indexBuffer = null;
            uint vertexCount = 4;
            uint indexCount = 6;

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
                                indices = new uint[NinePatchVertexCount],
                            };

                            MakeNinePatchGeometry(cache.vertices, cache.indices, s.texture, s.localScale.ToVector2(), s.border, false);

                            cachedNinePatchGeometries.Add(key, cache);
                        }

                        cache.framesSinceUse = 0;

                        vertexCount = (uint)cache.vertices.Length;
                        indexCount = (uint)cache.indices.Length;

                        vertexBuffer = VertexBuffer.CreateTransient(cache.vertices.AsSpan(), vertexLayout.Value);

                        indexBuffer = IndexBuffer.CreateTransient(cache.indices);
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

                        vertexBuffer = VertexBuffer.CreateTransient(spriteVertices.AsSpan(), vertexLayout.Value);

                        indexBuffer = IndexBuffer.CreateTransient(indices);
                    }

                    break;
            }

            if (vertexBuffer == null || indexBuffer == null)
            {
                continue;
            }

            vertexBuffer.SetActive(0, 0, vertexCount);
            indexBuffer.SetActive(0, indexCount);

            unsafe
            {
                var transform = Matrix4x4.CreateScale(s.scale) * s.transform.Matrix;

                _ = bgfx.set_transform(&transform, 1);
            }

            //Don't use culling for sprites
            bgfx.set_state((ulong)(state | s.material.shader.BlendingFlag), 0);

            s.material.shader.SetColor(s.material.GetShaderHandle(Material.MainColorProperty), s.color);
            s.material.shader.SetTexture(s.material.GetShaderHandle(Material.MainTextureProperty), s.texture);

            s.material.DisableShaderKeyword(Shader.SkinningKeyword);
            s.material.DisableShaderKeyword(Shader.InstancingKeyword);

            var lightSystem = RenderSystem.Instance.Get<LightSystem>();

            lightSystem?.ApplyMaterialLighting(s.material, MaterialLighting.Unlit);

            var program = s.material.ShaderProgram;

            if (program.Valid)
            {
                bgfx.submit(viewID, program, 0, (byte)bgfx.DiscardFlags.All);
            }
            else
            {
                bgfx.discard((byte)bgfx.DiscardFlags.All);
            }
        }
    }
}
