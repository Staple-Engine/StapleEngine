using SDL3;
using System;

namespace Staple.Internal;

internal partial class SDLGPURendererBackend
{
    internal void ReleaseBufferResource(BufferResource resource)
    {
        if ((resource?.used ?? false) == false)
        {
            return;
        }

        SDL.SDL_WaitForGPUIdle(device);

        if (resource.transferBuffer != nint.Zero)
        {
            SDL.SDL_ReleaseGPUTransferBuffer(device, resource.transferBuffer);

            resource.transferBuffer = nint.Zero;
        }

        if (resource.buffer != nint.Zero)
        {
            SDL.SDL_ReleaseGPUBuffer(device, resource.buffer);

            resource.buffer = nint.Zero;
        }

        resource.used = false;
    }

    internal static ResourceHandle<T> ReserveResourceBuffer<T>(BufferResource[] resources, RenderBufferFlags flags)
    {
        for (var i = 0; i < resources.Length; i++)
        {
            if (resources[i]?.used ?? false)
            {
                continue;
            }

            if (resources[i] is null)
            {
                resources[i] = new();
            }

            resources[i].used = true;
            resources[i].flags = flags;

            return new ResourceHandle<T>((ushort)i);
        }

        return ResourceHandle<T>.Invalid;
    }

    internal bool TryGetVertexBuffer(ResourceHandle<VertexBuffer> handle, out BufferResource resource)
    {
        if (handle.IsValid == false ||
            (vertexBuffers[handle.handle]?.used ?? false) == false)
        {
            resource = default;

            return false;
        }

        resource = vertexBuffers[handle.handle];

        return true;
    }

    internal bool TryGetIndexBuffer(ResourceHandle<IndexBuffer> handle, out BufferResource resource)
    {
        if (handle.IsValid == false ||
            (indexBuffers[handle.handle]?.used ?? false) == false)
        {
            resource = default;

            return false;
        }

        resource = indexBuffers[handle.handle];

        return true;
    }

    public VertexBuffer CreateVertexBuffer(Span<byte> data, VertexLayout layout, RenderBufferFlags flags)
    {
        if (layout == null || data.Length == 0)
        {
            return null;
        }

        var handle = SDLGPURendererBackend.ReserveResourceBuffer<VertexBuffer>(vertexBuffers, flags);

        if (handle.IsValid == false)
        {
            return null;
        }

        var outValue = new SDLGPUVertexBuffer(handle, flags, layout, this);

        outValue.Update(data);

        return outValue.IsValid ? outValue : null;
    }

    public VertexBuffer CreateVertexBuffer<T>(Span<T> data, VertexLayout layout, RenderBufferFlags flags) where T : unmanaged
    {
        if (layout == null || data.Length == 0)
        {
            return null;
        }

        var handle = SDLGPURendererBackend.ReserveResourceBuffer<VertexBuffer>(vertexBuffers, flags);

        if (handle.IsValid == false)
        {
            return null;
        }

        var outValue = new SDLGPUVertexBuffer(handle, flags, layout, this);

        outValue.Update(data);

        return outValue.IsValid ? outValue : null;
    }

    public IndexBuffer CreateIndexBuffer(Span<ushort> data, RenderBufferFlags flags)
    {
        var handle = SDLGPURendererBackend.ReserveResourceBuffer<IndexBuffer>(indexBuffers, flags);

        if (handle.IsValid == false)
        {
            return null;
        }

        var outValue = new SDLGPUIndexBuffer(handle, flags, this);

        outValue.Update(data);

        return outValue.Valid ? outValue : null;
    }

    public IndexBuffer CreateIndexBuffer(Span<uint> data, RenderBufferFlags flags)
    {
        var handle = SDLGPURendererBackend.ReserveResourceBuffer<IndexBuffer>(indexBuffers, flags);

        if (handle.IsValid == false)
        {
            return null;
        }

        var outValue = new SDLGPUIndexBuffer(handle, flags, this);

        outValue.Update(data);

        return outValue.Valid ? outValue : null;
    }

    public VertexLayoutBuilder CreateVertexLayoutBuilder()
    {
        return new SDLGPUVertexLayoutBuilder();
    }

    public void UpdateVertexBuffer(ResourceHandle<VertexBuffer> buffer, Span<byte> data)
    {
        AddCommand(new SDLGPUUpdateVertexBufferCommand(buffer, data.ToArray()));
    }

    public void UpdateIndexBuffer(ResourceHandle<IndexBuffer> buffer, Span<ushort> data)
    {
        unsafe
        {
            var holder = new byte[data.Length * sizeof(ushort)];

            fixed(void *ptr = holder)
            {
                var target = new Span<ushort>(ptr, data.Length);

                data.CopyTo(target);
            }

            AddCommand(new SDLGPUUpdateIndexBufferCommand(buffer, holder));
        }
    }

    public void UpdateIndexBuffer(ResourceHandle<IndexBuffer> buffer, Span<uint> data)
    {
        unsafe
        {
            var holder = new byte[data.Length * sizeof(uint)];

            fixed (void* ptr = holder)
            {
                var target = new Span<uint>(ptr, data.Length);

                data.CopyTo(target);
            }

            AddCommand(new SDLGPUUpdateIndexBufferCommand(buffer, holder));
        }
    }

    public void DestroyVertexBuffer(ResourceHandle<VertexBuffer> buffer)
    {
        AddCommand(new SDLGPUDestroyVertexBufferCommand(buffer));
    }

    public void DestroyIndexBuffer(ResourceHandle<IndexBuffer> buffer)
    {
        AddCommand(new SDLGPUDestroyIndexBufferCommand(buffer));
    }
}
