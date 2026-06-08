using SDL;
using System;

namespace Staple.Internal;

internal unsafe class SDLGPUUpdateIndexBufferCommand(SDLGPURendererBackend backend, ResourceHandle<IndexBuffer> handle, byte[] data) :
    IRenderCommand
{
    public void Update()
    {
        if (data == null || data.Length == 0 || !backend.TryGetIndexBuffer(handle, out var buffer))
        {
            return;
        }

        if (buffer.buffer == null || buffer.length != data.Length)
        {
            buffer.length = data.Length;

            if (buffer.buffer != null)
            {
                SDL3.SDL_ReleaseGPUBuffer(backend.device, buffer.buffer);

                buffer.transferBuffer = null;
                buffer.buffer = null;
            }

            var usageFlags = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX;

            if (buffer.flags.HasFlag(RenderBufferFlags.GraphicsRead))
            {
                usageFlags |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ;
            }

            if (buffer.flags.HasFlag(RenderBufferFlags.ComputeRead))
            {
                usageFlags |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ;
            }

            if (buffer.flags.HasFlag(RenderBufferFlags.ComputeWrite))
            {
                usageFlags |= SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE;
            }

            var createInfo = new SDL_GPUBufferCreateInfo()
            {
                size = (uint)data.Length,
                usage = usageFlags,
            };

            buffer.buffer = SDL3.SDL_CreateGPUBuffer(backend.device, &createInfo);

            if (buffer.buffer == null)
            {
                return;
            }

            buffer.transferBuffer = backend.GetTransferBuffer(false, data.Length);

            if (buffer.transferBuffer == null)
            {
                SDL3.SDL_ReleaseGPUBuffer(backend.device, buffer.buffer);

                buffer.buffer = null;

                return;
            }
        }

        if (!backend.BeginCopyPass())
        {
            return;
        }

        var mapData = SDL3.SDL_MapGPUTransferBuffer(backend.device, buffer.transferBuffer, true);

        unsafe
        {
            fixed (byte* ptr = data)
            {
                var from = new Span<byte>(ptr, data.Length);
                var to = new Span<byte>((void*)mapData, data.Length);

                from.CopyTo(to);
            }
        }

        SDL3.SDL_UnmapGPUTransferBuffer(backend.device, buffer.transferBuffer);

        var location = new SDL_GPUTransferBufferLocation()
        {
            transfer_buffer = buffer.transferBuffer,
        };

        var region = new SDL_GPUBufferRegion()
        {
            buffer = buffer.buffer,
            size = (uint)data.Length,
        };

        SDL3.SDL_UploadToGPUBuffer(backend.copyPass, &location, &region, false);
    }
}
