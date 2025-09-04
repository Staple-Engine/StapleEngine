using Bgfx;
using Staple.Internal;
using System;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Index Buffer resource
/// </summary>
public sealed class IndexBuffer
{
    private static IndexBuffer transientBuffer;

    private RenderBufferType type;

    /// <summary>
    /// The index buffer's handle
    /// </summary>
    internal bgfx.IndexBufferHandle handle;

    /// <summary>
    /// The index buffer's dynamic handle
    /// </summary>
    internal bgfx.DynamicIndexBufferHandle dynamicHandle;

    /// <summary>
    /// The index buffer's transient handle
    /// </summary>
    internal bgfx.TransientIndexBuffer transientHandle;

    /// <summary>
    /// Whether this was destroyed
    /// </summary>
    internal bool Disposed { get; private set; } = false;

    internal unsafe IndexBuffer(bgfx.IndexBufferHandle handle)
    {
        type = RenderBufferType.Normal;
        this.handle = handle;

        ResourceManager.instance.userCreatedIndexBuffers.Add(new(this));
    }

    internal unsafe IndexBuffer(bgfx.DynamicIndexBufferHandle handle)
    {
        type = RenderBufferType.Dynamic;
        dynamicHandle = handle;

        ResourceManager.instance.userCreatedIndexBuffers.Add(new(this));
    }

    internal unsafe IndexBuffer(bgfx.TransientIndexBuffer handle)
    {
        type = RenderBufferType.Transient;

        transientHandle = handle;
    }

    ~IndexBuffer()
    {
        Destroy();
    }

    /// <summary>
    /// Destroys the index buffer's resources
    /// </summary>
    public void Destroy()
    {
        if (Disposed)
        {
            return;
        }

        Disposed = true;

        switch(type)
        {
            case RenderBufferType.Normal:

                if(handle.Valid)
                {
                    bgfx.destroy_index_buffer(handle);
                }

                handle = default;

                break;

            case RenderBufferType.Dynamic:

                if(dynamicHandle.Valid)
                {
                    bgfx.destroy_dynamic_index_buffer(dynamicHandle);
                }

                dynamicHandle = default;

                break;
        }
    }

    /// <summary>
    /// Sets this index buffer active
    /// </summary>
    /// <param name="start">The starting index</param>
    /// <param name="count">The amount of indices to use</param>
    internal void SetActive(uint start, uint count)
    {
        if (Disposed)
        {
            return;
        }

        switch(type)
        {
            case RenderBufferType.Normal:

                bgfx.set_index_buffer(handle, start, count);

                break;

            case RenderBufferType.Dynamic:

                bgfx.set_dynamic_index_buffer(dynamicHandle, start, count);

                break;

            case RenderBufferType.Transient:

                unsafe
                {
                    fixed (bgfx.TransientIndexBuffer* buffer = &transientHandle)
                    {
                        bgfx.set_transient_index_buffer(buffer, start, count);
                    }
                }

                break;
        }
    }

    /// <summary>
    /// Sets this buffer as a compute buffer
    /// </summary>
    /// <param name="stage">The buffer stage</param>
    /// <param name="access">The access mode</param>
    public void SetBufferActive(byte stage, Access access)
    {
        if (Disposed || type == RenderBufferType.Transient)
        {
            return;
        }

        switch (type)
        {
            case RenderBufferType.Normal:

                bgfx.set_compute_index_buffer(stage, handle, BGFXUtils.GetBGFXAccess(access));

                break;

            case RenderBufferType.Dynamic:

                bgfx.set_compute_dynamic_index_buffer(stage, dynamicHandle, BGFXUtils.GetBGFXAccess(access));

                break;
        }
    }

