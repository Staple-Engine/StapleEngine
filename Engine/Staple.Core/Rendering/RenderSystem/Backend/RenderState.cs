using System;
using System.Collections.Generic;
using System.Linq;
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
    public Dictionary<int, VertexBuffer> vertexStorageBuffers;
    public Dictionary<int, VertexBuffer> fragmentStorageBuffers;
    public Dictionary<int, VertexBuffer> computeStorageBuffers;
    public int startVertex;
    public int startIndex;
    public int indexCount;
    public RenderTarget renderTarget;
    public Rect scissor;
    public Texture[] vertexTextures;
    public Texture[] fragmentTextures;
    public Matrix4x4 world;

    public readonly RenderState Clone()
    {
        return new()
        {
            computeShader = computeShader,
            computeStorageBuffers = computeStorageBuffers != null ? new(computeStorageBuffers) : null,
            cull = cull,
            depthWrite = depthWrite,
            destinationBlend = destinationBlend,
            enableDepth = enableDepth,
            fragmentStorageBuffers = fragmentStorageBuffers != null ? new(fragmentStorageBuffers) : null,
            fragmentTextures = (Texture[])fragmentTextures?.Clone(),
            indexBuffer = indexBuffer,
            indexCount = indexCount,
            instanceBuffer = instanceBuffer,
            primitiveType = primitiveType,
            renderTarget = renderTarget,
            scissor = scissor,
            vertexStorageBuffers = vertexStorageBuffers != null ? new(vertexStorageBuffers) : null,
            shader = shader,
            shaderInstance = shaderInstance,
            sourceBlend = sourceBlend,
            startIndex = startIndex,
            startVertex = startVertex,
            vertexBuffer = vertexBuffer,
            vertexTextures = (Texture[])vertexTextures?.Clone(),
            wireframe = wireframe,
            world = world,
        };
    }

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

    public void ApplyStorageBufferIfNeeded(string name, VertexBuffer buffer)
    {
        if (shaderInstance?.program == null)
        {
            return;
        }

        var binding = -1;

        void Apply(ref Dictionary<int, VertexBuffer> storageBuffers)
        {
            storageBuffers ??= [];

            if (storageBuffers.Count == 0)
            {
                storageBuffers.AddOrSetKey(binding, buffer);
            }
            else
            {
                var found = false;

                foreach (var pair in storageBuffers)
                {
                    if ((pair.Key == binding || pair.Value == buffer) &&
                        pair.Key != binding && pair.Value != buffer)
                    {
                        found = true;

                        break;
                    }
                }

                if (found)
                {
                    var keys = storageBuffers.Keys.ToArray();

                    foreach (var key in keys)
                    {
                        var value = storageBuffers[key];

                        if ((key == binding || value == buffer) &&
                            key != binding && value != buffer)
                        {
                            storageBuffers.Remove(key);
                        }
                    }
                }

                storageBuffers.AddOrSetKey(binding, buffer);
            }
        }

        var localIndex = shaderInstance.vertexUniforms.storageBuffers.FindIndex(x => x.name == name);

        if (localIndex >= 0)
        {
            var offset = shaderInstance.vertexShaderMetrics.samplerCount + shaderInstance.vertexShaderMetrics.storageTextureCount;

            binding = shaderInstance.vertexUniforms.storageBuffers[localIndex].binding - offset;

            Apply(ref vertexStorageBuffers);
        }

        localIndex = shaderInstance.fragmentUniforms.storageBuffers.FindIndex(x => x.name == name);

        if (localIndex >= 0)
        {
            var offset = shaderInstance.fragmentShaderMetrics.samplerCount + shaderInstance.fragmentShaderMetrics.storageTextureCount;

            binding = shaderInstance.fragmentUniforms.storageBuffers[localIndex].binding - offset;

            Apply(ref fragmentStorageBuffers);
        }

        //TODO: Compute Shaders
    }
}
