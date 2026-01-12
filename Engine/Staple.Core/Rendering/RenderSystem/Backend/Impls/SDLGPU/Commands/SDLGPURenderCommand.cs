using SDL3;
using Staple.Utilities;

namespace Staple.Internal;

internal class SDLGPURenderCommand(RenderState state, nint pipeline, Texture[] vertexTextures, Texture[] fragmentTextures,
    StapleShaderUniform[] vertexUniformData, StapleShaderUniform[] fragmentUniformData, VertexAttribute[] vertexAttributes) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = (Texture[])vertexTextures?.Clone();
    private readonly Texture[] fragmentTextures = (Texture[])fragmentTextures?.Clone();

    internal static readonly SDL.GPUBufferBinding[] vertexBinding = new SDL.GPUBufferBinding[1];

    internal static readonly SDL.GPUBufferBinding[] staticMeshVertexBinding = new SDL.GPUBufferBinding[18];

    internal static SDL.GPUBufferBinding indexBinding;
    internal static SDL.Rect scissor;

    public void Update(IRendererBackend rendererBackend)
    {
        var backend = (SDLGPURendererBackend)rendererBackend;
        var vertex = (SDLGPUVertexBuffer)state.vertexBuffer;
        var index = (SDLGPUIndexBuffer)state.indexBuffer;
        var staticMeshEntries = state.staticMeshEntries;

        SDLGPURendererBackend.BufferResource vertexBuffer = null;
        SDLGPURendererBackend.BufferResource indexBuffer = null;

        using var vertexUniformHandle = new GlobalAllocator<StapleShaderUniform>.GlobalAllocatorHandle(vertexUniformData);
        using var fragmentUniformHandle = new GlobalAllocator<StapleShaderUniform>.GlobalAllocatorHandle(fragmentUniformData);

        if ((staticMeshEntries == null &&
            (!backend.TryGetVertexBuffer(vertex.handle, out vertexBuffer) ||
            !backend.TryGetIndexBuffer(index.handle, out indexBuffer))) ||
            (staticMeshEntries != null && SDLGPURendererBackend.staticMeshVertexBuffers[0] == nint.Zero) ||
            !backend.TryGetTextureSamplers(vertexTextures, fragmentTextures, state.shaderInstance,
                out var vertexSamplers, out var fragmentSamplers))
        {
            return;
        }

        using var vertexSamplerHandle = new GlobalAllocator<SDL.GPUTextureSamplerBinding>.GlobalAllocatorHandle(vertexSamplers);
        using var fragmentSamplerHandle = new GlobalAllocator<SDL.GPUTextureSamplerBinding>.GlobalAllocatorHandle(fragmentSamplers);

        if (staticMeshEntries != null)
        {
            for (var i = 0; i < vertexAttributes.Length; i++)
            {
                var bufferIndex = BufferAttributeContainer.BufferIndex(vertexAttributes[i]);

                if(bufferIndex < 0)
                {
                    return;
                }

                staticMeshVertexBinding[i].Offset = (uint)(staticMeshEntries.positionEntry.start * 
                    SDLGPURendererBackend.staticMeshVertexBuffersElementSize[bufferIndex]);
                staticMeshVertexBinding[i].Buffer = SDLGPURendererBackend.staticMeshVertexBuffers[bufferIndex];
            }
        }

        var hasVertexStorageBuffers = state.vertexStorageBuffers != null;
        var hasFragmentStorageBuffers = state.fragmentStorageBuffers != null;

        var renderPass = backend.renderPass;

        if (renderPass == nint.Zero)
        {
            backend.ResumeRenderPass();

            renderPass = backend.renderPass;
        }

        if (SDLGPURendererBackend.lastGraphicsPipeline != pipeline)
        {
            SDLGPURendererBackend.lastGraphicsPipeline = pipeline;

            SDL.BindGPUGraphicsPipeline(renderPass, pipeline);
        }

        if (staticMeshEntries == null)
        {
            vertexBinding[0].Buffer = vertexBuffer.buffer;

            indexBinding.Offset = 0;
            indexBinding.Buffer = indexBuffer.buffer;

            if (SDLGPURendererBackend.lastVertexBuffer != vertexBuffer.buffer)
            {
                SDLGPURendererBackend.lastVertexBuffer = vertexBuffer.buffer;

                SDL.BindGPUVertexBuffers(renderPass, 0, vertexBinding, 1);
            }

            if(SDLGPURendererBackend.lastIndexBuffer != indexBuffer.buffer)
            {
                SDLGPURendererBackend.lastIndexBuffer = indexBuffer.buffer;

                SDL.BindGPUIndexBuffer(renderPass, in indexBinding, index.Is32Bit ?
                    SDL.GPUIndexElementSize.IndexElementSize32Bit :
                    SDL.GPUIndexElementSize.IndexElementSize16Bit);
            }
        }
        else
        {
            indexBinding.Offset = (uint)(staticMeshEntries.indicesEntry.start * sizeof(uint));
            indexBinding.Buffer = SDLGPURendererBackend.staticMeshIndexBuffer;

            if(SDLGPURendererBackend.lastVertexBuffer != staticMeshVertexBinding[0].Buffer)
            {
                SDLGPURendererBackend.lastVertexBuffer = staticMeshVertexBinding[0].Buffer;

                SDL.BindGPUVertexBuffers(renderPass, 0, staticMeshVertexBinding, (uint)vertexAttributes.Length);
            }

            if (SDLGPURendererBackend.lastIndexBuffer != indexBinding.Buffer)
            {
                SDLGPURendererBackend.lastIndexBuffer = indexBinding.Buffer;

                SDL.BindGPUIndexBuffer(renderPass, in indexBinding, SDL.GPUIndexElementSize.IndexElementSize32Bit);
            }
        }

        if (vertexSamplers != null)
        {
            SDL.BindGPUVertexSamplers(renderPass, 0, vertexSamplers, (uint)vertexSamplers.Length);
        }

        if (fragmentSamplers != null)
        {
            SDL.BindGPUFragmentSamplers(renderPass, 0, fragmentSamplers, (uint)fragmentSamplers.Length);
        }

        if (hasVertexStorageBuffers)
        {
            var buffers = GlobalAllocator<nint>.Instance.Rent(state.vertexStorageBuffers.Count);
            var counter = 0;
            var firstBinding = -1;

            foreach (var (binding, buffer) in state.vertexStorageBuffers)
            {
                if (buffer.Disposed ||
                    buffer is not SDLGPUVertexBuffer v ||
                    !backend.TryGetVertexBuffer(v.handle, out var resource) ||
                    !resource.used ||
                    resource.buffer == nint.Zero)
                {
                    return;
                }

                buffers[counter++] = resource.buffer;

                if (firstBinding < 0)
                {
                    firstBinding = binding;
                }
            }

            SDL.BindGPUVertexStorageBuffers(renderPass, (uint)firstBinding, buffers, (uint)buffers.Length);

            GlobalAllocator<nint>.Instance.Return(buffers);
        }

        if (hasFragmentStorageBuffers)
        {
            var buffers = GlobalAllocator<nint>.Instance.Rent(state.fragmentStorageBuffers.Count);
            var counter = 0;
            var firstBinding = -1;

            foreach (var (binding, buffer) in state.fragmentStorageBuffers)
            {
                if (buffer.Disposed ||
                    buffer is not SDLGPUVertexBuffer v ||
                    !backend.TryGetVertexBuffer(v.handle, out var resource) ||
                    !resource.used ||
                    resource.buffer == nint.Zero)
                {
                    return;
                }

                buffers[counter++] = resource.buffer;

                if (firstBinding < 0)
                {
                    firstBinding = binding;
                }
            }

            SDL.BindGPUFragmentStorageBuffers(renderPass, (uint)firstBinding, buffers, (uint)buffers.Length);

            GlobalAllocator<nint>.Instance.Return(buffers);
        }

        for (var i = 0; i < vertexUniformData.Length; i++)
        {
            var uniform = vertexUniformData[i];

            if(uniform.used == false)
            {
                continue;
            }

            var target = backend.frameAllocator.Get(uniform.position);

            SDL.PushGPUVertexUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
        }

        for (var i = 0; i < fragmentUniformData.Length; i++)
        {
            var uniform = fragmentUniformData[i];

            if (uniform.used == false)
            {
                continue;
            }

            var target = backend.frameAllocator.Get(uniform.position);

            SDL.PushGPUFragmentUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
        }

        SDL.DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, (uint)(state.instanceCount > 1 ? state.instanceCount : 1),
            (uint)state.startIndex, state.startVertex, 0);
    }
}
