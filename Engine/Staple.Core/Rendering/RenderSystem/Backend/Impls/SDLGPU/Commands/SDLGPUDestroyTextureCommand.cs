namespace Staple.Internal;

internal class SDLGPUDestroyTextureCommand(SDLGPURendererBackend backend, ResourceHandle<Texture> handle) : IRenderCommand
{
    public void Update()
    {
        backend.ReleaseTextureResource(backend.textures, handle);
    }
}
