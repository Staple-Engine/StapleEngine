using SDL3;
using System;
using System.Collections.Generic;

namespace Staple.Internal;

internal class SDLGPURenderTransientCommand(RenderState state, nint pipeline, Texture[] vertexTextures, Texture[] fragmentTextures,
    SDLGPURendererBackend.StapleShaderUniform[] vertexUniformData, SDLGPURendererBackend.StapleShaderUniform[] fragmentUniformData,
    SDLGPUShaderProgram program, SDLGPURendererBackend.TransientEntry entry) : IRenderCommand
{
    public RenderState state = state.Clone();
    public nint pipeline = pipeline;
    public SDLGPURendererBackend.TransientEntry entry = entry;
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

        if (entry.vertexBuffer == nint.Zero ||
            backend.TryGetTextureSamplers(vertexTextures, fragmentTextures, state.shaderInstance,
                out var vertexSamplers, out var fragmentSamplers) == false)
        {
            return;
        }

        if ((state.vertexStorageBuffers?.Count ?? 0) > 0)
        {
            foreach (var pair in state.vertexStorageBuffers)
            {
                if (pair.Value == null ||
                    pair.Value.Flags.HasFlag(RenderBufferFlags.GraphicsRead) == false ||
                    pair.Value.Disposed ||
                    pair.Value is not SDLGPUVertexBuffer v ||
                    backend.TryGetVertexBuffer(v.handle, out var resource) == false ||
                    resource.used == false ||
                    resource.buffer == nint.Zero)
                {
                    return;
                }
            }
        }

        if ((state.fragmentStorageBuffers?.Count ?? 0) > 0)
        {
            foreach (var pair in state.fragmentStorageBuffers)
            {
                if (pair.Value == null ||
                    pair.Value.Flags.HasFlag(RenderBufferFlags.GraphicsRead) == false ||
                    pair.Value.Disposed ||
                    pair.Value is not SDLGPUVertexBuffer v ||
                    backend.TryGetVertexBuffer(v.handle, out var resource) == false ||
                    resource.used == false ||
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

        SDL.BindGPUGraphicsPipeline(renderPass, pipeline);

        if (state.scissor != default)
        {
            scissor.X = state.scissor.left;
            scissor.Y = state.scissor.top;
            scissor.W = state.scissor.Width;
            scissor.H = state.scissor.Height;
        }
        else
        {
            scissor.X = scissor.Y = 0;
            scissor.W = backend.renderSize.X;
            scissor.H = backend.renderSize.Y;
        }

        SDL.SetGPUScissor(renderPass, in scissor);

        vertexBinding[0].Buffer = entry.vertexBuffer;

        indexBinding.Buffer = entry.indexBuffer;

        if (SDLGPURendererBackend.lastVertexBuffer != entry.vertexBuffer)
        {
            SDLGPURendererBackend.lastVertexBuffer = entry.vertexBuffer;

            SDL.BindGPUVertexBuffers(renderPass, 0, vertexBinding, 1);
        }

        if (SDLGPURendererBackend.lastIndexBuffer != entry.indexBuffer)
        {
            SDLGPURendererBackend.lastIndexBuffer = entry.indexBuffer;

            SDL.BindGPUIndexBuffer(renderPass, in indexBinding, SDL.GPUIndexElementSize.IndexElementSize16Bit);
        }

        if (vertexSamplers != null)
        {
            SDL.BindGPUVertexSamplers(renderPass, 0, vertexSamplers, (uint)vertexSamplers.Length);
        }

        if (fragmentSamplers != null)
        {
            SDL.BindGPUFragmentSamplers(renderPass, 0, fragmentSamplers, (uint)fragmentSamplers.Length);
        }

        var singleBuffer = new nint[1];

        if ((state.vertexStorageBuffers?.Count ?? 0) > 0)
        {
            foreach (var pair in state.vertexStorageBuffers)
            {
                var buffer = pair.Value as SDLGPUVertexBuffer;

                backend.TryGetVertexBuffer(buffer.handle, out var resource);

                singleBuffer[0] = resource.buffer;

                SDL.BindGPUVertexStorageBuffers(renderPass, (uint)pair.Key, singleBuffer, 1);
            }
        }

        if ((state.fragmentStorageBuffers?.Count ?? 0) > 0)
        {
            foreach (var pair in state.fragmentStorageBuffers)
            {
                var buffer = pair.Value as SDLGPUVertexBuffer;

                backend.TryGetVertexBuffer(buffer.handle, out var resource);

                singleBuffer[0] = resource.buffer;

                SDL.BindGPUFragmentStorageBuffers(renderPass, (uint)pair.Key, singleBuffer, 1);
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
