namespace Staple.Internal;

internal class SDLGPUDestroyIndexBufferCommand(SDLGPURendererBackend backend, ResourceHandle<IndexBuffer> handle) : IRenderCommand
{
    public void Update()
    {
        if (!handle.IsValid || !backend.TryGetIndexBuffer(handle, out var resource))
        {
            return;
        }

        backend.ReleaseBufferResource(resource);
    }
}
