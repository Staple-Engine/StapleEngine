using Bgfx;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple
{
    /// <summary>
    /// Sprite Render System
    /// </summary>
    internal class SpriteRenderSystem : IRenderSystem
    {
        /// <summary>
        /// Contains render information for a sprite
        /// </summary>
        private class SpriteRenderInfo
        {
            public Matrix4x4 transform;
            public Material material;
            public Color color;
            public Texture texture;
            public Rect textureRect;
            public ushort viewID;
            public int sortingOrder;
            public uint layer;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        struct SpriteVertex
        {
            public Vector3 position;
            public Vector2 uv;
        }

        private static VertexLayout vertexLayout;

        private static readonly Vector3[] vertices = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
        };

        private static readonly ushort[] indices = new ushort[]
        {
            0, 1, 2, 2, 3, 0
        };

        private static readonly SpriteVertex[] spriteVertices = new SpriteVertex[4]
        {
            new SpriteVertex() { position = vertices[0] },
            new SpriteVertex() { position = vertices[1] },
            new SpriteVertex() { position = vertices[2] },
            new SpriteVertex() { position = vertices[3] },
        };

        /// <summary>
        /// Contains a list of all sprites queued for rendering
        /// </summary>
        private readonly List<SpriteRenderInfo> sprites = new();

        public void Destroy()
        {
        }

        public Type RelatedComponent()
        {
            return typeof(SpriteRenderer);
        }

        public void Prepare()
        {
            sprites.Clear();
        }

        public void Preprocess(World world, Entity entity, Transform transform, IComponent renderer)
        {
            var r = renderer as SpriteRenderer;

            //We recalculate the bounds of this sprite
            if (r.texture != null &&
                r.texture.Disposed == false &&
                r.material != null &&
                r.material.Disposed == false &&
                r.material.shader != null &&
                r.material.Disposed == false &&
                r.spriteIndex >= 0 &&
                r.spriteIndex < r.texture.metadata.sprites.Count)
            {
                var sprite = r.texture.metadata.sprites[r.spriteIndex];

                var size = new Vector3(sprite.rect.Width * r.texture.SpriteScale, sprite.rect.Height * r.texture.SpriteScale, 0);

                r.localBounds = new AABB(Vector3.Zero, size);

                r.bounds = new AABB(transform.Position, size);
            }
        }

        public void Process(World world, Entity entity, Transform transform, IComponent renderer, ushort viewId)
        {
            var r = renderer as SpriteRenderer;

            if (r.texture == null ||
                r.texture.Disposed ||
                r.material == null ||
                r.material.shader == null ||
                r.material.Disposed ||
                r.material.shader.Disposed ||
                r.spriteIndex < 0 ||
                r.spriteIndex >= r.texture.metadata.sprites.Count)
            {
                return;
            }

            var sprite = r.texture.metadata.sprites[r.spriteIndex];

            var scale = Vector3.Zero;

            if (r.texture != null)
            {
                scale.X = sprite.rect.Width * r.texture.SpriteScale;
                scale.Y = sprite.rect.Height * r.texture.SpriteScale;
            }

            switch(sprite.rotation)
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

            var matrix = Matrix4x4.CreateScale(scale) * transform.Matrix;

            sprites.Add(new SpriteRenderInfo()
            {
                color = r.color,
                material = r.material,
                texture = r.texture,
                textureRect = sprite.rect,
                transform = matrix,
                viewID = viewId,
                sortingOrder = r.sortingOrder,
                layer = r.sortingLayer,
            });
        }

        public void Submit()
        {
            if (sprites.Count == 0)
            {
                return;
            }

            vertexLayout ??= new VertexLayoutBuilder()
                .Add(bgfx.Attrib.Position, 3, bgfx.AttribType.Float)
                .Add(bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float)
                .Build();

            var state = bgfx.StateFlags.WriteRgb |
                bgfx.StateFlags.WriteA |
                bgfx.StateFlags.DepthTestLequal;

            var orderedSprites = sprites
                .OrderBy(x => x.layer)
                .ThenBy(x => x.sortingOrder)
                .ToList();

            for (var i = 0; i < orderedSprites.Count; i++)
            {
                var s = orderedSprites[i];

                spriteVertices[0].uv.X = s.textureRect.left / (float)s.texture.Width;
                spriteVertices[0].uv.Y = s.textureRect.top / (float)s.texture.Height;

                spriteVertices[1].uv.X = s.textureRect.left / (float)s.texture.Width;
                spriteVertices[1].uv.Y = s.textureRect.bottom / (float)s.texture.Height;

                spriteVertices[2].uv.X = s.textureRect.right / (float)s.texture.Width;
                spriteVertices[2].uv.Y = s.textureRect.bottom / (float)s.texture.Height;

                spriteVertices[3].uv.X = s.textureRect.right / (float)s.texture.Width;
                spriteVertices[3].uv.Y = s.textureRect.top / (float)s.texture.Height;

                var vertexBuffer = VertexBuffer.Create(spriteVertices.AsSpan(), vertexLayout, true);

                var indexBuffer = IndexBuffer.Create(indices, RenderBufferFlags.None, true);

                if (vertexBuffer == null || indexBuffer == null)
                {
                    continue;
                }

                vertexBuffer.SetActive(0, 0, 4);
                indexBuffer.SetActive(0, 6);

                unsafe
                {
                    var transform = s.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)(state | s.material.shader.BlendingFlag()), 0);

                s.material.shader.SetColor(Material.MainColorProperty, s.color);
                s.material.shader.SetTexture(Material.MainTextureProperty, s.texture);

                bgfx.submit(s.viewID, s.material.shader.program, 0, (byte)bgfx.DiscardFlags.All);
            }
        }
    }
}
