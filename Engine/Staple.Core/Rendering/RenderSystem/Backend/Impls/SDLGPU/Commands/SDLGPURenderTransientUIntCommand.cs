using SDL3;
using System.Collections.Generic;

namespace Staple.Internal;

internal class SDLGPURenderTransientUIntCommand(RenderState state, nint pipeline, Texture[] vertexTextures, Texture[] fragmentTextures,
    List<SDLGPURendererBackend.StapleShaderUniform> vertexUniformData, List<SDLGPURendererBackend.StapleShaderUniform> fragmentUniformData,
    SDLGPUShaderProgram program, SDLGPURendererBackend.TransientEntry entry) : IRenderCommand
{
    public RenderState state = state.Clone();
    public nint pipeline = pipeline;
    public SDLGPURendererBackend.TransientEntry entry = entry;
    public Texture[] vertexTextures = (Texture[])vertexTextures?.Clone();
    public Texture[] fragmentTextures = (Texture[])fragmentTextures?.Clone();
    public List<SDLGPURendererBackend.StapleShaderUniform> vertexUniformData = vertexUniformData;
    public List<SDLGPURendererBackend.StapleShaderUniform> fragmentUniformData = fragmentUniformData;
    public SDLGPUShaderProgram program = program;

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

        for (var i = 0; i < vertexUniformData.Count; i++)
        {
            var uniform = vertexUniformData[i];
            var span = backend.frameAllocator.Get(uniform.position, uniform.size);

            unsafe
            {
                fixed(void *ptr = span)
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
