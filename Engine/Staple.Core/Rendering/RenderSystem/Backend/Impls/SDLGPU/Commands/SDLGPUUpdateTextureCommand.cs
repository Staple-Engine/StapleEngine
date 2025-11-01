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
                SDL.SDL_ReleaseGPUTransferBuffer(backend.device, resource.transferBuffer);

                resource.transferBuffer = nint.Zero;
            }

            var info = new SDL.SDL_GPUTransferBufferCreateInfo()
            {
                size = (uint)data.Length,
                usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            };

            resource.transferBuffer = SDL.SDL_CreateGPUTransferBuffer(backend.device, in info);

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
            backend.copyPass = SDL.SDL_BeginGPUCopyPass(backend.commandBuffer);
        }

        if (backend.copyPass == nint.Zero)
        {
            return;
        }

        var mapData = SDL.SDL_MapGPUTransferBuffer(backend.device, resource.transferBuffer, true);

        unsafe
        {
            var to = new Span<byte>((void*)mapData, data.Length);

            data.CopyTo(to);
        }

        SDL.SDL_UnmapGPUTransferBuffer(backend.device, resource.transferBuffer);

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

        SDL.SDL_UploadToGPUTexture(backend.copyPass, in textureInfo, in destination, false);
    }
}
