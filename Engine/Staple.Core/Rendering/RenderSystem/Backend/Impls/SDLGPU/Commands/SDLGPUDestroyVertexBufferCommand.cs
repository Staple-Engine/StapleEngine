namespace Staple.Internal;

internal class SDLGPUDestroyVertexBufferCommand(SDLGPURendererBackend backend, ResourceHandle<VertexBuffer> handle) : IRenderCommand
{
    public void Update()
    {
        backend.ReleaseBufferResource(backend.vertexBuffers, handle);
    }
}
