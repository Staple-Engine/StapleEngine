using SDL3;

namespace Staple.Internal;

internal class SDLGPURenderTransientCommand(SDLGPURendererBackend backend, RenderState state, nint pipeline, Texture[] vertexTextures,
    Texture[] fragmentTextures, int storageBufferBindingStart, (int, int) vertexUniformData, (int, int) fragmentUniformData,
    SDLGPURendererBackend.TransientEntry entry) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = (Texture[])vertexTextures?.Clone();
    private readonly Texture[] fragmentTextures = (Texture[])fragmentTextures?.Clone();

    public void Update()
    {
        if (entry.vertexBuffer == nint.Zero ||
            !backend.TryGetTextureSamplers(vertexTextures, fragmentTextures, state.shaderInstance,
                out var vertexSamplers, out var fragmentSamplers))
        {
            return;
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

        if (state.scissor != default)
        {
            SDLGPURenderCommand.scissor.X = state.scissor.left;
            SDLGPURenderCommand.scissor.Y = state.scissor.top;
            SDLGPURenderCommand.scissor.W = state.scissor.Width;
            SDLGPURenderCommand.scissor.H = state.scissor.Height;
        }
        else
        {
            SDLGPURenderCommand.scissor.X = SDLGPURenderCommand.scissor.Y = 0;
            SDLGPURenderCommand.scissor.W = backend.renderSize.X;
            SDLGPURenderCommand.scissor.H = backend.renderSize.Y;
        }

        SDL.SetGPUScissor(renderPass, in SDLGPURenderCommand.scissor);

        SDLGPURenderCommand.vertexBinding[0].Buffer = entry.vertexBuffer;

        SDLGPURenderCommand.indexBinding.Offset = 0;
        SDLGPURenderCommand.indexBinding.Buffer = entry.indexBuffer;

        if (SDLGPURendererBackend.lastVertexBuffer != entry.vertexBuffer)
        {
            SDLGPURendererBackend.lastVertexBuffer = entry.vertexBuffer;

            SDL.BindGPUVertexBuffers(renderPass, 0, SDLGPURenderCommand.vertexBinding, 1);
        }

        if (SDLGPURendererBackend.lastIndexBuffer != entry.indexBuffer)
        {
            SDLGPURendererBackend.lastIndexBuffer = entry.indexBuffer;

            SDL.BindGPUIndexBuffer(renderPass, in SDLGPURenderCommand.indexBinding, SDL.GPUIndexElementSize.IndexElementSize16Bit);
        }

        if (vertexSamplers.IsEmpty == false)
        {
            unsafe
            {
                fixed (void* ptr = vertexSamplers)
                {
                    SDL.BindGPUVertexSamplers(renderPass, 0, (nint)ptr, (uint)vertexSamplers.Length);
                }
            }

        }

        if (fragmentSamplers.IsEmpty == false)
        {
            unsafe
            {
                fixed (void* ptr = fragmentSamplers)
                {
                    SDL.BindGPUFragmentSamplers(renderPass, 0, (nint)ptr, (uint)fragmentSamplers.Length);
                }
            }
        }

        var buffers = backend.nintBufferStaging;
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
                    resource.buffer == nint.Zero)
                {
                    return;
                }

                buffers[counter++] = resource.buffer;
            }
        }

        SDL.BindGPUVertexStorageBuffers(renderPass, (uint)storageBufferBindingStart, buffers, (uint)counter);

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

            SDL.BindGPUFragmentStorageBuffers(renderPass, (uint)firstBinding, buffers, (uint)counter);
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

            SDL.PushGPUVertexUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
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

            SDL.PushGPUFragmentUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
        }

        SDL.DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, (uint)(state.instanceCount > 1 ? state.instanceCount : 1),
            (uint)state.startIndex, state.startVertex, 0);
    }
}
