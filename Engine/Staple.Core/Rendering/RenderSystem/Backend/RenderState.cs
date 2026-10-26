using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

internal struct RenderState
{
    internal record StorageBufferPair(int Binding, VertexBuffer Buffer);

    public MeshTopology primitiveType;
    public CullingMode cull;
    public bool wireframe;
    public bool enableDepth;
    public bool enableZWrite;
    public DepthTest depthTest;
    public BlendMode sourceBlend;
    public BlendMode destinationBlend;
    public MaterialRenderQueue renderQueue;
    public int renderQueueIndex;

    public Shader shader;
    public ShaderResource.ShaderInstance shaderInstance;
    public VertexBuffer vertexBuffer;
    public IndexBuffer indexBuffer;
    public BufferAttributeContainer.Entries staticMeshEntries;
    public List<StorageBufferPair> vertexStorageBuffers;
    public List<StorageBufferPair> fragmentStorageBuffers;
    public int startVertex;
    public int startIndex;
    public int indexCount;
    public RenderTarget renderTarget;
    public Rect scissor;
    public Texture[] vertexTextures;
    public Texture[] fragmentTextures;
    public Matrix4x4 world;
    public int instanceOffset;
    public int instanceCount;

    public List<Transform> instanceTransforms;

    public static RenderState Default
    {
        get
        {
            return new()
            {
                renderTarget = RenderTarget.Current,
                vertexStorageBuffers = null,
                fragmentStorageBuffers = null,
                enableDepth = true,
                enableZWrite = true,
                depthTest = DepthTest.GreaterEqual,
            };
        }
    }

    public readonly RenderState Clone()
    {
        return new()
        {
            cull = cull,
            depthTest = depthTest,
            destinationBlend = destinationBlend,
            enableDepth = enableDepth,
            enableZWrite = enableZWrite,
            fragmentStorageBuffers = fragmentStorageBuffers != null ? new(fragmentStorageBuffers) : null,
            fragmentTextures = MemoryUtils.SafeCloneArray(fragmentTextures),
            indexBuffer = indexBuffer,
            indexCount = indexCount,
            primitiveType = primitiveType,
            renderTarget = renderTarget,
            scissor = scissor,
            vertexStorageBuffers = vertexStorageBuffers != null ? new(vertexStorageBuffers) : null,
            shader = shader,
            shaderInstance = shaderInstance,
            sourceBlend = sourceBlend,
            staticMeshEntries = staticMeshEntries,
            startIndex = startIndex,
            startVertex = startVertex,
            vertexBuffer = vertexBuffer,
            vertexTextures = MemoryUtils.SafeCloneArray(vertexTextures),
            wireframe = wireframe,
            world = world,
            instanceOffset = instanceOffset,
            instanceTransforms = instanceTransforms != null ? new(instanceTransforms) : null,
            instanceCount = instanceCount,
            renderQueue = renderQueue,
            renderQueueIndex = renderQueueIndex,
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
            hashCode.Add(enableZWrite);
            hashCode.Add(depthTest);
            hashCode.Add(vertexBuffer?.layout?.StateKey ?? 0);
            hashCode.Add(sourceBlend);
            hashCode.Add(destinationBlend);
            hashCode.Add(shaderInstance?.program?.StateKey ?? 0);

            return hashCode.ToHashCode();
        }
    }

    public readonly void ClearStorageBuffers()
    {
        vertexStorageBuffers?.Clear();
        fragmentStorageBuffers?.Clear();
    }

    public void ApplyStorageBufferIfNeeded(string name, VertexBuffer buffer)
    {
        if (shaderInstance?.program == null || buffer == null)
        {
            return;
        }

        var binding = -1;

        void Apply(ref List<StorageBufferPair> storageBuffers)
        {
            storageBuffers ??= [];

            StorageBufferPair existing = null;

            foreach (var pair in storageBuffers)
            {
                if(pair.Binding == binding)
                {
                    existing = pair;

                    break;
                }
            }

            if(existing != null)
            {
                storageBuffers.Remove(existing);
            }

            storageBuffers.Add(new(binding, buffer));
        }

        var localIndex = shaderInstance.vertexUniforms.storageBuffers.FindIndex(x => x.name == name);

        if (localIndex >= 0)
        {
            var offset = shaderInstance.vertexShaderMetrics.samplerCount + shaderInstance.vertexShaderMetrics.storageTextureCount;

            binding = shaderInstance.vertexUniforms.storageBuffers[localIndex].binding - offset;

            Apply(ref vertexStorageBuffers);
        }

        localIndex = shaderInstance.fragmentUniforms.storageBuffers.FindIndex(x => x.name == name);

        if (localIndex < 0)
        {
            return;
        }

        {
            var offset = shaderInstance.fragmentShaderMetrics.samplerCount + shaderInstance.fragmentShaderMetrics.storageTextureCount;

            binding = shaderInstance.fragmentUniforms.storageBuffers[localIndex].binding - offset;

            Apply(ref fragmentStorageBuffers);
        }
    }
}
