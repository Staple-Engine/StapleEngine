using SDL3;
using System;

namespace Staple.Internal;

internal class SDLGPUUpdateIndexBufferCommand(ResourceHandle<IndexBuffer> handle, byte[] data) : IRenderCommand
{
    public void Update(IRendererBackend rendererBackend)
    {
        if (rendererBackend is not SDLGPURendererBackend backend ||
            backend.commandBuffer == nint.Zero ||
            !handle.IsValid ||
            data == null ||
            data.Length == 0 ||
            !backend.TryGetIndexBuffer(handle, out var buffer))
        {
            return;
        }

        if (buffer.buffer == nint.Zero || buffer.length != data.Length)
        {
            buffer.length = data.Length;

            if (buffer.buffer != nint.Zero)
            {
                SDL.ReleaseGPUBuffer(backend.device, buffer.buffer);

                buffer.transferBuffer = nint.Zero;
                buffer.buffer = nint.Zero;
            }

            var usageFlags = SDL.GPUBufferUsageFlags.Index;

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

            buffer.transferBuffer = backend.GetTransferBuffer(false, data.Length);

            if (buffer.transferBuffer == nint.Zero)
            {
                SDL.ReleaseGPUBuffer(backend.device, buffer.buffer);

                buffer.buffer = nint.Zero;

                return;
            }
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

        var mapData = SDL.MapGPUTransferBuffer(backend.device, buffer.transferBuffer, true);

        unsafe
        {
            fixed (byte* ptr = data)
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
