namespace Staple.Internal;

internal class SDLGPUDestroyTextureCommand(ResourceHandle<Texture> handle) : IRenderCommand
{
    public ResourceHandle<Texture> handle = handle;

    public void Update(IRendererBackend rendererBackend)
    {
        if (rendererBackend is not SDLGPURendererBackend backend ||
            backend.TryGetTexture(handle, out var resource) == false ||
            resource.used == false)
        {
            return;
        }

        backend.ReleaseTextureResource(resource);
    }
}
