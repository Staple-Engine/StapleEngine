using SDL3;
using System.Collections.Generic;

namespace Staple.Internal;

internal class SDLGPURenderCommand(RenderState state, nint pipeline, SDL.GPUTextureSamplerBinding[] vertexSamplers,
    SDL.GPUTextureSamplerBinding[] fragmentSamplers, List<SDLGPURendererBackend.StapleShaderUniform> vertexUniformData,
    List<SDLGPURendererBackend.StapleShaderUniform> fragmentUniformData, SDLGPUShaderProgram program) : IRenderCommand
{
    public RenderState state = state;
    public nint pipeline = pipeline;
    public SDL.GPUTextureSamplerBinding[] vertexSamplers = vertexSamplers;
    public SDL.GPUTextureSamplerBinding[] fragmentSamplers = fragmentSamplers;
    public List<SDLGPURendererBackend.StapleShaderUniform> vertexUniformData = vertexUniformData;
    public List<SDLGPURendererBackend.StapleShaderUniform> fragmentUniformData = fragmentUniformData;
    public SDLGPUShaderProgram program = program;

    public void Update(IRendererBackend rendererBackend)
    {
        var backend = (SDLGPURendererBackend)rendererBackend;
        var shaderInstance = state.shader.instances.TryGetValue(state.shaderVariant, out var sv) ? sv : null;
        var vertex = (SDLGPUVertexBuffer)state.vertexBuffer;
        var index = (SDLGPUIndexBuffer)state.indexBuffer;

        if (backend.TryGetVertexBuffer(vertex.handle, out var vertexBuffer) == false ||
            backend.TryGetIndexBuffer(index.handle, out var indexBuffer) == false)
        {
            return;
        }

        var renderPass = backend.renderPass;

        if(renderPass == nint.Zero)
        {
            backend.ResumeRenderPass();

            renderPass = backend.renderPass;
        }

        SDL.BindGPUGraphicsPipeline(renderPass, pipeline);

        var vertexBinding = new SDL.GPUBufferBinding()
        {
            Buffer = vertexBuffer.buffer,
        };

        var indexBinding = new SDL.GPUBufferBinding()
        {
            Buffer = indexBuffer.buffer,
        };

        if (state.scissor != default)
        {
            var scissor = new SDL.Rect()
            {
                X = state.scissor.left,
                Y = state.scissor.top,
                W = state.scissor.Width,
                H = state.scissor.Height,
            };

            SDL.SetGPUScissor(renderPass, in scissor);
        }

        SDL.BindGPUVertexBuffers(renderPass, 0, [vertexBinding], 1);

        SDL.BindGPUIndexBuffer(renderPass, in indexBinding, index.Is32Bit ?
            SDL.GPUIndexElementSize.IndexElementSize32Bit :
            SDL.GPUIndexElementSize.IndexElementSize16Bit);

        if (vertexSamplers != null)
        {
            SDL.BindGPUVertexSamplers(renderPass, 0, vertexSamplers, (uint)vertexSamplers.Length);
        }

        if (fragmentSamplers != null)
        {
            SDL.BindGPUFragmentSamplers(renderPass, 0, fragmentSamplers, (uint)fragmentSamplers.Length);
        }

        if((state.storageBuffers?.Length ?? 0) > 0)
        {
            var buffers = new List<nint>();

            foreach(var buffer in state.storageBuffers)
            {
                if(buffer.Item2 == null ||
                    buffer.Item2.Flags.HasFlag(RenderBufferFlags.GraphicsRead) == false ||
                    buffer.Item2.Disposed ||
                    buffer.Item2 is not SDLGPUVertexBuffer v ||
                    backend.TryGetVertexBuffer(v.handle, out var resource) == false ||
                    resource.used == false ||
                    resource.buffer == nint.Zero)
                {
                    continue;
                }

                buffers.Add(resource.buffer);
            }

            if(buffers.Count > 0)
            {
                var bufferArray = buffers.ToArray();

                SDL.BindGPUVertexStorageBuffers(renderPass,
                    (uint)(shaderInstance.vertexShaderMetrics.samplerCount + shaderInstance.vertexShaderMetrics.storageTextureCount),
                    bufferArray, (uint)buffers.Count);
            }
        }

        for (var i = 0; i < vertexUniformData.Count; i++)
        {
            var uniform = vertexUniformData[i];
            var span = backend.frameAllocator.Get(uniform.position, uniform.size);

            unsafe
            {
                fixed (void* ptr = span)
                {
                    SDL.PushGPUVertexUniformData(backend.commandBuffer, uniform.binding, (nint)ptr, (uint)uniform.size);
                }
            }
        }

        for (var i = 0; i < fragmentUniformData.Count; i++)
        {
            var uniform = fragmentUniformData[i];
            var span = backend.frameAllocator.Get(uniform.position, uniform.size);

            unsafe
            {
                fixed (void* ptr = span)
                {
                    SDL.PushGPUFragmentUniformData(backend.commandBuffer, uniform.binding, (nint)ptr, (uint)uniform.size);
                }
            }
        }

        SDL.DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, 1, (uint)state.startIndex, state.startVertex, 0);
    }
}
