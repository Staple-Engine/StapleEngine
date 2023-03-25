using System.Runtime.InteropServices;

namespace Bgfx
{
    public static partial class bgfx
    {
        #if WINDOWS
        const string DllName = "bgfx.dll";
        #elif OSX
        const string DllName = "libbgfx.dylib";
        #elif LINUX
        const string DllName = "libbgfx.so";
        #endif
    }
}
