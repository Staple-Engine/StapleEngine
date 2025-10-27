using SDL3;
using System;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPUVertexBuffer : VertexBuffer
{
    public nint buffer;

    private readonly nint device;
    private readonly RenderBufferFlags flags;
    private readonly SDLGPURendererBackend backend;
    private int length;
    private nint transferBuffer;

    public bool Valid => buffer != nint.Zero && transferBuffer != nint.Zero;

    public SDLGPUVertexBuffer(nint device, RenderBufferFlags flags, VertexLayout layout, SDLGPURendererBackend backend)
    {
        this.device = device;
        this.flags = flags;
        this.layout = layout;
        this.backend = backend;

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

        if (transferBuffer != nint.Zero)
        {
            SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

            transferBuffer = nint.Zero;
        }
    }

    public void ResizeIfNeeded(int lengthInBytes)
    {
        if (length == lengthInBytes)
        {
            return;
        }

        if (buffer != nint.Zero)
        {
            SDL.SDL_WaitForGPUIdle(device);
            SDL.SDL_ReleaseGPUBuffer(device, buffer);
            SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

            transferBuffer = nint.Zero;
            buffer = nint.Zero;
        }

        var usageFlags = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX;

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

        if (transferBuffer == nint.Zero)
        {
            SDL.SDL_WaitForGPUIdle(device);
            SDL.SDL_ReleaseGPUBuffer(device, buffer);

            buffer = nint.Zero;

            return;
        }

        length = lengthInBytes;
    }

    public override void Update(nint data, int lengthInBytes)
    {
        if (Disposed ||
            data == nint.Zero ||
            lengthInBytes == 0 ||
            lengthInBytes % layout.Stride != 0)
        {
            return;
        }

        ResizeIfNeeded(lengthInBytes);

        if (Valid == false || backend.TryGetCommandBuffer(out var command) == false)
        {
            return;
        }

        var mapData = SDL.SDL_MapGPUTransferBuffer(device, transferBuffer, true);

        unsafe
        {
            var from = new Span<byte>((void *)data, lengthInBytes);
            var to = new Span<byte>((void*)mapData, lengthInBytes);

            from.CopyTo(to);
        }

        SDL.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

        var copyPass = SDL.SDL_BeginGPUCopyPass(command);

        if(copyPass == nint.Zero)
        {
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
    }

    public override void Update<T>(Span<T> data)
    {
        var size = Marshal.SizeOf<T>();

        if (Disposed ||
            data.Length == 0 ||
            size % layout.Stride != 0)
        {
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
            var to = new Span<T>((void*)mapData, data.Length);

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
    }

    public override void Update(Span<byte> data)
    {
        if (Disposed ||
            data.Length == 0 ||
            data.Length % layout.Stride != 0)
        {
            return;
        }

        ResizeIfNeeded(data.Length);

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
            var to = new Span<byte>((void*)mapData, data.Length);

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
            size = (uint)data.Length,
        };

        SDL.SDL_UploadToGPUBuffer(copyPass, in location, in region, true);

        SDL.SDL_EndGPUCopyPass(copyPass);
    }
}
