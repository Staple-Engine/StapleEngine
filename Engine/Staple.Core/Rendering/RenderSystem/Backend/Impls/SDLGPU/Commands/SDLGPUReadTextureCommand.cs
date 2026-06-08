using SDL;
using System;

namespace Staple.Internal;

internal unsafe class SDLGPUReadTextureCommand(SDLGPURendererBackend backend, SDLGPUTexture texture, Action<byte[]> onComplete) :
    IRenderCommand
{
    public void Update()
    {
        if ((texture?.Disposed ?? true) ||
            !backend.TryGetTexture(texture.handle, out var resource) ||
            !resource.used ||
            resource.length == 0)
        {
            return;
        }

        resource.transferBuffer = backend.GetTransferBuffer(true, resource.length);

        if (resource.transferBuffer == null)
        {
            return;
        }

        if(!backend.BeginCopyPass())
        {
            return;
        }

        var textureInfo = new SDL_GPUTextureTransferInfo()
        {
            offset = 0,
            pixels_per_row = (uint)resource.width,
            rows_per_layer = (uint)resource.height,
            transfer_buffer = resource.transferBuffer,
        };

        var destination = new SDL_GPUTextureRegion()
        {
            texture = resource.texture,
            w = (uint)resource.width,
            h = (uint)resource.height,
            d = 1,
        };

        SDL3.SDL_DownloadFromGPUTexture(backend.copyPass, &destination, &textureInfo);

        backend.QueueTextureRead(texture, onComplete);
    }
}
