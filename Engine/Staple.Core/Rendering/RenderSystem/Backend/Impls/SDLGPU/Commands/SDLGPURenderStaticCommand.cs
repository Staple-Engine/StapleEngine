using SDL;

namespace Staple.Internal;

internal unsafe class SDLGPURenderStaticCommand(SDLGPURendererBackend backend, RenderState state, SDL_GPUGraphicsPipeline *pipeline,
    Texture[] vertexTextures, Texture[] fragmentTextures, int storageBufferBindingStart, (int, int) vertexUniformData,
    (int, int) fragmentUniformData, VertexAttribute[] vertexAttributes, int offset, int drawCount) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = MemoryUtils.SafeCloneArray(vertexTextures);
    private readonly Texture[] fragmentTextures = MemoryUtils.SafeCloneArray(fragmentTextures);

    public void Update()
    {
        if (SDLGPURendererBackend.staticMeshVertexBuffers[0] == null ||
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

            SDLGPURenderCommand.staticMeshVertexBinding[i].offset = 0;
            SDLGPURenderCommand.staticMeshVertexBinding[i].buffer = SDLGPURendererBackend.staticMeshVertexBuffers[bufferIndex];
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

        SDLGPURenderCommand.indexBinding.offset = 0;
        SDLGPURenderCommand.indexBinding.buffer = SDLGPURendererBackend.staticMeshIndexBuffer;

        if (SDLGPURendererBackend.lastVertexBuffer != SDLGPURenderCommand.staticMeshVertexBinding[0].buffer ||
            !SDLGPURendererBackend.ValidVertexAttributes(vertexAttributes))
        {
            SDLGPURendererBackend.lastVertexBuffer = SDLGPURenderCommand.staticMeshVertexBinding[0].buffer;

            fixed (SDL_GPUBufferBinding* b = SDLGPURenderCommand.staticMeshVertexBinding)
            {
                SDL3.SDL_BindGPUVertexBuffers(renderPass, 0, b, (uint)vertexAttributes.Length);
            }
        }

        if (SDLGPURendererBackend.lastIndexBuffer != SDLGPURenderCommand.indexBinding.buffer)
        {
            SDLGPURendererBackend.lastIndexBuffer = SDLGPURenderCommand.indexBinding.buffer;

            fixed (SDL_GPUBufferBinding* b = &SDLGPURenderCommand.indexBinding)
            {
                SDL3.SDL_BindGPUIndexBuffer(renderPass, b, SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT);
            }
        }

        SDLGPURenderCommand.SetupRenderState(backend, in state, vertexSamplers, fragmentSamplers, vertexUniformData, fragmentUniformData,
            hasVertexStorageBuffers, hasFragmentStorageBuffers, SDLGPURendererBackend.entityTransformIndexBuffer);

        SDL3.SDL_DrawGPUIndexedPrimitivesIndirect(renderPass, backend.indirectCommandBuffer,
            (uint)(offset * SDLGPURendererBackend.IndirectDrawCommandSize), (uint)drawCount);
    }
}
