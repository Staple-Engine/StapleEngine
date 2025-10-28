using SDL3;
using System;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPUIndexBuffer : IndexBuffer
{
    public nint buffer;

    private readonly nint device;
    private readonly RenderBufferFlags flags;
    private readonly SDLGPURendererBackend backend;
    private nint transferBuffer;
    private int length;

    public bool Valid => buffer != nint.Zero && transferBuffer != nint.Zero;

    public SDLGPUIndexBuffer(nint device, RenderBufferFlags flags, SDLGPURendererBackend backend)
    {
        this.device = device;
        this.flags = flags;
        this.backend = backend;

        ResourceManager.instance.userCreatedIndexBuffers.Add(new(this));
    }

    public override void Destroy()
    {
        base.Destroy();

        void Finish()
        {
            SDL.SDL_WaitForGPUIdle(device);

            if (buffer != nint.Zero)
            {
                SDL.SDL_ReleaseGPUBuffer(device, buffer);

                buffer = nint.Zero;
            }

            if (transferBuffer != nint.Zero)
            {
                SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

                transferBuffer = nint.Zero;
            }
        }

        if (backend.CanUpdateResources == false)
        {
            SDLGPURendererBackend.ReportResourceUnavailability();

            backend.QueueRenderUpdate(Finish);

            return;
        }

        Finish();
    }

    public void ResizeIfNeeded(int lengthInBytes)
    {
        if (length == lengthInBytes)
        {
            return;
        }

        if (backend.CanUpdateResources == false)
        {
            SDLGPURendererBackend.ReportResourceUnavailability();

            return;
        }

        if (buffer != nint.Zero)
        {
            SDL.SDL_WaitForGPUIdle(device);
            SDL.SDL_ReleaseGPUBuffer(device, buffer);

            if (transferBuffer != nint.Zero)
            {
                SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);
            }

            transferBuffer = nint.Zero;
            buffer = nint.Zero;
        }

        var usageFlags = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX;

        if (flags.HasFlag(RenderBufferFlags.GraphicsRead))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeRead))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeWrite))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE;
        }

        var createInfo = new SDL.SDL_GPUBufferCreateInfo()
        {
            size = (uint)lengthInBytes,
            usage = usageFlags,
        };

        buffer = SDL.SDL_CreateGPUBuffer(device, in createInfo);

        if (buffer == nint.Zero)
        {
            return;
        }

        var transferInfo = new SDL.SDL_GPUTransferBufferCreateInfo()
        {
            size = (uint)lengthInBytes,
            usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
        };

        transferBuffer = SDL.SDL_CreateGPUTransferBuffer(device, in transferInfo);

        if(transferBuffer == nint.Zero)
        {
            SDL.SDL_WaitForGPUIdle(device);
            SDL.SDL_ReleaseGPUBuffer(device, buffer);

            buffer = nint.Zero;

            return;
        }

        length = lengthInBytes;
    }

    public override void Update(Span<ushort> data)
    {
        var size = Marshal.SizeOf<ushort>();

        if (Disposed ||
            data.Length == 0)
        {
            return;
        }

        if (backend.CanUpdateResources == false)
        {
            SDLGPURendererBackend.ReportResourceUnavailability();

            return;
        }

        var byteSize = data.Length * size;

        ResizeIfNeeded(data.Length * byteSize);

        if (Valid == false || backend.TryGetCommandBuffer(out var command) == false)
        {
            return;
        }

        var copyPass = SDL.SDL_BeginGPUCopyPass(command);

        if (copyPass == nint.Zero)
        {
            return;
        }

        var mapData = SDL.SDL_MapGPUTransferBuffer(device, transferBuffer, true);

        unsafe
        {
            var to = new Span<ushort>((void*)mapData, data.Length);

            data.CopyTo(to);
        }

        SDL.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

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

        Is32Bit = false;
    }

    public override void Update(Span<uint> data)
    {
        var size = Marshal.SizeOf<uint>();

        if (Disposed ||
            data.Length == 0)
        {
            return;
        }

        if (backend.CanUpdateResources == false)
        {
            SDLGPURendererBackend.ReportResourceUnavailability();

            return;
        }

        var byteSize = data.Length * size;

        ResizeIfNeeded(byteSize);

        if (Valid == false || backend.TryGetCommandBuffer(out var command) == false)
        {
            return;
        }

        var copyPass = SDL.SDL_BeginGPUCopyPass(command);

        if (copyPass == nint.Zero)
        {
            return;
        }

        var mapData = SDL.SDL_MapGPUTransferBuffer(device, transferBuffer, true);

        unsafe
        {
            var to = new Span<uint>((void*)mapData, data.Length);

            data.CopyTo(to);
        }

        SDL.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

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

        Is32Bit = true;
    }
}
