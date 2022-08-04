using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    internal class IndexBuffer
    {
        public unsafe bgfx.Memory* data;
        public bgfx.IndexBufferHandle handle;
        public readonly int length;
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

        public void SetActive(uint start, uint count)
        {
            bgfx.set_index_buffer(handle, start, count);
        }

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
