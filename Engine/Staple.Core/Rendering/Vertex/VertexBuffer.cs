using Bgfx;
using Staple.Internal;
using System;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Vertex Buffer resource
/// </summary>
public sealed class VertexBuffer
{
    private static VertexBuffer transientBuffer;

    internal bgfx.VertexBufferHandle handle;
    internal bgfx.DynamicVertexBufferHandle dynamicHandle;
    internal bgfx.TransientVertexBuffer transientHandle;
    internal VertexLayout layout;

    public readonly RenderBufferType type;

    /// <summary>
    /// Whether this was destroyed
    /// </summary>
    internal bool Disposed { get; private set; } = false;

    internal unsafe VertexBuffer(VertexLayout layout, bgfx.VertexBufferHandle handle)
    {
        type = RenderBufferType.Normal;

        this.layout = layout;
        this.handle = handle;

        ResourceManager.instance.userCreatedVertexBuffers.Add(new(this));
    }

    internal unsafe VertexBuffer(VertexLayout layout, bgfx.DynamicVertexBufferHandle handle)
    {
        type = RenderBufferType.Dynamic;

        this.layout = layout;

        dynamicHandle = handle;

        ResourceManager.instance.userCreatedVertexBuffers.Add(new(this));
    }

    internal unsafe VertexBuffer(VertexLayout layout, bgfx.TransientVertexBuffer buffer)
    {
        type = RenderBufferType.Transient;

        this.layout = layout;

        transientHandle = buffer;
    }

    ~VertexBuffer()
    {
        Destroy();
    }

