using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    internal class IndexBuffer
    {
        public unsafe bgfx.Memory* data;
        public bgfx.IndexBufferHandle handle;

        public static IndexBuffer Create(ushort[] data, RenderBufferFlags flags)
        {
            var size = Marshal.SizeOf(data);

            unsafe
            {
                bgfx.Memory* outData;

                fixed (void* dataPtr = data)
                {
                    outData = bgfx.copy(dataPtr, (uint)size);
                }

                var handle = bgfx.create_index_buffer(outData, (ushort)flags);

                return new IndexBuffer()
                {
                    data = outData,
                    handle = handle,
                };
            }
        }

        public static IndexBuffer Create(uint[] data, RenderBufferFlags flags)
        {
            var size = Marshal.SizeOf(data);

            unsafe
            {
                bgfx.Memory* outData;

                fixed (void* dataPtr = data)
                {
                    outData = bgfx.copy(dataPtr, (uint)size);
                }

                var handle = bgfx.create_index_buffer(outData, (ushort)(flags | RenderBufferFlags.Index32));

                return new IndexBuffer()
                {
                    data = outData,
                    handle = handle,
                };
            }
        }
    }
}
