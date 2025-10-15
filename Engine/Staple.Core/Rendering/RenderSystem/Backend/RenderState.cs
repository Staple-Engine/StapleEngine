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
}
