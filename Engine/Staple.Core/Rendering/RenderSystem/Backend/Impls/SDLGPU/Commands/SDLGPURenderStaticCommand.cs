using SDL3;

namespace Staple.Internal;

internal class SDLGPURenderStaticCommand(SDLGPURendererBackend backend, RenderState state, nint pipeline, Texture[] vertexTextures,
    Texture[] fragmentTextures, int storageBufferBindingStart, (int, int) vertexUniformData, (int, int) fragmentUniformData,
    VertexAttribute[] vertexAttributes, int offset, int drawCount) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = (Texture[])vertexTextures?.Clone();
    private readonly Texture[] fragmentTextures = (Texture[])fragmentTextures?.Clone();

    public void Update()
    {
        if (SDLGPURendererBackend.staticMeshVertexBuffers[0] == nint.Zero ||
            !backend.TryGetTextureSamplers(vertexTextures, fragmentTextures, state.shaderInstance, out var vertexSamplers,
            out var fragmentSamplers))
        {
            return;
        }

        for (var i = 0; i < vertexAttributes.Length; i++)
        {
            var bufferIndex = BufferAttributeContainer.BufferIndex(vertexAttributes[i]);

            if (bufferIndex < 0)
            {
                return;
            }

            SDLGPURenderCommand.staticMeshVertexBinding[i].Offset = 0;
            SDLGPURenderCommand.staticMeshVertexBinding[i].Buffer = SDLGPURendererBackend.staticMeshVertexBuffers[bufferIndex];
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

        SDLGPURenderCommand.indexBinding.Offset = 0;
        SDLGPURenderCommand.indexBinding.Buffer = SDLGPURendererBackend.staticMeshIndexBuffer;

        if (SDLGPURendererBackend.lastVertexBuffer != SDLGPURenderCommand.staticMeshVertexBinding[0].Buffer)
        {
            SDLGPURendererBackend.lastVertexBuffer = SDLGPURenderCommand.staticMeshVertexBinding[0].Buffer;

            SDL.BindGPUVertexBuffers(renderPass, 0, SDLGPURenderCommand.staticMeshVertexBinding, (uint)vertexAttributes.Length);
        }

        if (SDLGPURendererBackend.lastIndexBuffer != SDLGPURenderCommand.indexBinding.Buffer)
        {
            SDLGPURendererBackend.lastIndexBuffer = SDLGPURenderCommand.indexBinding.Buffer;

            SDL.BindGPUIndexBuffer(renderPass, in SDLGPURenderCommand.indexBinding, SDL.GPUIndexElementSize.IndexElementSize32Bit);
        }

        if (vertexSamplers.IsEmpty == false)
        {
            unsafe
            {
                fixed (void* ptr = vertexSamplers)
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

            if (uniform.used == false)
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

        SDL.DrawGPUIndexedPrimitivesIndirect(renderPass, backend.indirectCommandBuffer,
            (uint)(offset * SDLGPURendererBackend.IndirectDrawCommandSize), (uint)drawCount);
    }
}
