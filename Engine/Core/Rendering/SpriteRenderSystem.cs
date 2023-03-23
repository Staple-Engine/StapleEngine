using Bgfx;
using Staple.Internal;
using System;
using System.Collections.Generic;
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
        /// Vertex data
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        struct SpriteVertex
        {
            public Vector3 position;
            public Vector2 texCoord;
        }

        /// <summary>
        /// Contains render information for a sprite
        /// </summary>
        private class SpriteRenderInfo
        {
            public Matrix4x4 transform;
            public Material material;
            public Color color;
            public Texture texture;
            public ushort viewID;
        }

        /// <summary>
        /// The vertices for a normal quad sprite
        /// </summary>
        private static SpriteVertex[] vertices = new SpriteVertex[]
        {
            new SpriteVertex() {
                position = new Vector3(-0.5f, -0.5f, 0),
                texCoord = Vector2.Zero,
            },
            new SpriteVertex() {
                position = new Vector3(-0.5f, 0.5f, 0),
                texCoord = new Vector2(0, 1),
            },
            new SpriteVertex() {
                position = new Vector3(0.5f, 0.5f, 0),
                texCoord = Vector2.One,
            },
            new SpriteVertex() {
                position = new Vector3(0.5f, -0.5f, 0),
                texCoord = new Vector2(1, 0),
            },
        };

        /// <summary>
        /// The indices for a normal quad sprite
        /// </summary>
        private static ushort[] indices = new ushort[]
        {
            0, 1, 2, 2, 3, 0
        };

        private VertexLayout vertexLayout;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;

        private List<SpriteRenderInfo> sprites = new List<SpriteRenderInfo>();

        public void Destroy()
        {
            vertexBuffer?.Destroy();
            indexBuffer?.Destroy();
        }

        public Type RelatedComponent()
        {
            return typeof(Sprite);
        }

        public void Prepare()
        {
            sprites.Clear();
        }

        public void Preprocess(Entity entity, Transform transform, IComponent renderer)
        {
            var r = renderer as Sprite;

            //We recalculate the bounds of this sprite
            if(r.texture != null && r.material != null && r.material.shader != null)
            {
                r.localBounds = new AABB(Vector3.Zero, new Vector3(r.texture.SpriteWidth, r.texture.SpriteHeight, 0));

                r.bounds = new AABB(transform.Position, new Vector3(r.texture.SpriteWidth, r.texture.SpriteHeight, 0));
            }
        }

        public void Process(Entity entity, Transform transform, IComponent renderer, ushort viewId)
        {
            var r = renderer as Sprite;

            if(r.material == null || r.material.shader == null || r.material.Disposed || r.material.shader.Disposed)
            {
                return;
            }

            var scale = Vector3.Zero;

            if (r.texture != null)
            {
                scale.X = r.texture.SpriteWidth;
                scale.Y = r.texture.SpriteHeight;
            }

            var matrix = Matrix4x4.CreateScale(scale) * transform.Matrix;

            sprites.Add(new SpriteRenderInfo()
            {
                color = r.color,
                material = r.material,
                texture = r.texture,
                transform = matrix,
                viewID = viewId
            });
        }

        public void Submit()
        {
            if (vertexLayout == null)
            {
                vertexLayout = new VertexLayoutBuilder()
                    .Add(bgfx.Attrib.Position, 3, bgfx.AttribType.Float)
                    .Add(bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float)
                    .Build();

                vertexBuffer = VertexBuffer.Create(vertices, vertexLayout);

                indexBuffer = IndexBuffer.Create(indices, RenderBufferFlags.None);
            }

            if(sprites.Count == 0)
            {
                return;
            }

            vertexBuffer.SetActive(0, 0, (uint)vertexBuffer.length);
            indexBuffer.SetActive(0, (uint)indexBuffer.length);

            bgfx.StateFlags state = bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA | bgfx.StateFlags.DepthTestGequal | bgfx.StateFlags.PtTristrip;

            for (var i = 0; i < sprites.Count; i++)
            {
                var s = sprites[i];

                unsafe
                {
                    var transform = s.transform;

                    bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)state, 0);

                s.material.ApplyProperties();

                s.material.shader.SetColor(Material.MainColorProperty, s.color);

                if (s.texture != null)
                {
                    s.material.shader.SetTexture(Material.MainTextureProperty, s.texture);
                }

                var discardFlags = i == sprites.Count - 1 ? bgfx.DiscardFlags.All : bgfx.DiscardFlags.Transform | bgfx.DiscardFlags.Bindings | bgfx.DiscardFlags.State;

                bgfx.submit(s.viewID, s.material.shader.program, 0, (byte)discardFlags);
            }
        }
    }
}
