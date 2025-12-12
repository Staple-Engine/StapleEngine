using System;
using System.Numerics;

namespace Staple.Internal;

internal struct RenderState
{
    public MeshTopology primitiveType;
    public CullingMode cull;
    public bool wireframe;
    public bool enableDepth;
    public bool depthWrite;
    public BlendMode sourceBlend;
    public BlendMode destinationBlend;

    public Shader shader;
    public ComputeShader computeShader;
    public Shader.ShaderInstance shaderInstance;
    public VertexBuffer vertexBuffer;
    public IndexBuffer indexBuffer;
    public InstanceBuffer instanceBuffer;
    public (int, VertexBuffer)[] storageBuffers;
    public int startVertex;
    public int startIndex;
    public int indexCount;
    public RenderTarget renderTarget;
    public Rect scissor;
    public Texture[] vertexTextures;
    public Texture[] fragmentTextures;
    public Matrix4x4 world;

    internal readonly int StateKey
    {
        get
        {
            var hashCode = new HashCode();

            hashCode.Add(primitiveType);
            hashCode.Add(cull);
            hashCode.Add(wireframe);
            hashCode.Add(enableDepth);
            hashCode.Add(depthWrite);
            hashCode.Add(vertexBuffer?.layout);
            hashCode.Add(sourceBlend);
            hashCode.Add(destinationBlend);
            hashCode.Add(shaderInstance?.program?.StateKey ?? 0);

            return hashCode.ToHashCode();
        }
    }
}
