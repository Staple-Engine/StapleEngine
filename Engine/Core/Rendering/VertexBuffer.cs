using Bgfx;
using System;
using System.Runtime.InteropServices;

namespace Staple.Internal
{
    /// <summary>
    /// Vertex Buffer resource
    /// </summary>
    internal class VertexBuffer
    {
        public VertexLayout layout;
        public bgfx.VertexBufferHandle handle;
        public bgfx.DynamicVertexBufferHandle dynamicHandle;
        public bgfx.TransientVertexBuffer transientHandle;
        public readonly VertexBufferType type;

        private bool destroyed = false;

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
            if (destroyed)
            {
                return;
            }

            destroyed = true;

            switch(type)
            {
                case VertexBufferType.Normal:

                    if (handle.Valid)
                    {
                        bgfx.destroy_vertex_buffer(handle);
                    }

                    break;

                case VertexBufferType.Dynamic:

                    if (dynamicHandle.Valid)
                    {
                        bgfx.destroy_dynamic_vertex_buffer(dynamicHandle);
                    }

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
            if(handle.Valid)
            {
                bgfx.set_vertex_buffer(stream, handle, start, count);
            }
            else if(dynamicHandle.Valid)
            {
                bgfx.set_dynamic_vertex_buffer(stream, dynamicHandle, start, count);
            }
            else if(transientHandle.handle.Valid)
            {
                unsafe
                {
                    fixed(bgfx.TransientVertexBuffer *buffer = &transientHandle)
                    {
                        bgfx.set_transient_vertex_buffer(stream, buffer, start, count);
                    }
                }
            }
        }

        /// <summary>
        /// Updates the vertex buffer's data (if it's dynamic)
        /// </summary>
        /// <typeparam name="T">A vertex type (probably a struct)</typeparam>
        /// <param name="data">An array of new data</param>
        /// <param name="startVertex">The starting vertex</param>
        public void Update<T>(T[] data, int startVertex) where T: unmanaged
        {
            if(type != VertexBufferType.Dynamic)
            {
                return;
            }

            var size = Marshal.SizeOf(typeof(T));

            if (dynamicHandle.Valid == false ||
                data == null ||
                data.Length == 0 ||
                size != layout.layout.stride)
            {
                return;
            }

            byte[] buffer = new byte[size * data.Length];

            IntPtr ptr = IntPtr.Zero;

            unsafe
            {
                try
                {
                    ptr = Marshal.AllocHGlobal(size);

                    for (var i = 0; i < data.Length; i++)
                    {
                        Marshal.StructureToPtr(data[i], ptr, true);
                        Marshal.Copy(ptr, buffer, i * size, size);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                Update(buffer, startVertex);
            }
        }

        /// <summary>
        /// Updates the vertex buffer's data (if it's dynamic)
        /// </summary>
        /// <param name="data">An array of new data as bytes</param>
        /// <param name="startVertex">The starting vertex</param>
        public void Update(byte[] data, int startVertex)
        {
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
        public static VertexBuffer Create<T>(T[] data, VertexLayout layout, bool isTransient = false) where T: unmanaged
        {
            var size = Marshal.SizeOf(typeof(T));

            if(size != layout.layout.stride)
            {
                return null;
            }

            var buffer = new byte[size * data.Length];

            IntPtr ptr = IntPtr.Zero;

            unsafe
            {
                try
                {
                    ptr = Marshal.AllocHGlobal(size);

                    for(var i = 0; i < data.Length; i++)
                    {
                        Marshal.StructureToPtr(data[i], ptr, true);
                        Marshal.Copy(ptr, buffer, i * size, size);
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                return Create(buffer, layout, isTransient);
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
        public static VertexBuffer Create(byte[] data, VertexLayout layout, bool isTransient = false)
        {
            var size = layout.layout.stride;

            if(data == null || data.Length == 0 || data.Length % size != 0)
            {
                return null;
            }

            IntPtr ptr = IntPtr.Zero;

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

                        try
                        {
                            Marshal.Copy(data, 0, (nint)buffer.data, data.Length);
                        }
                        catch(Exception)
                        {
                            return null;
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
}
