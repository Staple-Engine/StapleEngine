using SDL3;
using System;
using System.Collections.Generic;

namespace Staple.Internal;

internal class SDLGPURenderCommand(RenderState state, nint pipeline, SDL.SDL_GPUTextureSamplerBinding[] samplers,
    Dictionary<byte, byte[]> vertexUniformData, Dictionary<byte, byte[]> fragmentUniformData, SDLGPUShaderProgram program) : IRenderCommand
{
    public RenderState state = state;
    public nint pipeline = pipeline;
    public SDL.SDL_GPUTextureSamplerBinding[] samplers = samplers;
    public Dictionary<byte, byte[]> vertexUniformData = vertexUniformData;
    public Dictionary<byte, byte[]> fragmentUniformData = fragmentUniformData;
    public SDLGPUShaderProgram program = program;

    public void Update(IRendererBackend rendererBackend)
    {
        if (rendererBackend is not SDLGPURendererBackend backend ||
            state.shader == null ||
            program is not SDLGPUShaderProgram shader ||
            shader.Type != ShaderType.VertexFragment ||
            state.vertexBuffer is not SDLGPUVertexBuffer vertex ||
            backend.TryGetVertexBuffer(vertex.handle, out var vertexBuffer) == false ||
            vertex.layout is not SDLGPUVertexLayout vertexLayout ||
            state.indexBuffer is not SDLGPUIndexBuffer index ||
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

        SDL.SDL_BindGPUGraphicsPipeline(renderPass, pipeline);

        var vertexBinding = new SDL.SDL_GPUBufferBinding()
        {
            buffer = vertexBuffer.buffer,
        };

        var indexBinding = new SDL.SDL_GPUBufferBinding()
        {
            buffer = indexBuffer.buffer,
        };

        if (state.scissor != default)
        {
            var scissor = new SDL.SDL_Rect()
            {
                x = state.scissor.left,
                y = state.scissor.top,
                w = state.scissor.Width,
                h = state.scissor.Height,
            };

            SDL.SDL_SetGPUScissor(renderPass, in scissor);
        }

        SDL.SDL_BindGPUVertexBuffers(renderPass, 0, [vertexBinding], 1);

        SDL.SDL_BindGPUIndexBuffer(renderPass, in indexBinding, index.Is32Bit ?
            SDL.SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT :
            SDL.SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT);

        if (samplers != null)
        {
            SDL.SDL_BindGPUFragmentSamplers(renderPass, 0, samplers.AsSpan(), (uint)samplers.Length);
        }

        unsafe
        {
            foreach(var pair in vertexUniformData)
            {
                fixed (void* ptr = pair.Value)
                {
                    SDL.SDL_PushGPUVertexUniformData(backend.commandBuffer, pair.Key, (nint)ptr,
                        (uint)pair.Value.Length);
                }
            }

            foreach (var pair in fragmentUniformData)
            {
                fixed (void* ptr = pair.Value)
                {
                    SDL.SDL_PushGPUFragmentUniformData(backend.commandBuffer, pair.Key, (nint)ptr,
                        (uint)pair.Value.Length);
                }
            }
        }

        SDL.SDL_DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, 1,
            (uint)state.startIndex, state.startVertex, 0);
    }
}
