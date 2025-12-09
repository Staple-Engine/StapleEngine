using SDL3;
using System;
using System.Collections.Generic;

namespace Staple.Internal;

internal class SDLGPURenderTransientCommand(RenderState state, nint pipeline,
    SDL.GPUTextureSamplerBinding[] samplers, Dictionary<byte, byte[]> vertexUniformData, Dictionary<byte, byte[]> fragmentUniformData,
    SDLGPUShaderProgram program, SDLGPURendererBackend.TransientEntry entry) : IRenderCommand
{
    public RenderState state = state;
    public nint pipeline = pipeline;
    public SDL.GPUTextureSamplerBinding[] samplers = samplers;
    public SDLGPURendererBackend.TransientEntry entry = entry;
    public Dictionary<byte, byte[]> vertexUniformData = vertexUniformData;
    public Dictionary<byte, byte[]> fragmentUniformData = fragmentUniformData;
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

        SDL.BindGPUGraphicsPipeline(renderPass, pipeline);

        var vertexBinding = new SDL.GPUBufferBinding()
        {
            Buffer = entry.vertexBuffer,
        };

        var indexBinding = new SDL.GPUBufferBinding()
        {
            Buffer = entry.indexBuffer,
        };

        var scissor = new SDL.Rect();

        if (state.scissor != default)
        {
            scissor = new()
            {
                X = state.scissor.left,
                Y = state.scissor.top,
                W = state.scissor.Width,
                H = state.scissor.Height,
            };
        }
        else
        {
            scissor = new()
            {
                W = backend.renderSize.X,
                H = backend.renderSize.Y,
            };
        }

        SDL.SetGPUScissor(renderPass, in scissor);

        SDL.BindGPUVertexBuffers(renderPass, 0, [vertexBinding], 1);

        SDL.BindGPUIndexBuffer(renderPass, in indexBinding, SDL.GPUIndexElementSize.IndexElementSize16Bit);

        if (samplers != null)
        {
            SDL.BindGPUFragmentSamplers(renderPass, 0, samplers, (uint)samplers.Length);
        }

        unsafe
        {
            foreach (var pair in vertexUniformData)
            {
                fixed (void* ptr = pair.Value)
                {
                    SDL.PushGPUVertexUniformData(backend.commandBuffer, pair.Key, (nint)ptr, (uint)pair.Value.Length);
                }
            }

            foreach (var pair in fragmentUniformData)
            {
                fixed (void* ptr = pair.Value)
                {
                    SDL.PushGPUFragmentUniformData(backend.commandBuffer, pair.Key, (nint)ptr, (uint)pair.Value.Length);
                }
            }
        }

        SDL.DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, 1, (uint)state.startIndex, state.startVertex, 0);
    }
}
