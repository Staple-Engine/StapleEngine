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
        public unsafe bgfx.Memory *data;
        public VertexLayout layout;
        public bgfx.VertexBufferHandle handle;
        public bgfx.DynamicVertexBufferHandle dynamicHandle;

        private bool destroyed = false;

        internal unsafe VertexBuffer(bgfx.Memory* data, VertexLayout layout, bgfx.VertexBufferHandle handle)
        {
            this.data = data;
            this.layout = layout;
            this.handle = handle;
        }

        internal unsafe VertexBuffer(VertexLayout layout, bgfx.DynamicVertexBufferHandle handle)
        {
            this.layout = layout;
            dynamicHandle = handle;
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

            if (handle.Valid)
            {
                bgfx.destroy_vertex_buffer(handle);
            }

            if(dynamicHandle.Valid)
            {
                bgfx.destroy_dynamic_vertex_buffer(dynamicHandle);
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
        }

        /// <summary>
        /// Updates the vertex buffer's data (if it's dynamic)
        /// </summary>
        /// <typeparam name="T">A vertex type (probably a struct)</typeparam>
        /// <param name="data">An array of new data</param>
        /// <param name="startVertex">The starting vertex</param>
        public void Update<T>(T[] data, int startVertex) where T: unmanaged
        {
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
                bgfx.Memory* outData;

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
        /// Creates a vertex buffer from an array of data
        /// </summary>
        /// <typeparam name="T">A struct type</typeparam>
        /// <param name="data">An array of vertices</param>
        /// <param name="layout">The vertex layout to use</param>
        /// <returns>The vertex buffer, or null</returns>
        public static VertexBuffer Create<T>(T[] data, VertexLayout layout) where T: unmanaged
        {
            var size = Marshal.SizeOf(typeof(T));

            if(size != layout.layout.stride)
            {
                return null;
            }

            byte[] buffer = new byte[size * data.Length];

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

                return Create(buffer, layout);
            }
        }

        /// <summary>
        /// Creates a vertex buffer from an array of data
        /// </summary>
        /// <typeparam name="T">A struct type</typeparam>
        /// <param name="data">An array of vertices</param>
        /// <param name="layout">The vertex layout to use</param>
        /// <returns>The vertex buffer, or null</returns>
        public static VertexBuffer Create(byte[] data, VertexLayout layout)
        {
            var size = layout.layout.stride;

            if(data == null || data.Length == 0 || data.Length % size != 0)
            {
                return null;
            }

            IntPtr ptr = IntPtr.Zero;

            unsafe
            {
                bgfx.Memory* outData;

                fixed (void* dataPtr = data)
                {
                    outData = bgfx.copy(dataPtr, (uint)data.Length);
                }

                fixed (bgfx.VertexLayout* vertexLayout = &layout.layout)
                {
                    var handle = bgfx.create_vertex_buffer(outData, vertexLayout, (ushort)RenderBufferFlags.None);

                    return new VertexBuffer(outData, layout, handle);
                }
            }
        }
    }
}
