namespace Staple.Internal;

internal class SDLGPUDestroyVertexBufferCommand(SDLGPURendererBackend backend, ResourceHandle<VertexBuffer> handle) : IRenderCommand
{
    public void Update()
    {
        if(!handle.IsValid || !backend.TryGetVertexBuffer(handle, out var resource))
        {
            return;
        }

        backend.ReleaseBufferResource(resource);
    }
}
