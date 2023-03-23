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
        public readonly int length;

        private bool destroyed = false;

        internal unsafe VertexBuffer(bgfx.Memory* data, VertexLayout layout, bgfx.VertexBufferHandle handle, int length)
        {
            this.data = data;
            this.layout = layout;
            this.handle = handle;
            this.length = length;
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
        }

        /// <summary>
        /// Sets the buffer active
        /// </summary>
        /// <param name="stream">The stream to use</param>
        /// <param name="start">Vertex index to start at</param>
        /// <param name="count">Vertex count to use</param>
        internal void SetActive(byte stream, uint start, uint count)
        {
            bgfx.set_vertex_buffer(stream, handle, start, count);
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
                bgfx.Memory* outData;

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

                fixed(void * dataPtr = buffer)
                {
                    outData = bgfx.copy(dataPtr, (uint)buffer.Length);
                }

                fixed(bgfx.VertexLayout *vertexLayout = &layout.layout)
                {
                    var handle = bgfx.create_vertex_buffer(outData, vertexLayout, (ushort)RenderBufferFlags.None);

                    return new VertexBuffer(outData, layout, handle, data.Length);
                }
            }
        }
    }
}
