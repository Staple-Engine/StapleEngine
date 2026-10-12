using SDL;

namespace Staple.Internal;

internal unsafe class SDLGPURenderDynamicCommand(SDLGPURendererBackend backend, RenderState state, SDL_GPUGraphicsPipeline* pipeline,
    Texture[] vertexTextures, Texture[] fragmentTextures, int storageBufferBindingStart, (int, int) vertexUniformData,
    (int, int) fragmentUniformData, VertexAttribute[] vertexAttributes, int offset, int drawCount) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = MemoryUtils.SafeCloneArray(vertexTextures);
    private readonly Texture[] fragmentTextures = MemoryUtils.SafeCloneArray(fragmentTextures);

    public void Update()
    {
        SDLGPURenderCommand.Render(backend, in state, pipeline, state.staticMeshEntries, (SDLGPUVertexBuffer)state.vertexBuffer,
            (SDLGPUIndexBuffer)state.indexBuffer, state.indexBuffer.Is32Bit, vertexTextures, fragmentTextures, storageBufferBindingStart,
            vertexUniformData, fragmentUniformData, vertexAttributes, backend.dynamicIndirectCommandBuffer, offset, drawCount);
    }
}
