namespace Staple.Internal;

internal class SDLGPUDestroyIndexBufferCommand(ResourceHandle<IndexBuffer> handle) : IRenderCommand
{
    public void Update(IRendererBackend rendererBackend)
    {
        if (rendererBackend is not SDLGPURendererBackend backend ||
            !handle.IsValid ||
            !backend.TryGetIndexBuffer(handle, out var resource))
        {
            return;
        }

        backend.ReleaseBufferResource(resource);
    }
}
