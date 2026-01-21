using SDL3;

namespace Staple.Internal;

internal class SDLGPURenderCommand(SDLGPURendererBackend backend, RenderState state, nint pipeline, Texture[] vertexTextures,
    Texture[] fragmentTextures, int storageBufferBindingStart, (int, int) vertexUniformData, (int, int) fragmentUniformData,
    VertexAttribute[] vertexAttributes) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = (Texture[])vertexTextures?.Clone();
    private readonly Texture[] fragmentTextures = (Texture[])fragmentTextures?.Clone();
    private readonly SDLGPUVertexBuffer vertex = (SDLGPUVertexBuffer)state.vertexBuffer;
    private readonly SDLGPUIndexBuffer index = (SDLGPUIndexBuffer)state.indexBuffer;
    private readonly BufferAttributeContainer.Entries staticMeshEntries = state.staticMeshEntries;

    internal static readonly SDL.GPUBufferBinding[] vertexBinding = new SDL.GPUBufferBinding[1];

    internal static readonly SDL.GPUBufferBinding[] staticMeshVertexBinding = new SDL.GPUBufferBinding[18];

    internal static SDL.GPUBufferBinding indexBinding;
    internal static SDL.Rect scissor;

    public void Update()
    {
        SDLGPURendererBackend.BufferResource vertexBuffer = null;
        SDLGPURendererBackend.BufferResource indexBuffer = null;

        if(staticMeshEntries == null)
        {
            if(!backend.TryGetVertexBuffer(vertex.handle, out vertexBuffer) ||
                !backend.TryGetIndexBuffer(index.handle, out indexBuffer))
            {
                return;
            }
        }
        else if(SDLGPURendererBackend.staticMeshVertexBuffers[0] == nint.Zero)
        {
            return;
        }

        if (!backend.TryGetTextureSamplers(vertexTextures, fragmentTextures, state.shaderInstance, out var vertexSamplers,
            out var fragmentSamplers))
        {
            return;
        }

        if (staticMeshEntries != null)
        {
            for (var i = 0; i < vertexAttributes.Length; i++)
            {
                var bufferIndex = BufferAttributeContainer.BufferIndex(vertexAttributes[i]);

                if(bufferIndex < 0)
                {
                    return;
                }

                staticMeshVertexBinding[i].Offset = (uint)(staticMeshEntries.positionEntry.start * 
                    SDLGPURendererBackend.staticMeshVertexBuffersElementSize[bufferIndex]);
                staticMeshVertexBinding[i].Buffer = SDLGPURendererBackend.staticMeshVertexBuffers[bufferIndex];
            }
        }

        var hasVertexStorageBuffers = state.vertexStorageBuffers != null;
        var hasFragmentStorageBuffers = state.fragmentStorageBuffers != null;

        var renderPass = backend.renderPass;

        if (renderPass == nint.Zero)
        {
            backend.ResumeRenderPass();

            renderPass = backend.renderPass;
        }

        if (SDLGPURendererBackend.lastGraphicsPipeline != pipeline)
        {
            SDLGPURendererBackend.lastGraphicsPipeline = pipeline;

            SDL.BindGPUGraphicsPipeline(renderPass, pipeline);
        }

        if (staticMeshEntries == null)
        {
            vertexBinding[0].Buffer = vertexBuffer.buffer;

            indexBinding.Offset = 0;
            indexBinding.Buffer = indexBuffer.buffer;

            if (SDLGPURendererBackend.lastVertexBuffer != vertexBuffer.buffer)
            {
                SDLGPURendererBackend.lastVertexBuffer = vertexBuffer.buffer;

                SDL.BindGPUVertexBuffers(renderPass, 0, vertexBinding, 1);
            }

            if(SDLGPURendererBackend.lastIndexBuffer != indexBuffer.buffer)
            {
                SDLGPURendererBackend.lastIndexBuffer = indexBuffer.buffer;

                SDL.BindGPUIndexBuffer(renderPass, in indexBinding, index.Is32Bit ?
                    SDL.GPUIndexElementSize.IndexElementSize32Bit :
                    SDL.GPUIndexElementSize.IndexElementSize16Bit);
            }
        }
        else
        {
            indexBinding.Offset = (uint)(staticMeshEntries.indicesEntry.start * sizeof(uint));
            indexBinding.Buffer = SDLGPURendererBackend.staticMeshIndexBuffer;

            if(SDLGPURendererBackend.lastVertexBuffer != staticMeshVertexBinding[0].Buffer)
            {
                SDLGPURendererBackend.lastVertexBuffer = staticMeshVertexBinding[0].Buffer;

                SDL.BindGPUVertexBuffers(renderPass, 0, staticMeshVertexBinding, (uint)vertexAttributes.Length);
            }

            if (SDLGPURendererBackend.lastIndexBuffer != indexBinding.Buffer)
            {
                SDLGPURendererBackend.lastIndexBuffer = indexBinding.Buffer;

                SDL.BindGPUIndexBuffer(renderPass, in indexBinding, SDL.GPUIndexElementSize.IndexElementSize32Bit);
            }
        }

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

        if (vertexSamplers.IsEmpty == false)
        {
            unsafe
            {
                fixed(void *ptr = vertexSamplers)
                {
                    SDL.BindGPUVertexSamplers(renderPass, 0, (nint)ptr, (uint)vertexSamplers.Length);
                }
            }

        }

        if (fragmentSamplers.IsEmpty == false)
        {
            unsafe
            {
                fixed (void* ptr = fragmentSamplers)
                {
                    SDL.BindGPUFragmentSamplers(renderPass, 0, (nint)ptr, (uint)fragmentSamplers.Length);
                }
            }
        }

        var buffers = backend.nintBufferStaging;
        var counter = 2;

        buffers[0] = SDLGPURendererBackend.entityTransformsBuffer;
        buffers[1] = SDLGPURendererBackend.entityTransformIndexBuffer;

        if (hasVertexStorageBuffers)
        {
            if (state.vertexStorageBuffers.Count > buffers.Length)
            {
                return;
            }

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
            }
        }

        SDL.BindGPUVertexStorageBuffers(renderPass, (uint)storageBufferBindingStart, buffers, (uint)counter);

        if (hasFragmentStorageBuffers)
        {
            counter = 0;

            var firstBinding = -1;

            if (state.fragmentStorageBuffers.Count > buffers.Length)
            {
                return;
            }

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

            SDL.BindGPUFragmentStorageBuffers(renderPass, (uint)firstBinding, buffers, (uint)counter);
        }

        var vertexSpan = backend.shaderUniformFrameAllocator.GetSpan(vertexUniformData.Item1, vertexUniformData.Item2);

        for (var i = 0; i < vertexSpan.Length; i++)
        {
            var uniform = vertexSpan[i];

            if(uniform.used == false)
            {
                continue;
            }

            var target = backend.frameAllocator.Get(uniform.position);

            SDL.PushGPUVertexUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
        }

        var fragmentSpan = backend.shaderUniformFrameAllocator.GetSpan(fragmentUniformData.Item1, fragmentUniformData.Item2);

        for (var i = 0; i < fragmentSpan.Length; i++)
        {
            var uniform = fragmentSpan[i];

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
