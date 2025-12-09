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
                SDL.WaitForGPUIdle(backend.device);
                SDL.ReleaseGPUBuffer(backend.device, buffer.buffer);
                SDL.ReleaseGPUTransferBuffer(backend.device, buffer.transferBuffer);

                buffer.transferBuffer = nint.Zero;
                buffer.buffer = nint.Zero;
            }

            var usageFlags = SDL.GPUBufferUsageFlags.Vertex;

            if (buffer.flags.HasFlag(RenderBufferFlags.GraphicsRead))
            {
                usageFlags |= SDL.GPUBufferUsageFlags.GraphicsStorageRead;
            }

            if (buffer.flags.HasFlag(RenderBufferFlags.ComputeRead))
            {
                usageFlags |= SDL.GPUBufferUsageFlags.ComputeStorageRead;
            }

            if (buffer.flags.HasFlag(RenderBufferFlags.ComputeWrite))
            {
                usageFlags |= SDL.GPUBufferUsageFlags.ComputeStorageWrite;
            }

            var createInfo = new SDL.GPUBufferCreateInfo()
            {
                Size = (uint)data.Length,
                Usage = usageFlags,
            };

            buffer.buffer = SDL.CreateGPUBuffer(backend.device, in createInfo);

            if (buffer.buffer == nint.Zero)
            {
                return;
            }

            var transferInfo = new SDL.GPUTransferBufferCreateInfo()
            {
                Size = (uint)data.Length,
                Usage = SDL.GPUTransferBufferUsage.Upload,
            };

            buffer.transferBuffer = SDL.CreateGPUTransferBuffer(backend.device, in transferInfo);

            if (buffer.transferBuffer == nint.Zero)
            {
                SDL.WaitForGPUIdle(backend.device);
                SDL.ReleaseGPUBuffer(backend.device, buffer.buffer);

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
            backend.copyPass = SDL.BeginGPUCopyPass(backend.commandBuffer);
        }

        if (backend.copyPass == nint.Zero)
        {
            return;
        }

        var mapData = SDL.MapGPUTransferBuffer(backend.device, buffer.transferBuffer, false);

        unsafe
        {
            fixed(byte *ptr = data)
            {
                var from = new Span<byte>(ptr, data.Length);
                var to = new Span<byte>((void*)mapData, data.Length);

                from.CopyTo(to);
            }
        }

        SDL.UnmapGPUTransferBuffer(backend.device, buffer.transferBuffer);

        var location = new SDL.GPUTransferBufferLocation()
        {
            TransferBuffer = buffer.transferBuffer,
            Offset = 0,
        };

        var region = new SDL.GPUBufferRegion()
        {
            Buffer = buffer.buffer,
            Size = (uint)data.Length,
        };

        SDL.UploadToGPUBuffer(backend.copyPass, in location, in region, false);
    }
}
