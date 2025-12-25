using SDL3;

namespace Staple.Internal;

internal class SDLGPURenderCommand(RenderState state, nint pipeline, Texture[] vertexTextures, Texture[] fragmentTextures,
    SDLGPURendererBackend.StapleShaderUniform[] vertexUniformData, SDLGPURendererBackend.StapleShaderUniform[] fragmentUniformData,
    SDLGPUShaderProgram program) : IRenderCommand
{
    public RenderState state = state.Clone();
    public nint pipeline = pipeline;
    public Texture[] vertexTextures = (Texture[])vertexTextures?.Clone();
    public Texture[] fragmentTextures = (Texture[])fragmentTextures?.Clone();
    public SDLGPURendererBackend.StapleShaderUniform[] vertexUniformData = vertexUniformData;
    public SDLGPURendererBackend.StapleShaderUniform[] fragmentUniformData = fragmentUniformData;
    public SDLGPUShaderProgram program = program;

    internal static SDL.GPUBufferBinding[] vertexBinding = [new SDL.GPUBufferBinding()];
    internal static SDL.GPUBufferBinding indexBinding;
    internal static SDL.Rect scissor;

    public void Update(IRendererBackend rendererBackend)
    {
        var backend = (SDLGPURendererBackend)rendererBackend;
        var vertex = (SDLGPUVertexBuffer)state.vertexBuffer;
        var index = (SDLGPUIndexBuffer)state.indexBuffer;

        if (backend.TryGetVertexBuffer(vertex.handle, out var vertexBuffer) == false ||
            backend.TryGetIndexBuffer(index.handle, out var indexBuffer) == false ||
            backend.TryGetTextureSamplers(vertexTextures, fragmentTextures, state.shaderInstance,
                out var vertexSamplers, out var fragmentSamplers) == false)
        {
            return;
        }

        var hasVertexStorageBuffers = state.vertexStorageBuffers != null;
        var hasFragmentStorageBuffers = state.fragmentStorageBuffers != null;

        if (hasVertexStorageBuffers)
        {
            for (var i = 0; i < state.vertexStorageBuffers.Count; i++)
            {
                var binding = state.vertexStorageBuffers[i];

                if (binding.buffer.Disposed ||
                    binding.buffer is not SDLGPUVertexBuffer v ||
                    backend.TryGetVertexBuffer(v.handle, out var resource) == false ||
                    resource.used == false ||
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
                    backend.TryGetVertexBuffer(v.handle, out var resource) == false ||
                    resource.used == false ||
                    resource.buffer == nint.Zero)
                {
                    return;
                }
            }
        }

        var renderPass = backend.renderPass;

        if(renderPass == nint.Zero)
        {
            backend.ResumeRenderPass();

            renderPass = backend.renderPass;
        }

        if (SDLGPURendererBackend.lastGraphicsPipeline != pipeline)
        {
            SDLGPURendererBackend.lastGraphicsPipeline = pipeline;

            SDL.BindGPUGraphicsPipeline(renderPass, pipeline);
        }

        vertexBinding[0].Buffer = vertexBuffer.buffer;

        indexBinding.Buffer = indexBuffer.buffer;

        if (SDLGPURendererBackend.lastVertexBuffer != vertexBuffer.buffer)
        {
            SDLGPURendererBackend.lastVertexBuffer = vertexBuffer.buffer;

            SDL.BindGPUVertexBuffers(renderPass, 0, vertexBinding, 1);
        }

        if (SDLGPURendererBackend.lastIndexBuffer != indexBuffer.buffer)
        {
            SDLGPURendererBackend.lastIndexBuffer = indexBuffer.buffer;

            SDL.BindGPUIndexBuffer(renderPass, in indexBinding, index.Is32Bit ?
                SDL.GPUIndexElementSize.IndexElementSize32Bit :
                SDL.GPUIndexElementSize.IndexElementSize16Bit);
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

            var target = backend.frameAllocator.pinAddress + uniform.position;

            SDL.PushGPUVertexUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
        }

        for (var i = 0; i < fragmentUniformData.Length; i++)
        {
            var uniform = fragmentUniformData[i];

            var target = backend.frameAllocator.pinAddress + uniform.position;

            SDL.PushGPUFragmentUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
        }

        SDL.DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, (uint)(state.instanceCount > 1 ? state.instanceCount : 1),
            (uint)state.startIndex, state.startVertex, 0);
    }
}
