using SDL3;
using System;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPURenderCommand(RenderState state, nint pipeline, SDL.SDL_GPUTextureSamplerBinding[] samplers) : IRenderCommand
{
    public RenderState state = state;
    public nint pipeline = pipeline;
    public SDL.SDL_GPUTextureSamplerBinding[] samplers = samplers;

    public void Update(IRendererBackend rendererBackend)
    {
        if (rendererBackend is not SDLGPURendererBackend backend ||
            state.program is not SDLGPUShaderProgram shader ||
            shader.Type != ShaderType.VertexFragment ||
            state.vertexBuffer is not SDLGPUVertexBuffer vertex ||
            vertex.layout is not SDLGPUVertexLayout vertexLayout ||
            state.indexBuffer is not SDLGPUIndexBuffer index)
        {
            return;
        }

        var renderPass = backend.renderPass;

        if(renderPass == nint.Zero)
        {
            throw new InvalidOperationException("Can't render without a render pass!");
        }

        SDL.SDL_BindGPUGraphicsPipeline(renderPass, pipeline);

        var vertexBinding = new SDL.SDL_GPUBufferBinding()
        {
            buffer = vertex.buffer,
        };

        var indexBinding = new SDL.SDL_GPUBufferBinding()
        {
            buffer = index.buffer,
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

        SDL.SDL_BindGPUIndexBuffer(renderPass, in indexBinding, index.Is32Bit ?
            SDL.SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT :
            SDL.SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT);

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
        }

        SDL.SDL_DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, 1,
            (uint)state.startIndex, state.startVertex, 0);
    }
}
