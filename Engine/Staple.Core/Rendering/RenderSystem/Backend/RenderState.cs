using System;
using System.Numerics;

namespace Staple.Internal;

internal struct RenderState
{
    public Shader shader;
    public ComputeShader computeShader;
    public string shaderVariant;
    public MeshTopology primitiveType;
    public CullingMode cull;
    public bool wireframe;
    public bool enableDepth;
    public bool depthWrite;
    public VertexBuffer vertexBuffer;
    public IndexBuffer indexBuffer;
    public InstanceBuffer instanceBuffer;
    public (int, VertexBuffer)[] readOnlyBuffers;
    public (int, VertexBuffer)[] readWriteBuffers;
    public int startVertex;
    public int startIndex;
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

            hashCode.Add(shaderVariant);
            hashCode.Add(primitiveType);
            hashCode.Add(cull);
            hashCode.Add(wireframe);
            hashCode.Add(enableDepth);
            hashCode.Add(depthWrite);
            hashCode.Add(vertexBuffer?.layout);
            hashCode.Add(renderTarget);
            hashCode.Add(sourceBlend);
            hashCode.Add(destinationBlend);

            if(readOnlyBuffers != null)
            {
                foreach(var t in readOnlyBuffers)
                {
                    hashCode.Add(t);
                }
            }

            if (readWriteBuffers != null)
            {
                foreach (var t in readWriteBuffers)
                {
                    hashCode.Add(t);
                }
            }

            if (textures != null)
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
