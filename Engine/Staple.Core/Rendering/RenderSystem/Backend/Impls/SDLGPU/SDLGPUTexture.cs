using System;

namespace Staple.Internal;

internal class SDLGPUTexture(ResourceHandle<Texture> handle, int width, int height, TextureFormat format, TextureFlags flags,
    SDLGPURendererBackend backend) : ITexture
{
    public ResourceHandle<Texture> handle = handle;

    public TextureFormat Format { get; private set; } = format;

    public readonly TextureFlags flags = flags;

    private readonly SDLGPURendererBackend backend = backend;

    public int Width { get; private set; } = width;

    public int Height { get; private set; } = height;

    public bool Disposed { get; private set; }

    public void Destroy()
    {
        if(Disposed)
        {
            return;
        }

        Disposed = true;

        backend.DestroyTexture(handle);

        handle = ResourceHandle<Texture>.Invalid;
    }

    public void Update(Span<byte> data)
    {
        if(Disposed || handle.IsValid == false)
        {
            return;
        }

        backend.UpdateTexture(handle, data);
    }
}