    /// <summary>
    /// Destroys the resource
    /// </summary>
    public void Destroy()
    {
        if (Disposed)
        {
            return;
        }

        Disposed = true;

        switch (type)
        {
            case RenderBufferType.Normal:

                if (handle.Valid)
                {
                    bgfx.destroy_vertex_buffer(handle);
                }

                handle = default;

                break;

            case RenderBufferType.Dynamic:

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

        switch (type)
        {
            case RenderBufferType.Normal:

                bgfx.set_vertex_buffer(stream, handle, start, count);

                break;

            case RenderBufferType.Dynamic:

                bgfx.set_dynamic_vertex_buffer(stream, dynamicHandle, start, count);

                break;

            case RenderBufferType.Transient:

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
    /// Sets this buffer as a compute buffer
    /// </summary>
    /// <param name="stage">The buffer stage</param>
    /// <param name="access">The access mode</param>
    public void SetBufferActive(byte stage, Access access)
    {
        if(Disposed || type == RenderBufferType.Transient)
        {
            return;
        }

        switch(type)
        {
            case RenderBufferType.Normal:

                bgfx.set_compute_vertex_buffer(stage, handle, BGFXUtils.GetBGFXAccess(access));

                break;

            case RenderBufferType.Dynamic:

                bgfx.set_compute_dynamic_vertex_buffer(stage, dynamicHandle, BGFXUtils.GetBGFXAccess(access));

                break;
        }
    }

    /// <summary>
    /// Updates the vertex buffer's data (if it's dynamic)
    /// </summary>
    /// <param name="data">An array of new data</param>
    /// <param name="lengthInBytes">The amount of bytes for the data</param>
    /// <param name="startVertex">The starting vertex</param>
    /// <param name="copyMemory">Whether to copy the data to memory (data needs to be available for at least 2 frames)</param>
    public void Update(nint data, int lengthInBytes, int startVertex, bool copyMemory)
    {
        if (Disposed ||
            type != RenderBufferType.Dynamic ||
            dynamicHandle.Valid == false ||
            data == nint.Zero ||
            lengthInBytes == 0 ||
            lengthInBytes % layout.layout.stride != 0)
        {
            return;
        }

        unsafe
        {
            var size = (uint)lengthInBytes;

            bgfx.Memory* outData = copyMemory ? bgfx.alloc(size) : bgfx.make_ref((void *)data, size);

            if(copyMemory)
            {
                var source = new Span<byte>((void*)data, lengthInBytes);
                var target = new Span<byte>(outData->data, lengthInBytes);

                source.CopyTo(target);
            }

            bgfx.update_dynamic_vertex_buffer(dynamicHandle, (uint)startVertex, outData);
        }
    }

    /// <summary>
    /// Updates the vertex buffer's data (if it's dynamic)
    /// </summary>
    /// <typeparam name="T">A vertex type (probably a struct)</typeparam>
    /// <param name="data">An array of new data</param>
    /// <param name="startVertex">The starting vertex</param>
    /// <param name="copyMemory">Whether to copy the data to memory (data needs to be available for at least 2 frames)</param>
    public void Update<T>(Span<T> data, int startVertex, bool copyMemory) where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();

        if (Disposed ||
            type != RenderBufferType.Dynamic ||
            dynamicHandle.Valid == false ||
            data.Length == 0 ||
            size != layout.layout.stride)
        {
            return;
        }

        unsafe
        {
            fixed(void *ptr = data)
            {
                var dataSize = (uint)(data.Length * size);

                bgfx.Memory* outData = copyMemory ? bgfx.alloc(dataSize) : bgfx.make_ref(ptr, dataSize);

                if(copyMemory)
                {
                    var target = new Span<T>(outData->data, data.Length);

                    data.CopyTo(target);
                }

                bgfx.update_dynamic_vertex_buffer(dynamicHandle, (uint)startVertex, outData);
            }
        }
    }

    /// <summary>
    /// Updates the vertex buffer's data (if it's dynamic)
    /// </summary>
    /// <param name="data">An array of new data as bytes</param>
    /// <param name="startVertex">The starting vertex</param>
    /// <param name="copyMemory">Whether to copy the data to memory (data needs to be available for at least 2 frames)</param>
    public void Update(Span<byte> data, int startVertex, bool copyMemory)
    {
        var size = layout.layout.stride;

        if (Disposed ||
            type != RenderBufferType.Dynamic ||
            dynamicHandle.Valid == false ||
            data.Length == 0 ||
            data.Length % size != 0)
        {
            return;
        }

        unsafe
        {
            fixed (void* ptr = data)
            {
                var dataSize = (uint)data.Length;

                bgfx.Memory* outData = copyMemory ? bgfx.alloc((uint)data.Length) : bgfx.make_ref(ptr, dataSize);

                if(copyMemory)
                {
                    var target = new Span<byte>(outData->data, data.Length);

                    data.CopyTo(target);
                }

                bgfx.update_dynamic_vertex_buffer(dynamicHandle, (uint)startVertex, outData);
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
        if (vertexCount <= 0)
        {
            return false;
        }

        unsafe
        {
            fixed (bgfx.VertexLayout* l = &layout.layout)
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
    /// <param name="flags">Additional flags</param>
    /// <returns>The vertex buffer, or null</returns>
    public static VertexBuffer Create<T>(Span<T> data, VertexLayout layout, RenderBufferFlags flags = RenderBufferFlags.None) where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();

        if (size != layout.layout.stride)
        {
            return null;
        }

        unsafe
        {
            fixed (bgfx.VertexLayout* vertexLayout = &layout.layout)
            {
                bgfx.Memory* outData = bgfx.alloc((uint)(data.Length * size));

                var target = new Span<T>(outData->data, data.Length);

                data.CopyTo(target);

                var handle = bgfx.create_vertex_buffer(outData, vertexLayout, (ushort)BGFXUtils.GetBGFXBufferFlags(flags));

                if (handle.Valid)
                {
                    return new VertexBuffer(layout, handle);
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
    /// <param name="flags">Additional flags</param>
    /// <returns>The vertex buffer, or null</returns>
    public static VertexBuffer Create(Span<byte> data, VertexLayout layout, RenderBufferFlags flags = RenderBufferFlags.None)
    {
        var size = layout.layout.stride;

        if (data.Length == 0 || data.Length % size != 0)
        {
            return null;
        }

        unsafe
        {
            fixed (bgfx.VertexLayout* vertexLayout = &layout.layout)
            {
                bgfx.Memory* outData = bgfx.alloc((uint)data.Length);

                var target = new Span<byte>(outData->data, data.Length);

                data.CopyTo(target);

                var handle = bgfx.create_vertex_buffer(outData, vertexLayout, (ushort)BGFXUtils.GetBGFXBufferFlags(flags));

                if (handle.Valid)
                {
                    return new VertexBuffer(layout, handle);
                }

                return null;
            }
        }
    }

    /// <summary>
    /// Creates a dynamic vertex buffer
    /// </summary>
    /// <param name="layout">The vertex layout to use</param>
    /// <param name="flags">Additional flags</param>
    /// <param name="allowResize">Whether the buffer can be resized</param>
    /// <param name="elementCount">The element count for the buffer</param>
    /// <returns>The vertex buffer</returns>
    public static VertexBuffer CreateDynamic(VertexLayout layout, RenderBufferFlags flags = RenderBufferFlags.None,
        bool allowResize = true, uint elementCount = 0)
    {
        unsafe
        {
            fixed (bgfx.VertexLayout* vertexLayout = &layout.layout)
            {
                if (allowResize)
                {
                    flags |= RenderBufferFlags.AllowResize;
                }

                var handle = bgfx.create_dynamic_vertex_buffer(elementCount, vertexLayout, (ushort)BGFXUtils.GetBGFXBufferFlags(flags));

                if(handle.Valid)
                {
                    return new VertexBuffer(layout, handle);
                }

                return null;
            }
        }
    }

    /// <summary>
    /// Creates a transient vertex buffer from an array of data
    /// </summary>
    /// <typeparam name="T">A struct type</typeparam>
    /// <param name="data">An array of vertices</param>
    /// <param name="layout">The vertex layout to use</param>
    /// <returns>The transient vertex buffer, or null</returns>
    public static VertexBuffer CreateTransient<T>(Span<T> data, VertexLayout layout) where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();

        if (size != layout.layout.stride)
        {
            return null;
        }

        unsafe
        {
            fixed (bgfx.VertexLayout* vertexLayout = &layout.layout)
            {
                if (bgfx.get_avail_transient_vertex_buffer((uint)data.Length, vertexLayout) < data.Length)
                {
                    return null;
                }

                bgfx.TransientVertexBuffer buffer;

                bgfx.alloc_transient_vertex_buffer(&buffer, (uint)data.Length, vertexLayout);

                var target = new Span<T>(buffer.data, data.Length);

                data.CopyTo(target);

                transientBuffer ??= new(layout, buffer);

                transientBuffer.layout = layout;
                transientBuffer.transientHandle = buffer;

                return transientBuffer;
            }
        }
    }

    /// <summary>
    /// Creates a transient vertex buffer from an array of data.
    /// </summary>
    /// <param name="data">An array of vertices</param>
    /// <param name="layout">The vertex layout to use</param>
    /// <returns>The vertex buffer, or null</returns>
    public static VertexBuffer CreateTransient(Span<byte> data, VertexLayout layout)
    {
        var size = layout.layout.stride;

        if (data.Length == 0 || data.Length % size != 0)
        {
            return null;
        }

        unsafe
        {
            fixed (bgfx.VertexLayout* vertexLayout = &layout.layout)
            {
                var vertexCount = (uint)(data.Length / size);

                if (bgfx.get_avail_transient_vertex_buffer(vertexCount, vertexLayout) < vertexCount)
                {
                    return null;
                }

                bgfx.TransientVertexBuffer buffer;

                bgfx.alloc_transient_vertex_buffer(&buffer, vertexCount, vertexLayout);

                var target = new Span<byte>(buffer.data, data.Length);

                data.CopyTo(target);

                transientBuffer ??= new(layout, buffer);

                transientBuffer.layout = layout;
                transientBuffer.transientHandle = buffer;

                return transientBuffer;
            }
        }
    }
}
