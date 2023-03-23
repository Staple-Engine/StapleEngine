using Bgfx;
using System.Runtime.InteropServices;

namespace Staple.Internal
{
    /// <summary>
    /// Index Buffer resource
    /// </summary>
    internal class IndexBuffer
    {
        /// <summary>
        /// The index buffer's data
        /// </summary>
        public unsafe bgfx.Memory* data;

        /// <summary>
        /// The index buffer's handle
        /// </summary>
        public bgfx.IndexBufferHandle handle;

        /// <summary>
        /// The index buffer's data length
        /// </summary>
        public readonly int length;

        /// <summary>
        /// Whether this was destroyed
        /// </summary>
        private bool destroyed = false;

        public unsafe IndexBuffer(bgfx.Memory* data, bgfx.IndexBufferHandle handle, int length)
        {
            this.data = data;
            this.handle = handle;
            this.length = length;
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
            if (destroyed)
            {
                return;
            }

            destroyed = true;

            if (handle.Valid)
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
            bgfx.set_index_buffer(handle, start, count);
        }

        /// <summary>
        /// Creates an index buffer from ushort data
        /// </summary>
        /// <param name="data">The data</param>
        /// <param name="flags">The buffer flags</param>
        /// <returns>The index buffer, or null</returns>
        public static IndexBuffer Create(ushort[] data, RenderBufferFlags flags)
        {
            var size = Marshal.SizeOf(typeof(ushort));

            unsafe
            {
                bgfx.Memory* outData;

                fixed (void* dataPtr = data)
                {
                    outData = bgfx.copy(dataPtr, (uint)(size * data.Length));
                }

                var handle = bgfx.create_index_buffer(outData, (ushort)flags);

                return new IndexBuffer(outData, handle, data.Length);
            }
        }

        /// <summary>
        /// Creates an index buffer from uint data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="flags"></param>
        /// <param name="data">The data</param>
        /// <param name="flags">The buffer flags</param>
        /// <returns>The index buffer, or null</returns>
        public static IndexBuffer Create(uint[] data, RenderBufferFlags flags)
        {
            var size = Marshal.SizeOf(typeof(uint));

            unsafe
            {
                bgfx.Memory* outData;

                fixed (void* dataPtr = data)
                {
                    outData = bgfx.copy(dataPtr, (uint)(size * data.Length));
                }

                var handle = bgfx.create_index_buffer(outData, (ushort)(flags | RenderBufferFlags.Index32));

                return new IndexBuffer(outData, handle, data.Length);
            }
        }
    }
}
