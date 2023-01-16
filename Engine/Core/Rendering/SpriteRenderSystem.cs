using Bgfx;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    internal class SpriteRenderSystem : IRenderSystem
    {
        [StructLayout(LayoutKind.Sequential)]
        struct SpriteVertex
        {
            public Vector3 position;
            public Vector2 texCoord;
        }

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

        private static ushort[] indices = new ushort[]
        {
            0, 1, 2, 2, 3, 0
        };

        private VertexLayout vertexLayout;
        private VertexBuffer vertexBuffer;
        private IndexBuffer indexBuffer;

        public void Destroy()
        {
            vertexBuffer?.Destroy();
            indexBuffer?.Destroy();
        }

        public Type RelatedComponent()
        {
            return typeof(Sprite);
        }

        public void Preprocess(Entity entity, Transform transform, Component renderer)
        {
            var r = renderer as Sprite;

            if(r.texture != null && r.material != null && r.material.shader != null)
            {
                r.localBounds = new AABB(Vector3.Zero, new Vector3(r.texture.SpriteWidth, r.texture.SpriteHeight, 0));

                r.bounds = new AABB(transform.Position, new Vector3(r.texture.SpriteWidth, r.texture.SpriteHeight, 0));
            }
        }

        public void Process(Entity entity, Transform transform, Component renderer, ushort viewId)
        {
            var r = renderer as Sprite;

            if(vertexLayout == null)
            {
                vertexLayout = new VertexLayoutBuilder()
                    .Add(bgfx.Attrib.Position, 3, bgfx.AttribType.Float)
                    .Add(bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float)
                    .Build();

                vertexBuffer = VertexBuffer.Create(vertices, vertexLayout);

                indexBuffer = IndexBuffer.Create(indices, RenderBufferFlags.None);
            }

            if(r.material == null || r.material.shader == null)
            {
                return;
            }

            vertexBuffer.SetActive(0, 0, (uint)vertexBuffer.length);
            indexBuffer.SetActive(0, (uint)indexBuffer.length);

            var scale = Vector3.Zero;

            if (r.texture != null)
            {
                scale.X = r.texture.SpriteWidth;
                scale.Y = r.texture.SpriteHeight;
            }

            var matrix = Matrix4x4.CreateScale(scale) * transform.Matrix;

            unsafe
            {
                bgfx.set_transform(&matrix, 1);
            }

            bgfx.StateFlags state = bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA | bgfx.StateFlags.DepthTestGequal | bgfx.StateFlags.PtTristrip;

            bgfx.set_state((ulong)state, 0);

            r.material.ApplyProperties();

            r.material.shader.SetColor(Material.MainColorProperty, r.color);

            if (r.texture != null)
            {
                r.material.shader.SetTexture(Material.MainTextureProperty, r.texture);
            }

            bgfx.submit(viewId, r.material.shader.program, 0, (byte)bgfx.DiscardFlags.All);
        }
    }
}
