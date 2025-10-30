using SDL3;
using System;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPUVertexBuffer : VertexBuffer
{
    public ResourceHandle<VertexBuffer> handle;

    private readonly RenderBufferFlags flags;
    private readonly SDLGPURendererBackend backend;

    public bool IsValid => handle.IsValid;

    public SDLGPUVertexBuffer(ResourceHandle<VertexBuffer> handle, RenderBufferFlags flags, VertexLayout layout,
        SDLGPURendererBackend backend)
    {
        this.handle = handle;
        this.flags = flags;
        this.layout = layout;
        this.backend = backend;

        ResourceManager.instance.userCreatedVertexBuffers.Add(new(this));
    }

    public override void Destroy()
    {
        base.Destroy();

        backend.DestroyVertexBuffer(handle);

        handle = ResourceHandle<VertexBuffer>.Invalid;
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

        if (IsValid == false || backend.commandBuffer == nint.Zero)
        {
            return;
        }

        unsafe
        {
            var dataSpan = new Span<byte>((void *)data, lengthInBytes);

            backend.UpdateVertexBuffer(handle, dataSpan);
        }
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

        if (IsValid == false || backend.commandBuffer == nint.Zero)
        {
            return;
        }

        unsafe
        {
            fixed(void *ptr = data)
            {
                var source = new Span<byte>(ptr, byteSize);

                backend.UpdateVertexBuffer(handle, source);
            }
        }
    }

    public override void Update(Span<byte> data)
    {
        if (Disposed ||
            data.Length == 0 ||
            data.Length % layout.Stride != 0)
        {
            return;
        }

        if (IsValid == false || backend.commandBuffer == nint.Zero)
        {
            return;
        }

        backend.UpdateVertexBuffer(handle, data);
    }
}
