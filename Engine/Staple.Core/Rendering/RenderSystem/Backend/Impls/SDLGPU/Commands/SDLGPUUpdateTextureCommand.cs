using SDL3;
using System;

namespace Staple.Internal;

internal class SDLGPUUpdateTextureCommand(ResourceHandle<Texture> handle, byte[] data) : IRenderCommand
{
    public ResourceHandle<Texture> handle = handle;
    public byte[] data = data;

    public void Update(IRendererBackend rendererBackend)
    {
        if(rendererBackend is not SDLGPURendererBackend backend ||
            backend.TryGetTexture(handle, out var resource) == false ||
            resource.used == false)
        {
            return;
        }

        if(resource.length != data.Length || resource.transferBuffer == nint.Zero)
        {
            if(resource.transferBuffer != nint.Zero)
            {
                resource.transferBuffer = nint.Zero;
            }

            resource.transferBuffer = backend.GetTransferBuffer(false, data.Length);

            if (resource.transferBuffer == nint.Zero)
            {
                return;
            }
        }

        if(backend.renderPass != nint.Zero)
        {
            backend.FinishPasses();
        }

        if(backend.copyPass == nint.Zero)
        {
            backend.copyPass = SDL.BeginGPUCopyPass(backend.commandBuffer);
        }

        if (backend.copyPass == nint.Zero)
        {
            return;
        }

        var mapData = SDL.MapGPUTransferBuffer(backend.device, resource.transferBuffer, true);

        unsafe
        {
            var to = new Span<byte>((void*)mapData, data.Length);

            data.CopyTo(to);
        }

        SDL.UnmapGPUTransferBuffer(backend.device, resource.transferBuffer);

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

        SDL.UploadToGPUTexture(backend.copyPass, in textureInfo, in destination, false);
    }
}
