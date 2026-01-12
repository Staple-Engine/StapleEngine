using SDL3;

namespace Staple.Internal;

internal class SDLGPURenderTransientUIntCommand(RenderState state, nint pipeline, Texture[] vertexTextures, Texture[] fragmentTextures,
    StapleShaderUniform[] vertexUniformData, StapleShaderUniform[] fragmentUniformData, SDLGPURendererBackend.TransientEntry entry) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = (Texture[])vertexTextures?.Clone();
    private readonly Texture[] fragmentTextures = (Texture[])fragmentTextures?.Clone();

    public void Update(IRendererBackend rendererBackend)
    {
        var backend = (SDLGPURendererBackend)rendererBackend;

        if (entry.vertexBuffer == nint.Zero ||
            !backend.TryGetTextureSamplers(vertexTextures, fragmentTextures, state.shaderInstance,
                out var vertexSamplers, out var fragmentSamplers))
        {
            return;
        }

        var hasVertexStorageBuffers = state.vertexStorageBuffers != null;
        var hasFragmentStorageBuffers = state.fragmentStorageBuffers != null;

        if (hasVertexStorageBuffers)
        {
            for(var i = 0; i < state.vertexStorageBuffers.Count; i++)
            {
                var binding = state.vertexStorageBuffers[i];

                if (binding.buffer.Disposed ||
                    binding.buffer is not SDLGPUVertexBuffer v ||
                    !backend.TryGetVertexBuffer(v.handle, out var resource) ||
                    !resource.used ||
                    resource.buffer == nint.Zero)
                {
                    return;
                }
            }
        }

        if (hasFragmentStorageBuffers)
        {
            for (var i = 0; i < state.fragmentStorageBuffers.Count; i++)
            {
                var binding = state.fragmentStorageBuffers[i];

                if (binding.buffer.Disposed ||
                    binding.buffer is not SDLGPUVertexBuffer v ||
                    !backend.TryGetVertexBuffer(v.handle, out var resource) ||
                    !resource.used ||
                    resource.buffer == nint.Zero)
                {
                    return;
                }
            }
        }

        var renderPass = backend.renderPass;

        if (renderPass == nint.Zero)
        {
            backend.ResumeRenderPass();

            renderPass = backend.renderPass;
        }

        if(SDLGPURendererBackend.lastGraphicsPipeline != pipeline)
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
        SDLGPURenderCommand.indexBinding.Buffer = entry.uintIndexBuffer;

        if(SDLGPURendererBackend.lastVertexBuffer != entry.vertexBuffer)
        {
            SDLGPURendererBackend.lastVertexBuffer = entry.vertexBuffer;

            SDL.BindGPUVertexBuffers(renderPass, 0, SDLGPURenderCommand.vertexBinding, 1);
        }

        if (SDLGPURendererBackend.lastIndexBuffer != entry.uintIndexBuffer)
        {
            SDLGPURendererBackend.lastIndexBuffer = entry.uintIndexBuffer;

            SDL.BindGPUIndexBuffer(renderPass, in SDLGPURenderCommand.indexBinding, SDL.GPUIndexElementSize.IndexElementSize32Bit);
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
            for (var i = 0; i < state.vertexStorageBuffers.Count; i++)
            {
                var binding = state.vertexStorageBuffers[i];

                var buffer = binding.buffer as SDLGPUVertexBuffer;

                backend.TryGetVertexBuffer(buffer.handle, out var resource);

                SDLGPURendererBackend.singleBuffer[0] = resource.buffer;

                SDL.BindGPUVertexStorageBuffers(renderPass, (uint)binding.binding, SDLGPURendererBackend.singleBuffer, 1);
            }
        }

        if (hasFragmentStorageBuffers)
        {
            for (var i = 0; i < state.fragmentStorageBuffers.Count; i++)
            {
                var binding = state.fragmentStorageBuffers[i];

                var buffer = binding.buffer as SDLGPUVertexBuffer;

                backend.TryGetVertexBuffer(buffer.handle, out var resource);

                SDLGPURendererBackend.singleBuffer[0] = resource.buffer;

                SDL.BindGPUFragmentStorageBuffers(renderPass, (uint)binding.binding, SDLGPURendererBackend.singleBuffer, 1);
            }
        }

        for (var i = 0; i < vertexUniformData.Length; i++)
        {
            var uniform = vertexUniformData[i];

            if (uniform.used == false)
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
