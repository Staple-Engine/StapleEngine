using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    internal class VertexBuffer
    {
        public unsafe bgfx.Memory *data;
        public VertexLayout layout;
        public bgfx.VertexBufferHandle handle;

        public static VertexBuffer Create<T>(T[] data, VertexLayout layout)
        {
            var size = Marshal.SizeOf(data);

            byte[] buffer = new byte[size];

            IntPtr ptr = IntPtr.Zero;

            unsafe
            {
                bgfx.Memory* outData;

                try
                {
                    ptr = Marshal.AllocHGlobal(size);
                    Marshal.StructureToPtr(data, ptr, true);
                    Marshal.Copy(ptr, buffer, 0, size);
                }
                finally
                {
                    Marshal.FreeHGlobal(ptr);
                }

                fixed(void * dataPtr = buffer)
                {
                    outData = bgfx.copy(dataPtr, (uint)size);
                }

                fixed(bgfx.VertexLayout *vertexLayout = &layout.layout)
                {
                    var handle = bgfx.create_vertex_buffer(outData, vertexLayout, (ushort)RenderBufferFlags.None);

                    return new VertexBuffer()
                    {
                        layout = layout,
                        data = outData,
                        handle = handle,
                    };
                }
            }
        }
    }
}
