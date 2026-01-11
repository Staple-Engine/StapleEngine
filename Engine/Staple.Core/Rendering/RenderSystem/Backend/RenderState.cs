using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal;

internal struct RenderState
{
    public struct BufferBinding
    {
        public int binding;
        public VertexBuffer buffer;
    }

    public MeshTopology primitiveType;
    public CullingMode cull;
    public bool wireframe;
    public bool enableDepth;
    public bool depthWrite;
    public BlendMode sourceBlend;
    public BlendMode destinationBlend;

    public Shader shader;
    public Shader.ShaderInstance shaderInstance;
    public VertexBuffer vertexBuffer;
    public IndexBuffer indexBuffer;
    public BufferAttributeContainer.Entries staticMeshEntries;
    public List<BufferBinding> vertexStorageBuffers;
    public List<BufferBinding> fragmentStorageBuffers;
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

    public readonly RenderState Clone()
    {
        var vertexTextures = this.vertexTextures != null ? new Texture[this.vertexTextures.Length] : null;
        var fragmentTextures = this.fragmentTextures != null ? new Texture[this.fragmentTextures.Length] : null;

        if(vertexTextures != null)
        {
            Array.Copy(this.vertexTextures, vertexTextures, vertexTextures.Length);
        }

        if(fragmentTextures != null)
        {
            Array.Copy(this.fragmentTextures, fragmentTextures, fragmentTextures.Length);
        }

        return new()
        {
            cull = cull,
            depthWrite = depthWrite,
            destinationBlend = destinationBlend,
            enableDepth = enableDepth,
            fragmentStorageBuffers = fragmentStorageBuffers != null ? new(fragmentStorageBuffers) : null,
            fragmentTextures = fragmentTextures,
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
            vertexTextures = vertexTextures,
            wireframe = wireframe,
            world = world,
            instanceOffset = instanceOffset,
            instanceCount = instanceCount,
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

    public readonly void ClearStorageBuffers()
    {
        vertexStorageBuffers?.Clear();
        fragmentStorageBuffers?.Clear();
    }

    public void ApplyStorageBufferIfNeeded(string name, VertexBuffer buffer)
    {
        if (shaderInstance?.program == null)
        {
            return;
        }

        var binding = -1;

        void Apply(ref List<BufferBinding> storageBuffers)
        {
            storageBuffers ??= [];

            storageBuffers.Add(new()
            {
                binding = binding,
                buffer = buffer
            });
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
