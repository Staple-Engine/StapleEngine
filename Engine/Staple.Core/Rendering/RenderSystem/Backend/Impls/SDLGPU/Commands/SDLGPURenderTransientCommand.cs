using SDL;

namespace Staple.Internal;

internal unsafe class SDLGPURenderTransientCommand(SDLGPURendererBackend backend, RenderState state,
    SDL_GPUGraphicsPipeline *pipeline, Texture[] vertexTextures, Texture[] fragmentTextures, int storageBufferBindingStart,
    (int, int) vertexUniformData, (int, int) fragmentUniformData, SDLGPURendererBackend.TransientEntry entry) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = (Texture[])vertexTextures?.Clone();
    private readonly Texture[] fragmentTextures = (Texture[])fragmentTextures?.Clone();

    public void Update()
    {
        if (entry.vertexBuffer == null ||
            !backend.TryGetTextureSamplers(vertexTextures, fragmentTextures, state.shaderInstance,
                out var vertexSamplers, out var fragmentSamplers))
        {
            return;
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

        SDLGPURenderCommand.vertexBinding[0].buffer = entry.vertexBuffer;

        SDLGPURenderCommand.indexBinding.offset = 0;
        SDLGPURenderCommand.indexBinding.buffer = entry.indexBuffer;

        if (SDLGPURendererBackend.lastVertexBuffer != entry.vertexBuffer)
        {
            SDLGPURendererBackend.lastVertexBuffer = entry.vertexBuffer;

            fixed (SDL_GPUBufferBinding* b = SDLGPURenderCommand.vertexBinding)
            {
                SDL3.SDL_BindGPUVertexBuffers(renderPass, 0, b, 1);
            }
        }

        if (SDLGPURendererBackend.lastIndexBuffer != entry.indexBuffer)
        {
            SDLGPURendererBackend.lastIndexBuffer = entry.indexBuffer;

            fixed (SDL_GPUBufferBinding* b = &SDLGPURenderCommand.indexBinding)
            {
                SDL3.SDL_BindGPUIndexBuffer(renderPass, b, SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT);
            }
        }

        if (vertexSamplers.IsEmpty == false)
        {
            unsafe
            {
                fixed (SDL_GPUTextureSamplerBinding* ptr = vertexSamplers)
                {
                    SDL3.SDL_BindGPUVertexSamplers(renderPass, 0, ptr, (uint)vertexSamplers.Length);
                }
            }

        }

        if (fragmentSamplers.IsEmpty == false)
        {
            unsafe
            {
                fixed (SDL_GPUTextureSamplerBinding* ptr = fragmentSamplers)
                {
                    SDL3.SDL_BindGPUFragmentSamplers(renderPass, 0, ptr, (uint)fragmentSamplers.Length);
                }
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

        SDL3.SDL_DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, (uint)(state.instanceCount > 1 ?
            state.instanceCount : 1), (uint)state.startIndex, state.startVertex, 0);
    }
}