    /// <summary>
    /// Updates the index buffer's data (if it's dynamic)
    /// </summary>
    /// <param name="indices">An array of new data</param>
    /// <param name="startIndex">The starting index</param>
    /// <param name="copyMemory">Whether to copy the data to memory (data needs to be available for at least 2 frames)</param>
    public void Update(Span<ushort> indices, int startIndex, bool copyMemory)
    {
        if(Disposed ||
            type != RenderBufferType.Dynamic ||
            dynamicHandle.Valid == false ||
            indices.Length == 0)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = indices)
            {
                var dataSize = (uint)(indices.Length * sizeof(ushort));

                bgfx.Memory* outData = copyMemory ? bgfx.alloc(dataSize) : bgfx.make_ref(ptr, dataSize);

                if (copyMemory)
                {
                    var target = new Span<ushort>(outData->data, indices.Length);

                    indices.CopyTo(target);
                }

                bgfx.update_dynamic_index_buffer(dynamicHandle, (uint)startIndex, outData);
            }
        }
    }

    /// <summary>
    /// Updates the index buffer's data (if it's dynamic)
    /// </summary>
    /// <param name="indices">An array of new data</param>
    /// <param name="startIndex">The starting index</param>
    /// <param name="copyMemory">Whether to copy the data to memory (data needs to be available for at least 2 frames)</param>
    public void Update(Span<uint> indices, int startIndex, bool copyMemory)
    {
        if (Disposed ||
            type != RenderBufferType.Dynamic ||
            dynamicHandle.Valid == false ||
            indices.Length == 0)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = indices)
            {
                var dataSize = (uint)(indices.Length * sizeof(uint));

                bgfx.Memory* outData = copyMemory ? bgfx.alloc(dataSize) : bgfx.make_ref(ptr, dataSize);

                if(copyMemory)
                {
                    var target = new Span<uint>(outData->data, indices.Length);

                    indices.CopyTo(target);
                }

                bgfx.update_dynamic_index_buffer(dynamicHandle, (uint)startIndex, outData);
            }
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
    /// <param name="flags">Additional flags</param>
    /// <returns>The index buffer, or null</returns>
    public static IndexBuffer Create(Span<ushort> data, RenderBufferFlags flags = RenderBufferFlags.None)
    {
        var size = Marshal.SizeOf<ushort>();

        unsafe
        {
            bgfx.Memory* outData = bgfx.alloc((uint)(size * data.Length));

            var target = new Span<ushort>(outData->data, data.Length);

            data.CopyTo(target);

            var handle = bgfx.create_index_buffer(outData, (ushort)BGFXUtils.GetBGFXBufferFlags(flags));

            return new IndexBuffer(handle);
        }
    }

    /// <summary>
    /// Creates an index buffer from uint data
    /// </summary>
    /// <param name="data">The data</param>
    /// <param name="flags">Additional flags</param>
    /// <returns>The index buffer, or null</returns>
    public static IndexBuffer Create(Span<uint> data, RenderBufferFlags flags = RenderBufferFlags.None)
    {
        var size = Marshal.SizeOf<uint>();

        unsafe
        {
            bgfx.Memory* outData = bgfx.alloc((uint)(data.Length * size));

            var target = new Span<uint>(outData->data, data.Length);

            data.CopyTo(target);

            var handle = bgfx.create_index_buffer(outData, (ushort)BGFXUtils.GetBGFXBufferFlags((flags | RenderBufferFlags.Index32)));

            return new IndexBuffer(handle);
        }
    }

    /// <summary>
    /// Creates a dynamic index buffer
    /// </summary>
    /// <param name="flags">Additional flags</param>
    /// <param name="allowResize">Whether the buffer can be resized</param>
    /// <param name="elementCount">The element count for the buffer</param>
    /// <returns>The index buffer</returns>
    public static IndexBuffer CreateDynamic(RenderBufferFlags flags = RenderBufferFlags.None, bool allowResize = true, uint elementCount = 0)
    {
        unsafe
        {
            if (allowResize)
            {
                flags |= RenderBufferFlags.AllowResize;
            }

            var handle = bgfx.create_dynamic_index_buffer(elementCount, (ushort)BGFXUtils.GetBGFXBufferFlags(flags));

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
