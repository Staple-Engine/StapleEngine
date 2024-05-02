using Bgfx;
using System;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Vertex Buffer resource
/// </summary>
public class VertexBuffer
{
    public VertexLayout layout;
    public bgfx.VertexBufferHandle handle;
    public bgfx.DynamicVertexBufferHandle dynamicHandle;
    public bgfx.TransientVertexBuffer transientHandle;
    public readonly VertexBufferType type;

    /// <summary>
    /// Whether this was destroyed
    /// </summary>
    internal bool Disposed { get; private set; } = false;

    internal unsafe VertexBuffer(VertexLayout layout, bgfx.VertexBufferHandle handle)
    {
        this.layout = layout;

        this.handle = handle;

        type = VertexBufferType.Normal;
    }

    internal unsafe VertexBuffer(VertexLayout layout, bgfx.DynamicVertexBufferHandle handle)
    {
        this.layout = layout;

        dynamicHandle = handle;

        type = VertexBufferType.Dynamic;
    }

    internal unsafe VertexBuffer(VertexLayout layout, bgfx.TransientVertexBuffer buffer)
    {
        this.layout = layout;

        transientHandle = buffer;

        type = VertexBufferType.Transient;
    }

    ~VertexBuffer()
    {
        Destroy();
    }

    /// <summary>
    /// Destroys the resource
    /// </summary>
    internal void Destroy()
    {
        if (Disposed)
        {
            return;
        }

        Disposed = true;

        switch(type)
        {
            case VertexBufferType.Normal:

                if (handle.Valid)
                {
                    bgfx.destroy_vertex_buffer(handle);
                }

                handle = default;

                break;

            case VertexBufferType.Dynamic:

                if (dynamicHandle.Valid)
                {
                    bgfx.destroy_dynamic_vertex_buffer(dynamicHandle);
                }

                dynamicHandle = default;

                break;
        }
    }

    /// <summary>
    /// Sets the buffer active
    /// </summary>
    /// <param name="stream">The stream to use</param>
    /// <param name="start">Vertex index to start at</param>
    /// <param name="count">Vertex count to use</param>
    internal void SetActive(byte stream, uint start, uint count)
    {
        if (Disposed)
        {
            return;
        }

        switch(type)
        {
            case VertexBufferType.Normal:

                bgfx.set_vertex_buffer(stream, handle, start, count);

                break;

            case VertexBufferType.Dynamic:

                bgfx.set_dynamic_vertex_buffer(stream, dynamicHandle, start, count);

                break;

            case VertexBufferType.Transient:

                unsafe
                {
                    fixed (bgfx.TransientVertexBuffer* buffer = &transientHandle)
                    {
                        bgfx.set_transient_vertex_buffer(stream, buffer, start, count);
                    }
                }

                break;
        }
    }

    /// <summary>
    /// Updates the vertex buffer's data (if it's dynamic)
    /// </summary>
    /// <param name="data">An array of new data</param>
    /// <param name="lengthInBytes">The amount of bytes for the data</param>
    /// <param name="startVertex">The starting vertex</param>
    public void Update(nint data, int lengthInBytes, int startVertex)
    {
        if (Disposed)
        {
            return;
        }

        if (type != VertexBufferType.Dynamic)
        {
            return;
        }

        if (dynamicHandle.Valid == false ||
            data == nint.Zero ||
            lengthInBytes == 0 ||
            lengthInBytes % layout.layout.stride != 0)
        {
            return;
        }

        unsafe
        {
            bgfx.Memory* outData = bgfx.copy((void*)data, (uint)lengthInBytes);

            bgfx.update_dynamic_vertex_buffer(dynamicHandle, (uint)startVertex, outData);
        }
    }

    /// <summary>
    /// Updates the vertex buffer's data (if it's dynamic)
    /// </summary>
    /// <typeparam name="T">A vertex type (probably a struct)</typeparam>
    /// <param name="data">An array of new data</param>
    /// <param name="startVertex">The starting vertex</param>
    public void Update<T>(Span<T> data, int startVertex) where T: unmanaged
    {
        if (Disposed)
        {
            return;
        }

        if (type != VertexBufferType.Dynamic)
        {
            return;
        }

        var size = Marshal.SizeOf<T>();

        if (dynamicHandle.Valid == false ||
            data == null ||
            data.Length == 0 ||
            size != layout.layout.stride)
        {
            return;
        }

        unsafe
        {
            bgfx.Memory* outData;

            fixed (void* dataPtr = data)
            {
                outData = bgfx.copy(dataPtr, (uint)data.Length);
            }

            bgfx.update_dynamic_vertex_buffer(dynamicHandle, (uint)startVertex, outData);
        }
    }

    /// <summary>
    /// Updates the vertex buffer's data (if it's dynamic)
    /// </summary>
    /// <param name="data">An array of new data as bytes</param>
    /// <param name="startVertex">The starting vertex</param>
    public void Update(Span<byte> data, int startVertex)
    {
        if (Disposed)
        {
            return;
        }

        if (type != VertexBufferType.Dynamic)
        {
            return;
        }

        var size = layout.layout.stride;

        if (dynamicHandle.Valid == false || data == null || data.Length == 0 || data.Length % size != 0)
        {
            return;
        }

        unsafe
        {
            bgfx.Memory* outData;

            fixed (void* dataPtr = data)
            {
                outData = bgfx.copy(dataPtr, (uint)data.Length);
            }

            bgfx.update_dynamic_vertex_buffer(dynamicHandle, (uint)startVertex, outData);
        }
    }

