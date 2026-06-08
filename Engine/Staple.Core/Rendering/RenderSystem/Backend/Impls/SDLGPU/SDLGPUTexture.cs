using System;

namespace Staple.Internal;

internal class SDLGPUTexture(ResourceHandle<Texture> handle, int width, int height, TextureFormat format, TextureFlags flags,
    SDLGPURendererBackend backend) : ITexture
{
    public ResourceHandle<Texture> handle = handle;

    public TextureFormat Format { get; } = format;

    public readonly TextureFlags flags = flags;

    public int Width { get; } = width;

    public int Height { get; } = height;

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
        if(Disposed || !handle.IsValid)
        {
            return;
        }

        backend.UpdateTexture(handle, data);
    }
}
