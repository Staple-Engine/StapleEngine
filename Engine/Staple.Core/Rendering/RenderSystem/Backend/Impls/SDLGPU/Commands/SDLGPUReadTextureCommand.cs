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
            SDL.ReleaseGPUTransferBuffer(backend.device, resource.transferBuffer);

            resource.transferBuffer = nint.Zero;
        }

        var info = new SDL.GPUTransferBufferCreateInfo()
        {
            Size = (uint)resource.length,
            Usage = SDL.GPUTransferBufferUsage.Download,
        };

        resource.transferBuffer = SDL.CreateGPUTransferBuffer(backend.device, in info);

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
            backend.copyPass = SDL.BeginGPUCopyPass(backend.commandBuffer);
        }

        if (backend.copyPass == nint.Zero)
        {
            return;
        }

        var textureInfo = new SDL.GPUTextureTransferInfo()
        {
            Offset = 0,
            PixelsPerRow = (uint)resource.width,
            RowsPerLayer = (uint)resource.height,
            TransferBuffer = resource.transferBuffer,
        };

        var destination = new SDL.GPUTextureRegion()
        {
            Texture = resource.texture,
            W = (uint)resource.width,
            H = (uint)resource.height,
            D = 1,
        };

        SDL.DownloadFromGPUTexture(backend.copyPass, in destination, in textureInfo);

        backend.QueueTextureRead(texture, onComplete);
    }
}
