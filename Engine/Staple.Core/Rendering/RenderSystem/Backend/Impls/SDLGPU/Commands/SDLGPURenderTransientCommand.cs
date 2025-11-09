using SDL3;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPURenderTransientCommand(RenderState state, nint pipeline,
    SDL.SDL_GPUTextureSamplerBinding[] samplers, Dictionary<byte, byte[]> uniformData,
    SDLGPUShaderProgram program, SDLGPURendererBackend.TransientEntry entry) : IRenderCommand
{
    public RenderState state = state;
    public nint pipeline = pipeline;
    public SDL.SDL_GPUTextureSamplerBinding[] samplers = samplers;
    public SDLGPURendererBackend.TransientEntry entry = entry;
    public Dictionary<byte, byte[]> uniformData = uniformData;
    public SDLGPUShaderProgram program = program;

    public void Update(IRendererBackend rendererBackend)
    {
        if (rendererBackend is not SDLGPURendererBackend backend ||
            state.shader == null ||
            program is not SDLGPUShaderProgram shader ||
            shader.Type != ShaderType.VertexFragment ||
            entry.vertexBuffer == nint.Zero)
        {
            return;
        }

        var renderPass = backend.renderPass;

        if (renderPass == nint.Zero)
        {
            backend.ResumeRenderPass();

            renderPass = backend.renderPass;
        }

        SDL.SDL_BindGPUGraphicsPipeline(renderPass, pipeline);

        var vertexBinding = new SDL.SDL_GPUBufferBinding()
        {
            buffer = entry.vertexBuffer,
        };

        var indexBinding = new SDL.SDL_GPUBufferBinding()
        {
            buffer = entry.indexBuffer,
        };

        var scissor = new SDL.SDL_Rect();

        if (state.scissor != default)
        {
            scissor = new()
            {
                x = state.scissor.left,
                y = state.scissor.top,
                w = state.scissor.Width,
                h = state.scissor.Height,
            };
        }
        else
        {
            scissor = new()
            {
                w = backend.renderSize.X,
                h = backend.renderSize.Y,
            };
        }

        SDL.SDL_SetGPUScissor(renderPass, in scissor);

        SDL.SDL_BindGPUVertexBuffers(renderPass, 0, [vertexBinding], 1);

        SDL.SDL_BindGPUIndexBuffer(renderPass, in indexBinding, SDL.SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT);

        if (samplers != null)
        {
            SDL.SDL_BindGPUFragmentSamplers(renderPass, 0, samplers.AsSpan(), (uint)samplers.Length);
        }

        backend.viewData.renderData.world = state.world;

        unsafe
        {
            fixed (void* ptr = &backend.viewData.renderData)
            {
                SDL.SDL_PushGPUVertexUniformData(backend.commandBuffer, 0, (nint)ptr,
                    (uint)Marshal.SizeOf<SDLGPURendererBackend.StapleRenderData>());
            }

            foreach (var pair in uniformData)
            {
                fixed (void* ptr = pair.Value)
                {
                    SDL.SDL_PushGPUVertexUniformData(backend.commandBuffer, pair.Key, (nint)ptr,
                        (uint)pair.Value.Length);
                }
            }
        }

        SDL.SDL_DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, 1,
            (uint)state.startIndex, state.startVertex, 0);
    }
}
