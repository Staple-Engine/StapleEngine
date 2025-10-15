using SDL3;
using System;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPUIndexBuffer : IndexBuffer
{
    public nint buffer;

    private readonly nint device;
    private readonly Func<SDLGPURenderCommand> commandSupplier;

    public SDLGPUIndexBuffer(nint device, nint buffer, Func<SDLGPURenderCommand> commandSupplier)
    {
        this.device = device;
        this.buffer = buffer;
        this.commandSupplier = commandSupplier;

        ResourceManager.instance.userCreatedIndexBuffers.Add(new(this));
    }

    public override void Destroy()
    {
        base.Destroy();

        if (buffer != nint.Zero)
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

    public override void Update(Span<ushort> data)
    {
        var size = Marshal.SizeOf<ushort>();

        if (Disposed ||
            buffer == nint.Zero ||
            data.Length == 0)
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
            var to = new Span<ushort>((void*)mapData, data.Length);

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

        Is32Bit = false;
    }

    public override void Update(Span<uint> data)
    {
        var size = Marshal.SizeOf<uint>();

        if (Disposed ||
            buffer == nint.Zero ||
            data.Length == 0)
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
            var to = new Span<uint>((void*)mapData, data.Length);

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

        Is32Bit = true;
    }
}
