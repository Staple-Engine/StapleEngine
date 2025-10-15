using SDL3;
using System;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPUVertexBuffer : VertexBuffer
{
    public nint buffer;

    private readonly nint device;
    private readonly Func<SDLGPURenderCommand> commandSupplier;

    public SDLGPUVertexBuffer(nint device, nint buffer, VertexLayout layout, Func<SDLGPURenderCommand> commandSupplier)
    {
        this.device = device;
        this.buffer = buffer;
        this.layout = layout;
        this.commandSupplier = commandSupplier;

        ResourceManager.instance.userCreatedVertexBuffers.Add(new(this));
    }

    public override void Destroy()
    {
        base.Destroy();

        if(buffer != nint.Zero)
        {
            SDL.SDL_ReleaseGPUBuffer(device, buffer);

            buffer = nint.Zero;
        }
    }

    public override void SetActive(uint start, uint count)
    {
    }

    public override void SetBufferActive(byte stage, Access access)
    {
    }

    public override void Update(nint data, int lengthInBytes)
    {
        if (Disposed ||
            buffer == nint.Zero ||
            data == nint.Zero ||
            lengthInBytes == 0 ||
            lengthInBytes % layout.Stride != 0)
        {
            return;
        }

        var transferInfo = new SDL.SDL_GPUTransferBufferCreateInfo()
        {
            size = (uint)lengthInBytes,
            usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
        };

        var transferBuffer = SDL.SDL_CreateGPUTransferBuffer(device, in transferInfo);

        if(transferBuffer == nint.Zero)
        {
            return;
        }

        var command = commandSupplier();

        if (command == null)
        {
            SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

            return;
        }

        var mapData = SDL.SDL_MapGPUTransferBuffer(device, transferBuffer, false);

        unsafe
        {
            var from = new Span<byte>((void *)data, lengthInBytes);
            var to = new Span<byte>((void*)mapData, lengthInBytes);

            from.CopyTo(to);
        }

        SDL.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

        var copyPass = SDL.SDL_BeginGPUCopyPass(command.commandBuffer);

        if(copyPass == nint.Zero)
        {
            SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

            command.Discard();

            return;
        }

        var location = new SDL.SDL_GPUTransferBufferLocation()
        {
            transfer_buffer = transferBuffer,
            offset = 0,
        };

        var region = new SDL.SDL_GPUBufferRegion()
        {
            buffer = buffer,
            size = (uint)lengthInBytes,
        };

        SDL.SDL_UploadToGPUBuffer(copyPass, in location, in region, true);

        SDL.SDL_EndGPUCopyPass(copyPass);

        command.Submit();

        SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
    }

    public override void Update<T>(Span<T> data)
    {
        var size = Marshal.SizeOf<T>();

        if (Disposed ||
            buffer == nint.Zero ||
            data.Length == 0 ||
            size % layout.Stride != 0)
        {
            return;
        }

        var byteSize = data.Length * size;

        var transferInfo = new SDL.SDL_GPUTransferBufferCreateInfo()
        {
            size = (uint)byteSize,
            usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
        };

        var transferBuffer = SDL.SDL_CreateGPUTransferBuffer(device, in transferInfo);

        if (transferBuffer == nint.Zero)
        {
            return;
        }

        var command = commandSupplier();

        if (command == null)
        {
            SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

            return;
        }

        var mapData = SDL.SDL_MapGPUTransferBuffer(device, transferBuffer, false);

        unsafe
        {
            var to = new Span<T>((void*)mapData, data.Length);

            data.CopyTo(to);
        }

        SDL.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

        var copyPass = SDL.SDL_BeginGPUCopyPass(command.commandBuffer);

        if (copyPass == nint.Zero)
        {
            SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

            command.Discard();

            return;
        }

        var location = new SDL.SDL_GPUTransferBufferLocation()
        {
            transfer_buffer = transferBuffer,
            offset = 0,
        };

        var region = new SDL.SDL_GPUBufferRegion()
        {
            buffer = buffer,
            size = (uint)byteSize,
        };

        SDL.SDL_UploadToGPUBuffer(copyPass, in location, in region, true);

        SDL.SDL_EndGPUCopyPass(copyPass);

        command.Submit();

        SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
    }

    public override void Update(Span<byte> data)
    {
        if (Disposed ||
            buffer == nint.Zero ||
            data.Length == 0 ||
            data.Length % layout.Stride != 0)
        {
            return;
        }

        var transferInfo = new SDL.SDL_GPUTransferBufferCreateInfo()
        {
            size = (uint)data.Length,
            usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
        };

        var transferBuffer = SDL.SDL_CreateGPUTransferBuffer(device, in transferInfo);

        if (transferBuffer == nint.Zero)
        {
            return;
        }

        var command = commandSupplier();

        if (command == null)
        {
            SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

            return;
        }

        var mapData = SDL.SDL_MapGPUTransferBuffer(device, transferBuffer, false);

        unsafe
        {
            var to = new Span<byte>((void*)mapData, data.Length);

            data.CopyTo(to);
        }

        SDL.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

        var copyPass = SDL.SDL_BeginGPUCopyPass(command.commandBuffer);

        if (copyPass == nint.Zero)
        {
            SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

            command.Discard();

            return;
        }

        var location = new SDL.SDL_GPUTransferBufferLocation()
        {
            transfer_buffer = transferBuffer,
            offset = 0,
        };

        var region = new SDL.SDL_GPUBufferRegion()
        {
            buffer = buffer,
            size = (uint)data.Length,
        };

        SDL.SDL_UploadToGPUBuffer(copyPass, in location, in region, true);

        SDL.SDL_EndGPUCopyPass(copyPass);

        command.Submit();

        SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
    }
}
