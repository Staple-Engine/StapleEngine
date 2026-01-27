using SDL;
using System;

namespace Staple.Internal;

internal unsafe class SDLGPUUpdateTextureCommand(SDLGPURendererBackend backend, ResourceHandle<Texture> handle, byte[] data) :
    IRenderCommand
{
    public void Update()
    {
        if (!backend.TryGetTexture(handle, out var resource) || !resource.used)
        {
            return;
        }

        if (resource.length != data.Length || resource.transferBuffer == null)
        {
            resource.transferBuffer = backend.GetTransferBuffer(false, data.Length);

            if (resource.transferBuffer == null)
            {
                return;
            }
        }

        if (!backend.BeginCopyPass())
        {
            return;
        }

        var mapData = SDL3.SDL_MapGPUTransferBuffer(backend.device, resource.transferBuffer, true);

        unsafe
        {
            var to = new Span<byte>((void*)mapData, data.Length);

            data.CopyTo(to);
        }

        SDL3.SDL_UnmapGPUTransferBuffer(backend.device, resource.transferBuffer);

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

        SDL3.SDL_UploadToGPUTexture(backend.copyPass, &textureInfo, &destination, false);
    }
}
