namespace Staple.Internal;

internal class SDLGPUDestroyIndexBufferCommand(SDLGPURendererBackend backend, ResourceHandle<IndexBuffer> handle) : IRenderCommand
{
    public void Update()
    {
        backend.ReleaseBufferResource(backend.indexBuffers, handle);
    }
}
