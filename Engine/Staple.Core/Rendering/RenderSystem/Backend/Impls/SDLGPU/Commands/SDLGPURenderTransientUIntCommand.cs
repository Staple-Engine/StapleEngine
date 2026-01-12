using SDL3;
using Staple.Utilities;

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

        using var vertexUniformHandle = new GlobalAllocator<StapleShaderUniform>.GlobalAllocatorHandle(vertexUniformData);
        using var fragmentUniformHandle = new GlobalAllocator<StapleShaderUniform>.GlobalAllocatorHandle(fragmentUniformData);

        if (entry.vertexBuffer == nint.Zero ||
            !backend.TryGetTextureSamplers(vertexTextures, fragmentTextures, state.shaderInstance,
                out var vertexSamplers, out var fragmentSamplers))
        {
            return;
        }

        using var vertexSamplerHandle = new GlobalAllocator<SDL.GPUTextureSamplerBinding>.GlobalAllocatorHandle(vertexSamplers);
        using var fragmentSamplerHandle = new GlobalAllocator<SDL.GPUTextureSamplerBinding>.GlobalAllocatorHandle(fragmentSamplers);

        var hasVertexStorageBuffers = state.vertexStorageBuffers != null;
        var hasFragmentStorageBuffers = state.fragmentStorageBuffers != null;

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
