using SDL3;
using System;

namespace Staple.Internal;

internal class SDLGPUUpdateVertexBufferCommand(ResourceHandle<VertexBuffer> handle, byte[] data) : IRenderCommand
{
    public void Update(IRendererBackend rendererBackend)
    {
        if (rendererBackend is not SDLGPURendererBackend backend ||
            backend.commandBuffer == nint.Zero ||
            handle.IsValid == false ||
            data == null ||
            data.Length == 0 ||
            backend.TryGetVertexBuffer(handle, out var buffer) == false)
        {
            return;
        }

        if (buffer.buffer == nint.Zero || buffer.length != data.Length)
        {
            buffer.length = data.Length;

            if (buffer.buffer != nint.Zero)
            {
                SDL.SDL_WaitForGPUIdle(backend.device);
                SDL.SDL_ReleaseGPUBuffer(backend.device, buffer.buffer);
                SDL.SDL_ReleaseGPUTransferBuffer(backend.device, buffer.transferBuffer);

                buffer.transferBuffer = nint.Zero;
                buffer.buffer = nint.Zero;
            }

            var usageFlags = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX;

            if (buffer.flags.HasFlag(RenderBufferFlags.GraphicsRead))
            {
                usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ;
            }

            if (buffer.flags.HasFlag(RenderBufferFlags.ComputeRead))
            {
                usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ;
            }

            if (buffer.flags.HasFlag(RenderBufferFlags.ComputeWrite))
            {
                usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE;
            }

            var createInfo = new SDL.SDL_GPUBufferCreateInfo()
            {
                size = (uint)data.Length,
                usage = usageFlags,
            };

            buffer.buffer = SDL.SDL_CreateGPUBuffer(backend.device, in createInfo);

            if (buffer.buffer == nint.Zero)
            {
                return;
            }

            var transferInfo = new SDL.SDL_GPUTransferBufferCreateInfo()
            {
                size = (uint)data.Length,
                usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            };

            buffer.transferBuffer = SDL.SDL_CreateGPUTransferBuffer(backend.device, in transferInfo);

            if (buffer.transferBuffer == nint.Zero)
            {
                SDL.SDL_WaitForGPUIdle(backend.device);
                SDL.SDL_ReleaseGPUBuffer(backend.device, buffer.buffer);

                buffer.buffer = nint.Zero;

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

        var mapData = SDL.SDL_MapGPUTransferBuffer(backend.device, buffer.transferBuffer, true);

        unsafe
        {
            fixed(byte *ptr = data)
            {
                var from = new Span<byte>(ptr, data.Length);
                var to = new Span<byte>((void*)mapData, data.Length);

                from.CopyTo(to);
            }
        }

        SDL.SDL_UnmapGPUTransferBuffer(backend.device, buffer.transferBuffer);

        var location = new SDL.SDL_GPUTransferBufferLocation()
        {
            transfer_buffer = buffer.transferBuffer,
            offset = 0,
        };

        var region = new SDL.SDL_GPUBufferRegion()
        {
            buffer = buffer.buffer,
            size = (uint)data.Length,
        };

        SDL.SDL_UploadToGPUBuffer(backend.copyPass, in location, in region, true);
    }
}
