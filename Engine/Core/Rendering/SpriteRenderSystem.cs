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
    internal class SpriteRenderSystem
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

        internal void Destroy()
        {
            vertexBuffer?.Destroy();
            indexBuffer?.Destroy();
        }

        public void Process(Entity entity, SpriteRenderer renderer, ushort viewId)
        {
            if(vertexLayout == null)
            {
                vertexLayout = new VertexLayoutBuilder()
                    .Add(bgfx.Attrib.Position, 3, bgfx.AttribType.Float)
                    .Add(bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float)
                    .Build();

                vertexBuffer = VertexBuffer.Create(vertices, vertexLayout);

                indexBuffer = IndexBuffer.Create(indices, RenderBufferFlags.None);
            }

            if(renderer.material == null || renderer.material.shader == null)
            {
                return;
            }

            vertexBuffer.SetActive(0, 0, (uint)vertexBuffer.length);
            indexBuffer.SetActive(0, (uint)indexBuffer.length);

            var scale = Vector3.Zero;

            if(renderer.material.MainTexture != null)
            {
                scale.X = renderer.material.MainTexture.SpriteWidth;
                scale.Y = renderer.material.MainTexture.SpriteHeight;
            }

            var matrix = Matrix4x4.CreateScale(scale) * entity.Transform.Matrix;

            unsafe
            {
                bgfx.set_transform(&matrix, 1);
            }

            bgfx.StateFlags state = bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA | bgfx.StateFlags.DepthTestGequal | bgfx.StateFlags.PtTristrip;

            bgfx.set_state((ulong)state, 0);

            renderer.material.shader.ApplyUniforms();

            renderer.material.shader.SetColor("mainColor", renderer.color);

            bgfx.submit(viewId, renderer.material.shader.program, 0, (byte)bgfx.DiscardFlags.All);
        }
    }
}
