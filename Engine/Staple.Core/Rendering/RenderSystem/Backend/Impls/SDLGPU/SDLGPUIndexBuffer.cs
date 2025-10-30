using SDL3;
using System;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPUIndexBuffer : IndexBuffer
{
    public ResourceHandle<IndexBuffer> handle;

    private readonly RenderBufferFlags flags;
    private readonly SDLGPURendererBackend backend;

    public bool Valid => handle.IsValid;

    public SDLGPUIndexBuffer(ResourceHandle<IndexBuffer> handle, RenderBufferFlags flags, SDLGPURendererBackend backend)
    {
        this.handle = handle;
        this.flags = flags;
        this.backend = backend;

        ResourceManager.instance.userCreatedIndexBuffers.Add(new(this));
    }

    public override void Destroy()
    {
        base.Destroy();

        backend.DestroyIndexBuffer(handle);

        handle = ResourceHandle<IndexBuffer>.Invalid;
    }

    public override void Update(Span<ushort> data)
    {
        if (Disposed ||
            data.Length == 0)
        {
            return;
        }

        backend.UpdateIndexBuffer(handle, data);

        Is32Bit = false;
    }

    public override void Update(Span<uint> data)
    {
        if (Disposed ||
            data.Length == 0)
        {
            return;
        }

        backend.UpdateIndexBuffer(handle, data);

        Is32Bit = true;
    }
}
