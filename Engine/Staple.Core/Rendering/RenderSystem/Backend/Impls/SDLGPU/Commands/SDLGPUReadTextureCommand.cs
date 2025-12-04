using SDL3;
using System;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Staple.Internal;

internal class SDLGPUReadTextureCommand(SDLGPUTexture texture, Action<byte[]> onComplete) : IRenderCommand
{
    public readonly SDLGPUTexture texture = texture;
    public readonly Action<byte[]> onComplete = onComplete;

    public void Update(IRendererBackend rendererBackend)
    {
        if (rendererBackend is not SDLGPURendererBackend backend ||
            (texture?.Disposed ?? true) ||
            backend.TryGetTexture(texture.handle, out var resource) == false ||
            resource.used == false ||
            resource.length == 0)
        {
            return;
        }

        if (resource.transferBuffer != nint.Zero)
        {
            SDL.SDL_ReleaseGPUTransferBuffer(backend.device, resource.transferBuffer);

            resource.transferBuffer = nint.Zero;
        }

        var info = new SDL.SDL_GPUTransferBufferCreateInfo()
        {
            size = (uint)resource.length,
            usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD,
        };

        resource.transferBuffer = SDL.SDL_CreateGPUTransferBuffer(backend.device, in info);

        if (resource.transferBuffer == nint.Zero)
        {
            return;
        }

        if (backend.renderPass != nint.Zero)
        {
            backend.FinishPasses();
        }

        if (backend.copyPass == nint.Zero)
        {
            backend.copyPass = SDL.SDL_BeginGPUCopyPass(backend.commandBuffer);
        }

        if (backend.copyPass == nint.Zero)
        {
            return;
        }

        var textureInfo = new SDL.SDL_GPUTextureTransferInfo()
        {
            offset = 0,
            pixels_per_row = (uint)resource.width,
            rows_per_layer = (uint)resource.height,
            transfer_buffer = resource.transferBuffer,
        };

        var destination = new SDL.SDL_GPUTextureRegion()
        {
            texture = resource.texture,
            w = (uint)resource.width,
            h = (uint)resource.height,
            d = 1,
        };

        SDL.SDL_DownloadFromGPUTexture(backend.copyPass, in destination, in textureInfo);

        backend.QueueTextureRead(texture, onComplete);
    }
}
