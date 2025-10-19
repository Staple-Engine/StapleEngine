using System;

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

            return hashCode.ToHashCode();
        }
    }
}
