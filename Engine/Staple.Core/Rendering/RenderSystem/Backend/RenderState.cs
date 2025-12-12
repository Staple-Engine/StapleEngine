using System;
using System.Numerics;

namespace Staple.Internal;

internal struct RenderState
{
    public Shader shader;
    public ComputeShader computeShader;
    public StringID shaderVariant;
    public MeshTopology primitiveType;
    public CullingMode cull;
    public bool wireframe;
    public bool enableDepth;
    public bool depthWrite;
    public VertexBuffer vertexBuffer;
    public IndexBuffer indexBuffer;
    public InstanceBuffer instanceBuffer;
    public (int, VertexBuffer)[] storageBuffers;
    public int startVertex;
    public int startIndex;
    public int indexCount;
    public RenderTarget renderTarget;
    public BlendMode sourceBlend;
    public BlendMode destinationBlend;
    public Rect scissor;
    public Texture[] vertexTextures;
    public Texture[] fragmentTextures;
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

            if(storageBuffers != null)
            {
                foreach(var t in storageBuffers)
                {
                    hashCode.Add(t);
                }
            }

            if (vertexTextures != null)
            {
                foreach(var t in vertexTextures)
                {
                    hashCode.Add(t);
                }
            }

            if (fragmentTextures != null)
            {
                foreach (var t in fragmentTextures)
                {
                    hashCode.Add(t);
                }
            }

            if ((renderTarget?.Disposed ?? true) == false)
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
