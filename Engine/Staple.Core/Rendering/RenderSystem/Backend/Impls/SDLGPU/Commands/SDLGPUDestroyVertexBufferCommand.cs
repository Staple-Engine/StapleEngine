namespace Staple.Internal;

internal class SDLGPUDestroyVertexBufferCommand(ResourceHandle<VertexBuffer> handle) : IRenderCommand
{
    public ResourceHandle<VertexBuffer> handle = handle;

    public void Update(IRendererBackend rendererBackend)
    {
        if(rendererBackend is not SDLGPURendererBackend backend ||
            handle.IsValid == false ||
            backend.TryGetVertexBuffer(handle, out var resource) == false)
        {
            return;
        }

        backend.ReleaseBufferResource(resource);
    }
}
