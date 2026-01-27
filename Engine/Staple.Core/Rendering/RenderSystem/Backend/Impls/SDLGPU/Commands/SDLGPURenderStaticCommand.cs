using SDL;

namespace Staple.Internal;

internal unsafe class SDLGPURenderStaticCommand(SDLGPURendererBackend backend, RenderState state, SDL_GPUGraphicsPipeline *pipeline,
    Texture[] vertexTextures, Texture[] fragmentTextures, int storageBufferBindingStart, (int, int) vertexUniformData,
    (int, int) fragmentUniformData, VertexAttribute[] vertexAttributes, int offset, int drawCount) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = (Texture[])vertexTextures?.Clone();
    private readonly Texture[] fragmentTextures = (Texture[])fragmentTextures?.Clone();

    public void Update()
    {
        if (SDLGPURendererBackend.staticMeshVertexBuffers[0] == null ||
            !backend.TryGetTextureSamplers(vertexTextures, fragmentTextures, state.shaderInstance, out var vertexSamplers,
            out var fragmentSamplers))
        {
            return;
        }

        for (var i = 0; i < vertexAttributes.Length; i++)
        {
            var bufferIndex = BufferAttributeContainer.BufferIndex(vertexAttributes[i]);

            if (bufferIndex < 0)
            {
                return;
            }

            SDLGPURenderCommand.staticMeshVertexBinding[i].offset = 0;
            SDLGPURenderCommand.staticMeshVertexBinding[i].buffer = SDLGPURendererBackend.staticMeshVertexBuffers[bufferIndex];
        }

        var hasVertexStorageBuffers = state.vertexStorageBuffers != null;
        var hasFragmentStorageBuffers = state.fragmentStorageBuffers != null;

        var renderPass = backend.renderPass;

        if (renderPass == null)
        {
            backend.ResumeRenderPass();

            renderPass = backend.renderPass;
        }

        if (SDLGPURendererBackend.lastGraphicsPipeline != pipeline)
        {
            SDLGPURendererBackend.lastGraphicsPipeline = pipeline;

            SDL3.SDL_BindGPUGraphicsPipeline(renderPass, pipeline);
        }

        SDLGPURenderCommand.indexBinding.offset = 0;
        SDLGPURenderCommand.indexBinding.buffer = SDLGPURendererBackend.staticMeshIndexBuffer;

        if (SDLGPURendererBackend.lastVertexBuffer != SDLGPURenderCommand.staticMeshVertexBinding[0].buffer)
        {
            SDLGPURendererBackend.lastVertexBuffer = SDLGPURenderCommand.staticMeshVertexBinding[0].buffer;

            fixed (SDL_GPUBufferBinding* b = SDLGPURenderCommand.staticMeshVertexBinding)
            {
                SDL3.SDL_BindGPUVertexBuffers(renderPass, 0, b, (uint)vertexAttributes.Length);
            }
        }

        if (SDLGPURendererBackend.lastIndexBuffer != SDLGPURenderCommand.indexBinding.buffer)
        {
            SDLGPURendererBackend.lastIndexBuffer = SDLGPURenderCommand.indexBinding.buffer;

            fixed (SDL_GPUBufferBinding* b = &SDLGPURenderCommand.indexBinding)
            {
                SDL3.SDL_BindGPUIndexBuffer(renderPass, b, SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT);
            }
        }

        if (state.scissor != default)
        {
            SDLGPURenderCommand.scissor.x = state.scissor.left;
            SDLGPURenderCommand.scissor.y = state.scissor.top;
            SDLGPURenderCommand.scissor.w = state.scissor.Width;
            SDLGPURenderCommand.scissor.h = state.scissor.Height;
        }
        else
        {
            SDLGPURenderCommand.scissor.x = SDLGPURenderCommand.scissor.y = 0;
            SDLGPURenderCommand.scissor.w = backend.renderSize.X;
            SDLGPURenderCommand.scissor.h = backend.renderSize.Y;
        }

        fixed (SDL_Rect* r = &SDLGPURenderCommand.scissor)
        {
            SDL3.SDL_SetGPUScissor(renderPass, r);
        }

        if (vertexSamplers.IsEmpty == false)
        {
            fixed (SDL_GPUTextureSamplerBinding* ptr = vertexSamplers)
            {
                SDL3.SDL_BindGPUVertexSamplers(renderPass, 0, ptr, (uint)vertexSamplers.Length);
            }
        }

        if (fragmentSamplers.IsEmpty == false)
        {
            fixed (SDL_GPUTextureSamplerBinding* ptr = fragmentSamplers)
            {
                SDL3.SDL_BindGPUFragmentSamplers(renderPass, 0, ptr, (uint)fragmentSamplers.Length);
            }
        }

        var buffers = backend.bufferStaging;
        var counter = 2;

        buffers[0] = SDLGPURendererBackend.entityTransformsBuffer;
        buffers[1] = SDLGPURendererBackend.entityTransformIndexBuffer;

        if (hasVertexStorageBuffers)
        {
            if (state.vertexStorageBuffers.Count > buffers.Length)
            {
                return;
            }

            foreach (var (binding, buffer) in state.vertexStorageBuffers)
            {
                if (buffer.Disposed ||
                    buffer is not SDLGPUVertexBuffer v ||
                    !backend.TryGetVertexBuffer(v.handle, out var resource) ||
                    !resource.used ||
                    resource.buffer == null)
                {
                    return;
                }

                buffers[counter++] = resource.buffer;
            }
        }

        fixed (SDL_GPUBuffer** ptr = buffers)
        {
            SDL3.SDL_BindGPUVertexStorageBuffers(renderPass, (uint)storageBufferBindingStart, ptr, (uint)counter);
        }

        if (hasFragmentStorageBuffers)
        {
            counter = 0;

            var firstBinding = -1;

            if (state.fragmentStorageBuffers.Count > buffers.Length)
            {
                return;
            }

            foreach (var (binding, buffer) in state.fragmentStorageBuffers)
            {
                if (buffer.Disposed ||
                    buffer is not SDLGPUVertexBuffer v ||
                    !backend.TryGetVertexBuffer(v.handle, out var resource) ||
                    !resource.used ||
                    resource.buffer == null)
                {
                    return;
                }

                buffers[counter++] = resource.buffer;

                if (firstBinding < 0)
                {
                    firstBinding = binding;
                }
            }

            fixed (SDL_GPUBuffer** ptr = buffers)
            {
                SDL3.SDL_BindGPUFragmentStorageBuffers(renderPass, (uint)firstBinding, ptr, (uint)counter);
            }
        }

        var vertexSpan = backend.shaderUniformFrameAllocator.GetSpan(vertexUniformData.Item1, vertexUniformData.Item2);

        for (var i = 0; i < vertexSpan.Length; i++)
        {
            var uniform = vertexSpan[i];

            if (uniform.used == false)
            {
                continue;
            }

            var target = backend.frameAllocator.Get(uniform.position);

            SDL3.SDL_PushGPUVertexUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
        }

        var fragmentSpan = backend.shaderUniformFrameAllocator.GetSpan(fragmentUniformData.Item1, fragmentUniformData.Item2);

        for (var i = 0; i < fragmentSpan.Length; i++)
        {
            var uniform = fragmentSpan[i];

            if (uniform.used == false)
            {
                continue;
            }

            var target = backend.frameAllocator.Get(uniform.position);

            SDL3.SDL_PushGPUFragmentUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
        }

        SDL3.SDL_DrawGPUIndexedPrimitivesIndirect(renderPass, backend.indirectCommandBuffer,
            (uint)(offset * SDLGPURendererBackend.IndirectDrawCommandSize), (uint)drawCount);
    }
}
