using Bgfx;
using System;
using System.Runtime.InteropServices;

namespace Staple.Internal
{
    /// <summary>
    /// Index Buffer resource
    /// </summary>
    internal class IndexBuffer
    {
        /// <summary>
        /// The index buffer's handle
        /// </summary>
        public bgfx.IndexBufferHandle handle;

        /// <summary>
        /// The index buffer's transient handle
        /// </summary>
        public bgfx.TransientIndexBuffer transientHandle;

        /// <summary>
        /// Whether this was destroyed
        /// </summary>
        internal bool Disposed { get; private set; } = false;

        /// <summary>
        /// Whether this buffer is transient
        /// </summary>
        private bool isTransient = false;

        public unsafe IndexBuffer(bgfx.IndexBufferHandle handle)
        {
            this.handle = handle;

            isTransient = false;
        }

        public unsafe IndexBuffer(bgfx.TransientIndexBuffer handle)
        {
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

            if (handle.Valid)
            {
                bgfx.set_index_buffer(handle, start, count);
            }
            else if(transientHandle.handle.Valid)
            {
                unsafe
                {
                    fixed(bgfx.TransientIndexBuffer *buffer = &transientHandle)
                    {
                        bgfx.set_transient_index_buffer(buffer, start, count);
                    }
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
            return bgfx.get_avail_transient_index_buffer((uint)indexCount, isInt32) >= indexCount;
        }

        /// <summary>
        /// Creates an index buffer from ushort data
        /// </summary>
        /// <param name="data">The data</param>
        /// <param name="flags">The buffer flags</param>
        /// <param name="isTransient">Whether this buffer is transient (lasts only one frame)</param>
        /// <returns>The index buffer, or null</returns>
        public static IndexBuffer Create(ushort[] data, RenderBufferFlags flags, bool isTransient = false)
        {
            var size = Marshal.SizeOf<ushort>();

            if(isTransient && bgfx.get_avail_transient_index_buffer((uint)data.Length, false) < data.Length)
            {
                return null;
            }

            unsafe
            {
                if(isTransient)
                {
                    bgfx.TransientIndexBuffer handle;

                    bgfx.alloc_transient_index_buffer(&handle, (uint)data.Length, false);

                    try
                    {
                        var temp = new byte[size * data.Length];

                        Buffer.BlockCopy(data, 0, temp, 0, temp.Length);

                        Marshal.Copy(temp, 0, (nint)handle.data, data.Length);
                    }
                    catch (Exception)
                    {
                        return null;
                    }

                    return new IndexBuffer(handle);
                }
                else
                {
                    bgfx.Memory* outData;

                    fixed (void* dataPtr = data)
                    {
                        outData = bgfx.copy(dataPtr, (uint)(size * data.Length));
                    }

                    var handle = bgfx.create_index_buffer(outData, (ushort)flags);

                    return new IndexBuffer(handle);
                }
            }
        }

        /// <summary>
        /// Creates an index buffer from uint data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="flags"></param>
        /// <param name="data">The data</param>
        /// <param name="flags">The buffer flags</param>
        /// <param name="isTransient">Whether this buffer is transient (lasts only one frame)</param>
        /// <returns>The index buffer, or null</returns>
        public static IndexBuffer Create(uint[] data, RenderBufferFlags flags, bool isTransient = false)
        {
            var size = Marshal.SizeOf<uint>();

            unsafe
            {
                if (isTransient)
                {
                    bgfx.TransientIndexBuffer handle;

                    bgfx.alloc_transient_index_buffer(&handle, (uint)data.Length, true);

                    try
                    {
                        var temp = new byte[size * data.Length];

                        Buffer.BlockCopy(data, 0, temp, 0, temp.Length);

                        Marshal.Copy(temp, 0, (nint)handle.data, data.Length);
                    }
                    catch (Exception)
                    {
                        return null;
                    }

                    return new IndexBuffer(handle);
                }
                else
                {
                    bgfx.Memory* outData;

                    fixed (void* dataPtr = data)
                    {
                        outData = bgfx.copy(dataPtr, (uint)(size * data.Length));
                    }

                    var handle = bgfx.create_index_buffer(outData, (ushort)(flags | RenderBufferFlags.Index32));

                    return new IndexBuffer(handle);
                }
            }
        }
    }
}