    /// <summary>
    /// Creates a dynamic vertex buffer
    /// </summary>
    /// <param name="layout">The vertex layout to use</param>
    /// <param name="allowResize">Whether the buffer can be resized</param>
    /// <param name="elementCount">The element count for the buffer</param>
    /// <returns>The vertex buffer</returns>
    public static VertexBuffer CreateDynamic(VertexLayout layout, bool allowResize = true, uint elementCount = 0)
    {
        unsafe
        {
            fixed (bgfx.VertexLayout* vertexLayout = &layout.layout)
            {
                var flags = allowResize ? RenderBufferFlags.AllowResize : RenderBufferFlags.None;

                var handle = bgfx.create_dynamic_vertex_buffer(elementCount, vertexLayout, (ushort)flags);

                return new VertexBuffer(layout, handle);
            }
        }
    }

    /// <summary>
    /// Checks whether we have enough space for vertices for transient buffers
    /// </summary>
    /// <param name="vertexCount">The amount of vertices we need</param>
    /// <param name="layout">The vertex layout to check</param>
    /// <returns>Whether we have enough space</returns>
    public static bool TransientBufferHasSpace(int vertexCount, VertexLayout layout)
    {
        unsafe
        {
            fixed(bgfx.VertexLayout *l = &layout.layout)
            {
                return bgfx.get_avail_transient_vertex_buffer((uint)vertexCount, l) >= vertexCount;
            }
        }
    }

    /// <summary>
    /// Creates a vertex buffer from an array of data
    /// </summary>
    /// <typeparam name="T">A struct type</typeparam>
    /// <param name="data">An array of vertices</param>
    /// <param name="layout">The vertex layout to use</param>
    /// <param name="isTransient">Whether this buffer is transient (lasts only one frame)</param>
    /// <returns>The vertex buffer, or null</returns>
    public static VertexBuffer Create<T>(Span<T> data, VertexLayout layout, bool isTransient = false) where T: unmanaged
    {
        var size = Marshal.SizeOf<T>();

        if(size != layout.layout.stride)
        {
            return null;
        }

        unsafe
        {
            fixed (bgfx.VertexLayout* vertexLayout = &layout.layout)
            {
                if (isTransient)
                {
                    if (bgfx.get_avail_transient_vertex_buffer((uint)data.Length, vertexLayout) < data.Length)
                    {
                        return null;
                    }

                    bgfx.TransientVertexBuffer buffer;

                    bgfx.alloc_transient_vertex_buffer(&buffer, (uint)data.Length, vertexLayout);

                    fixed (void* dataPtr = data)
                    {
                        Buffer.MemoryCopy(dataPtr, buffer.data, data.Length * size, data.Length * size);
                    }

                    return new VertexBuffer(layout, buffer);
                }
                else
                {
                    bgfx.Memory* outData;

                    fixed (void* dataPtr = data)
                    {
                        outData = bgfx.copy(dataPtr, (uint)(data.Length * size));
                    }

                    var handle = bgfx.create_vertex_buffer(outData, vertexLayout, (ushort)RenderBufferFlags.None);

                    if (handle.Valid)
                    {
                        return new VertexBuffer(layout, handle);
                    }
                }

                return null;
            }
        }
    }

    /// <summary>
    /// Creates a vertex buffer from an array of data
    /// </summary>
    /// <param name="data">An array of vertices</param>
    /// <param name="layout">The vertex layout to use</param>
    /// <param name="isTransient">Whether this buffer is transient (lasts only one frame)</param>
    /// <returns>The vertex buffer, or null</returns>
    public static VertexBuffer Create(Span<byte> data, VertexLayout layout, bool isTransient = false)
    {
        var size = layout.layout.stride;

        if(data == null || data.Length == 0 || data.Length % size != 0)
        {
            return null;
        }

        unsafe
        {
            fixed (bgfx.VertexLayout* vertexLayout = &layout.layout)
            {
                if(isTransient)
                {
                    var vertexCount = (uint)(data.Length / size);

                    if (bgfx.get_avail_transient_vertex_buffer(vertexCount, vertexLayout) < vertexCount)
                    {
                        return null;
                    }

                    bgfx.TransientVertexBuffer buffer;

                    bgfx.alloc_transient_vertex_buffer(&buffer, vertexCount, vertexLayout);

                    fixed(void *dataPtr = data)
                    {
                        Buffer.MemoryCopy(dataPtr, buffer.data, data.Length, data.Length);
                    }

                    return new VertexBuffer(layout, buffer);
                }
                else
                {
                    bgfx.Memory* outData;

                    fixed (void* dataPtr = data)
                    {
                        outData = bgfx.copy(dataPtr, (uint)data.Length);
                    }

                    var handle = bgfx.create_vertex_buffer(outData, vertexLayout, (ushort)RenderBufferFlags.None);

                    if(handle.Valid)
                    {
                        return new VertexBuffer(layout, handle);
                    }
                }

                return null;
            }
        }
    }
}
