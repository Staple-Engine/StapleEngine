using SDL3;
using System.Collections.Generic;

namespace Staple.Internal;

internal class SDLGPURenderTransientUIntCommand(RenderState state, nint pipeline,
    SDL.GPUTextureSamplerBinding[] samplers, SDLGPURendererBackend.StapleShaderUniform[] vertexUniformData,
    SDLGPURendererBackend.StapleShaderUniform[] fragmentUniformData, SDLGPUShaderProgram program, SDLGPURendererBackend.TransientEntry entry) :
    IRenderCommand
{
    public RenderState state = state;
    public nint pipeline = pipeline;
    public SDL.GPUTextureSamplerBinding[] samplers = samplers;
    public SDLGPURendererBackend.TransientEntry entry = entry;
    public SDLGPURendererBackend.StapleShaderUniform[] vertexUniformData = vertexUniformData;
    public SDLGPURendererBackend.StapleShaderUniform[] fragmentUniformData = fragmentUniformData;
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
            Buffer = entry.uintIndexBuffer,
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

        SDL.BindGPUIndexBuffer(renderPass, in indexBinding, SDL.GPUIndexElementSize.IndexElementSize32Bit);

        if (samplers != null)
        {
            SDL.BindGPUFragmentSamplers(renderPass, 0, samplers, (uint)samplers.Length);
        }

        if ((state.storageBuffers?.Length ?? 0) > 0)
        {
            var buffers = new List<nint>();

            foreach (var buffer in state.storageBuffers)
            {
                if (buffer.Item2 == null ||
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

            SDL.BindGPUVertexStorageBuffers(renderPass, 0, buffers.ToArray(), (uint)buffers.Count);
        }

        for (var i = 0; i < vertexUniformData.Length; i++)
        {
            var uniform = vertexUniformData[i];

            if (program.ShouldPushVertexUniform(uniform.binding, uniform.data) == false)
            {
                continue;
            }

            SDL.PushGPUVertexUniformData(backend.commandBuffer, uniform.binding, uniform.data, (uint)uniform.data.Length);
        }

        for (var i = 0; i < fragmentUniformData.Length; i++)
        {
            var uniform = fragmentUniformData[i];

            if (program.ShouldPushFragmentUniform(uniform.binding, uniform.data) == false)
            {
                continue;
            }

            SDL.PushGPUFragmentUniformData(backend.commandBuffer, uniform.binding, uniform.data, (uint)uniform.data.Length);
        }

        SDL.DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, 1, (uint)state.startIndex, state.startVertex, 0);
    }
}
