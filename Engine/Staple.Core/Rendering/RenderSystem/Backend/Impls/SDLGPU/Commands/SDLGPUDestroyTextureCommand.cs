namespace Staple.Internal;

internal class SDLGPUDestroyTextureCommand(ResourceHandle<Texture> handle) : IRenderCommand
{
    public void Update(IRendererBackend rendererBackend)
    {
        if (rendererBackend is not SDLGPURendererBackend backend ||
            !backend.TryGetTexture(handle, out var resource) ||
            !resource.used)
        {
            return;
        }

        backend.ReleaseTextureResource(resource);
    }
}
