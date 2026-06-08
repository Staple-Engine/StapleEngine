namespace Staple.Internal;

internal class SDLGPUDestroyTextureCommand(SDLGPURendererBackend backend, ResourceHandle<Texture> handle) : IRenderCommand
{
    public void Update()
    {
        if (!backend.TryGetTexture(handle, out var resource) || !resource.used)
        {
            return;
        }

        backend.ReleaseTextureResource(resource);
    }
}
