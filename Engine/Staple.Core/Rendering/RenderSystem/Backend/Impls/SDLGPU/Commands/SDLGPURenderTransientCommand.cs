using SDL;
using System;

namespace Staple.Internal;

internal unsafe class SDLGPURenderTransientCommand(SDLGPURendererBackend backend, RenderState state,
    SDL_GPUGraphicsPipeline *pipeline, Texture[] vertexTextures, Texture[] fragmentTextures, int storageBufferBindingStart,
    (int, int) vertexUniformData, (int, int) fragmentUniformData, SDLGPURendererBackend.TransientEntry entry) : IRenderCommand
{
    private readonly RenderState state = state.Clone();
    private readonly Texture[] vertexTextures = MemoryUtils.SafeCloneArray(vertexTextures);
    private readonly Texture[] fragmentTextures = MemoryUtils.SafeCloneArray(fragmentTextures);

    public void Update()
    {
        SDLGPURenderCommand.Render(backend, in state, pipeline, null, entry.vertexBuffer, entry.indexBuffer, false, vertexTextures.AsSpan(),
            fragmentTextures.AsSpan(), storageBufferBindingStart, vertexUniformData, fragmentUniformData, default);
    }
}
