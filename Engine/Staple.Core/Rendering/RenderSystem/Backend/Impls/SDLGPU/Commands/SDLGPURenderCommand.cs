using SDL;

namespace Staple.Internal;

internal unsafe class SDLGPURenderCommand(SDLGPURendererBackend backend, RenderState state, SDL_GPUGraphicsPipeline *pipeline,
    Texture[] vertexTextures, Texture[] fragmentTextures, int storageBufferBindingStart, (int, int) vertexUniformData,
    (int, int) fragmentUniformData, VertexAttribute[] vertexAttributes) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = (Texture[])vertexTextures?.Clone();
    private readonly Texture[] fragmentTextures = (Texture[])fragmentTextures?.Clone();
    private readonly SDLGPUVertexBuffer vertex = (SDLGPUVertexBuffer)state.vertexBuffer;
    private readonly SDLGPUIndexBuffer index = (SDLGPUIndexBuffer)state.indexBuffer;
    private readonly BufferAttributeContainer.Entries staticMeshEntries = state.staticMeshEntries;

    internal static readonly SDL_GPUBufferBinding[] vertexBinding = new SDL_GPUBufferBinding[1];

    internal static readonly SDL_GPUBufferBinding[] staticMeshVertexBinding = new SDL_GPUBufferBinding[18];

    internal static SDL_GPUBufferBinding indexBinding;
    internal static SDL_Rect scissor;

    public void Update()
    {
        SDLGPURendererBackend.BufferResource vertexBuffer = null;
        SDLGPURendererBackend.BufferResource indexBuffer = null;

        if (staticMeshEntries == null)
        {
            if (!backend.TryGetVertexBuffer(vertex.handle, out vertexBuffer) ||
                !backend.TryGetIndexBuffer(index.handle, out indexBuffer))
            {
                return;
            }
        }
        else if (SDLGPURendererBackend.staticMeshVertexBuffers[0] == null)
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

                staticMeshVertexBinding[i].offset = (uint)(staticMeshEntries.positionEntry.start *
                    SDLGPURendererBackend.staticMeshVertexBuffersElementSize[bufferIndex]);

                staticMeshVertexBinding[i].buffer = SDLGPURendererBackend.staticMeshVertexBuffers[bufferIndex];
            }
        }

        var hasVertexStorageBuffers = state.vertexStorageBuffers != null;
        var hasFragmentStorageBuffers = state.fragmentStorageBuffers != null;

        var renderPass = backend.renderPass;

        if (renderPass == null)
        {
            backend.ResumeRenderPass();

            renderPass = backend.renderPass;
        }

        if (SDLGPURendererBackend.lastGraphicsPipeline != pipeline)
        {
            SDLGPURendererBackend.lastGraphicsPipeline = pipeline;

            SDL3.SDL_BindGPUGraphicsPipeline(renderPass, pipeline);
        }

        if (staticMeshEntries == null)
        {
            vertexBinding[0].buffer = vertexBuffer.buffer;

            indexBinding.offset = 0;
            indexBinding.buffer = indexBuffer.buffer;

            if (SDLGPURendererBackend.lastVertexBuffer != vertexBuffer.buffer)
            {
                SDLGPURendererBackend.lastVertexBuffer = vertexBuffer.buffer;

                fixed (SDL_GPUBufferBinding* b = SDLGPURenderCommand.vertexBinding)
                {
                    SDL3.SDL_BindGPUVertexBuffers(renderPass, 0, b, 1);
                }
            }

            if(SDLGPURendererBackend.lastIndexBuffer != indexBuffer.buffer)
            {
                SDLGPURendererBackend.lastIndexBuffer = indexBuffer.buffer;

                fixed (SDL_GPUBufferBinding* b = &indexBinding)
                {
                    SDL3.SDL_BindGPUIndexBuffer(renderPass, b, index.Is32Bit ?
                    SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT :
                    SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT);
                }
            }
        }
        else
        {
            indexBinding.offset = (uint)(staticMeshEntries.indicesEntry.start * sizeof(uint));
            indexBinding.buffer = SDLGPURendererBackend.staticMeshIndexBuffer;

            if(SDLGPURendererBackend.lastVertexBuffer != staticMeshVertexBinding[0].buffer)
            {
                SDLGPURendererBackend.lastVertexBuffer = staticMeshVertexBinding[0].buffer;

                fixed (SDL_GPUBufferBinding* b = staticMeshVertexBinding)
                {
                    SDL3.SDL_BindGPUVertexBuffers(renderPass, 0, b, (uint)vertexAttributes.Length);
                }
            }

            if (SDLGPURendererBackend.lastIndexBuffer != indexBinding.buffer)
            {
                SDLGPURendererBackend.lastIndexBuffer = indexBinding.buffer;

                fixed (SDL_GPUBufferBinding* b = &indexBinding)
                {
                    SDL3.SDL_BindGPUIndexBuffer(renderPass, b, SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT);
                }
            }
        }

        if (state.scissor != default)
        {
            scissor.x = state.scissor.left;
            scissor.y = state.scissor.top;
            scissor.w = state.scissor.Width;
            scissor.h = state.scissor.Height;
        }
        else
        {
            scissor.x = scissor.y = 0;
            scissor.w = backend.renderSize.X;
            scissor.h = backend.renderSize.Y;
        }

        fixed (SDL_Rect* r = &scissor)
        {
            SDL3.SDL_SetGPUScissor(renderPass, r);
        }

        if (vertexSamplers.IsEmpty == false)
        {
            fixed(SDL_GPUTextureSamplerBinding *ptr = vertexSamplers)
            {
                SDL3.SDL_BindGPUVertexSamplers(renderPass, 0, ptr, (uint)vertexSamplers.Length);
            }
        }

        if (fragmentSamplers.IsEmpty == false)
        {
            fixed(SDL_GPUTextureSamplerBinding* ptr = fragmentSamplers)
            {
                SDL3.SDL_BindGPUFragmentSamplers(renderPass, 0, ptr, (uint)fragmentSamplers.Length);
            }
        }

        var buffers = backend.bufferStaging;
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
                    resource.buffer == null)
                {
                    return;
                }

                buffers[counter++] = resource.buffer;
            }
        }

        fixed (SDL_GPUBuffer** ptr = buffers)
        {
            SDL3.SDL_BindGPUVertexStorageBuffers(renderPass, (uint)storageBufferBindingStart, ptr, (uint)counter);
        }

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
                    resource.buffer == null)
                {
                    return;
                }

                buffers[counter++] = resource.buffer;

                if (firstBinding < 0)
                {
                    firstBinding = binding;
                }
            }

            fixed (SDL_GPUBuffer** ptr = buffers)
            {
                SDL3.SDL_BindGPUFragmentStorageBuffers(renderPass, (uint)firstBinding, ptr, (uint)counter);
            }
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

            SDL3.SDL_PushGPUVertexUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
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

            SDL3.SDL_PushGPUFragmentUniformData(backend.commandBuffer, uniform.binding, target, (uint)uniform.size);
        }

        SDL3.SDL_DrawGPUIndexedPrimitives(renderPass, (uint)state.indexCount, (uint)(state.instanceCount > 1 ?
            state.instanceCount : 1), (uint)state.startIndex, state.startVertex, 0);
    }
}
