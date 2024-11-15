using Bgfx;
using System;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Index Buffer resource
/// </summary>
public class IndexBuffer
{
    private static IndexBuffer transientBuffer;

    /// <summary>
    /// The index buffer's handle
    /// </summary>
    internal bgfx.IndexBufferHandle handle;

    /// <summary>
    /// The index buffer's transient handle
    /// </summary>
    internal bgfx.TransientIndexBuffer transientHandle;

    /// <summary>
    /// Whether this was destroyed
    /// </summary>
    internal bool Disposed { get; private set; } = false;

    /// <summary>
    /// Whether this buffer is transient
    /// </summary>
    private readonly bool isTransient = false;

    internal unsafe IndexBuffer(bgfx.IndexBufferHandle handle)
    {
        this.handle = handle;

        isTransient = false;
    }

    internal unsafe IndexBuffer(bgfx.TransientIndexBuffer handle)
    {
        this.handle = new bgfx.IndexBufferHandle()
        {
            idx = ushort.MaxValue,
        };

        transientHandle = handle;
        isTransient = true;
    }

    ~IndexBuffer()
    {
        Destroy();
    }

    /// <summary>
    /// Destroys the index buffer's resources
    /// </summary>
    internal void Destroy()
    {
        if (Disposed)
        {
            return;
        }

        Disposed = true;

        if (isTransient == false && handle.Valid)
        {
            bgfx.destroy_index_buffer(handle);
        }
    }

    /// <summary>
    /// Sets this index buffer active
    /// </summary>
    /// <param name="start">The starting index</param>
    /// <param name="count">The amount of indices to use</param>
    public void SetActive(uint start, uint count)
    {
        if (Disposed)
        {
            return;
        }

        if(isTransient)
        {
            unsafe
            {
                fixed (bgfx.TransientIndexBuffer* buffer = &transientHandle)
                {
                    bgfx.set_transient_index_buffer(buffer, start, count);
                }
            }
        }
        else
        {
            bgfx.set_index_buffer(handle, start, count);
        }
    }

    /// <summary>
    /// Checks whether we have enough space for indices for transient buffers
    /// </summary>
    /// <param name="indexCount">The amount of indices we need</param>
    /// <param name="isInt32">Whether we want 32-bit indices</param>
    /// <returns>Whether we have enough space</returns>
    public static bool TransientBufferHasSpace(int indexCount, bool isInt32)
    {
        if(indexCount <= 0)
        {
            return false;
        }

        return bgfx.get_avail_transient_index_buffer((uint)indexCount, isInt32) >= indexCount;
    }

    /// <summary>
    /// Creates an index buffer from ushort data
    /// </summary>
    /// <param name="data">The data</param>
    /// <param name="flags">The buffer flags</param>
    /// <returns>The index buffer, or null</returns>
    public static IndexBuffer Create(Span<ushort> data, RenderBufferFlags flags)
    {
        var size = Marshal.SizeOf<ushort>();

        unsafe
        {
            bgfx.Memory* outData = bgfx.alloc((uint)(size * data.Length));

            var target = new Span<ushort>(outData->data, data.Length);

            data.CopyTo(target);

            var handle = bgfx.create_index_buffer(outData, (ushort)flags);

            return new IndexBuffer(handle);
        }
    }

    /// <summary>
    /// Creates an index buffer from uint data
    /// </summary>
    /// <param name="data">The data</param>
    /// <param name="flags">The buffer flags</param>
    /// <returns>The index buffer, or null</returns>
    public static IndexBuffer Create(Span<uint> data, RenderBufferFlags flags)
    {
        var size = Marshal.SizeOf<uint>();

        unsafe
        {
            bgfx.Memory* outData = bgfx.alloc((uint)(data.Length * size));

            var target = new Span<uint>(outData->data, data.Length);

            data.CopyTo(target);

            var handle = bgfx.create_index_buffer(outData, (ushort)(flags | RenderBufferFlags.Index32));

            return new IndexBuffer(handle);
        }
    }

    /// <summary>
    /// Creates a transient index buffer from ushort data
    /// </summary>
    /// <param name="data">The data</param>
    /// <returns>The index buffer, or null</returns>
    public static IndexBuffer CreateTransient(Span<ushort> data)
    {
        if (bgfx.get_avail_transient_index_buffer((uint)data.Length, false) < data.Length)
        {
            return null;
        }

        unsafe
        {
            bgfx.TransientIndexBuffer handle;

            bgfx.alloc_transient_index_buffer(&handle, (uint)data.Length, false);

            var target = new Span<ushort>(handle.data, data.Length);

            data.CopyTo(target);

            transientBuffer ??= new(handle);

            transientBuffer.transientHandle = handle;

            return transientBuffer;
        }
    }

    /// <summary>
    /// Creates a transient index buffer from uint data
    /// </summary>
    /// <param name="data">The data</param>
    /// <returns>The index buffer, or null</returns>
    public static IndexBuffer CreateTransient(Span<uint> data)
    {
        if (bgfx.get_avail_transient_index_buffer((uint)data.Length, false) < data.Length)
        {
            return null;
        }

        unsafe
        {
            bgfx.TransientIndexBuffer handle;

            bgfx.alloc_transient_index_buffer(&handle, (uint)data.Length, true);

            var target = new Span<uint>(handle.data, data.Length);

            data.CopyTo(target);

            transientBuffer ??= new(handle);

            transientBuffer.transientHandle = handle;

            return transientBuffer;
        }
    }
}
