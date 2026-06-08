using System;
using System.Runtime.InteropServices;
using System.Text;

namespace NfdSharp
{
    public static class Utils
    {
        /// <summary>
        /// Converts a .NET string into a IntPtr for NFD on the heap.
        ///
        /// The returned pointer has to be freed afterwards with
        /// <see cref="Marshal.FreeHGlobal"/>
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static IntPtr ToNfdString(string str)
        {
            if (str == "")
                return IntPtr.Zero;
            
            byte[] bytes = Encoding.UTF8.GetBytes(str);
            byte[] bytesTerminated = new byte[bytes.Length + 1];
            Array.Copy(bytes, bytesTerminated, bytes.Length);
            bytesTerminated[bytes.Length] = 0;
            
            var ptr = Marshal.AllocHGlobal(bytesTerminated.Length);
            Marshal.Copy(bytesTerminated, 0, ptr, bytesTerminated.Length);

            return ptr;
        }

        public static string FromNfdString(IntPtr str)
        {
            byte[] bytes = new byte[4096];
            Marshal.Copy(str, bytes, 0, 1);
            int i = 0;
            while (i < 4095 && bytes[i++] != 0)
            {
                Marshal.Copy(str, bytes, 0, i + 1);
            }
            i--;

            byte[] outPathB = new byte[i];
            Array.Copy(bytes, outPathB, i);

            return Encoding.UTF8.GetString(outPathB);
        }
    }
}