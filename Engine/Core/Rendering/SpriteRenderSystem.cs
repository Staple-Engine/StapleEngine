using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    internal class SpriteRenderSystem
    {
        struct SpriteVertex
        {
            public Vector3 position;
            public Vector2 texCoord;
        }

        private static SpriteVertex[] vertices = new SpriteVertex[]
        {
            new SpriteVertex() {
                position = Vector3.Zero,
                texCoord = Vector2.Zero,
            },
            new SpriteVertex() {
                position = new Vector3(0, 1, 0),
                texCoord = new Vector2(0, 1),
            },
            new SpriteVertex() {
                position = new Vector3(1, 1, 0),
                texCoord = Vector2.One,
            },
            new SpriteVertex() {
                position = new Vector3(1, 0, 0),
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

        public void Process(Entity entity, SpriteRenderer renderer)
        {
            if(vertexLayout == null)
            {
                vertexLayout = new VertexLayoutBuilder()
                    .Add(bgfx.Attrib.Position, 3, bgfx.AttribType.Float)
                    //.Add(bgfx.Attrib.Color0, 4, bgfx.AttribType.Uint8, true)
                    .Add(bgfx.Attrib.TexCoord0, 2, bgfx.AttribType.Float)
                    .Build();

                vertexBuffer = VertexBuffer.Create(vertices, vertexLayout);

                indexBuffer = IndexBuffer.Create(indices, RenderBufferFlags.None);
            }
        }
    }
}
