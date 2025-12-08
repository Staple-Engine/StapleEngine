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
    public (int, VertexBuffer)[] writeOnlyBuffers;
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

            if (writeOnlyBuffers != null)
            {
                foreach (var t in writeOnlyBuffers)
                {
                    hashCode.Add(t);
                }
            }

            if (textures != null)
            {
                foreach(var t in textures)
                {
                    hashCode.Add(t);
                }
            }

            if((renderTarget?.Disposed ?? true) == false)
            {
                for(var i = 0; i < renderTarget.ColorTextureCount; i++)
                {
                    var texture = renderTarget.GetColorTexture(i);

                    if((texture?.Disposed ?? true) || texture.impl is not SDLGPUTexture t)
                    {
                        continue;
                    }

                    hashCode.Add(t.handle);
                }

                {
                    if ((renderTarget.DepthTexture?.Disposed ?? true) == false &&
                        renderTarget.DepthTexture.impl is SDLGPUTexture t)
                    {
                        hashCode.Add(t.handle);
                    }
                }
            }

            return hashCode.ToHashCode();
        }
    }
}
