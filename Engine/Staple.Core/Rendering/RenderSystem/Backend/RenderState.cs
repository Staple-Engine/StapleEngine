using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

internal struct RenderState
{
    public IShaderProgram program;
    public MeshTopology primitiveType;
    public CullingMode cull;
    public bool wireframe;
    public bool enableDepth;
    public bool depthWrite;
    public VertexBuffer vertexBuffer;
    public IndexBuffer indexBuffer;
    public InstanceBuffer instanceBuffer;
    public VertexLayout vertexLayout;
    public int startVertex;
    public int startIndex;
    public int vertexCount;
    public int indexCount;
    public RenderTarget renderTarget;
    public BlendMode sourceBlend;
    public BlendMode destinationBlend;
    public Rect scissor;
    public Texture[] textures;
    public Matrix4x4 world;

    internal readonly int StateKey
    {
        get
        {
            var hashCode = new HashCode();

            hashCode.Add(program);
            hashCode.Add(primitiveType);
            hashCode.Add(cull);
            hashCode.Add(wireframe);
            hashCode.Add(enableDepth);
            hashCode.Add(depthWrite);
            hashCode.Add(vertexLayout);
            hashCode.Add(renderTarget);
            hashCode.Add(sourceBlend);
            hashCode.Add(destinationBlend);

            if(textures != null)
            {
                foreach(var t in textures)
                {
                    hashCode.Add(t.GetHashCode());
                }
            }

            return hashCode.ToHashCode();
        }
    }
}
