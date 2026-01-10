namespace Staple.Internal;

internal class SDLGPUDestroyVertexBufferCommand(ResourceHandle<VertexBuffer> handle) : IRenderCommand
{
    public ResourceHandle<VertexBuffer> handle = handle;

    public void Update(IRendererBackend rendererBackend)
    {
        if(rendererBackend is not SDLGPURendererBackend backend ||
            !handle.IsValid ||
            !backend.TryGetVertexBuffer(handle, out var resource))
        {
            return;
        }

        backend.ReleaseBufferResource(resource);
    }
}
