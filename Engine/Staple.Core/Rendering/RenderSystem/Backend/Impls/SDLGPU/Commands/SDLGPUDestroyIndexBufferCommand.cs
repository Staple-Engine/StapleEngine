namespace Staple.Internal;

internal class SDLGPUDestroyIndexBufferCommand(ResourceHandle<IndexBuffer> handle) : IRenderCommand
{
    public ResourceHandle<IndexBuffer> handle = handle;

    public void Update(IRendererBackend rendererBackend)
    {
        if (rendererBackend is not SDLGPURendererBackend backend ||
            handle.IsValid == false ||
            backend.TryGetIndexBuffer(handle, out var resource) == false)
        {
            return;
        }

        backend.ReleaseBufferResource(resource);
    }
}
