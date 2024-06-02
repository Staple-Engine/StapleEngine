using System;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal static class StapleSupport
{
#if STAPLE_WINDOWS
    const string DllName = "staplesupport.dll";
#elif STAPLE_LINUX
    const string DllName = "libstaplesupport.so";
#elif STAPLE_OSX
    const string DllName = "libstaplesupport.dylib";
#elif ANDROID
    const string DllName = "libstaplesupport.so";
#elif WINDOWS
    const string DllName = "staplesupport.dll";
#elif OSX
    const string DllName = "libstaplesupport.dylib";
#elif LINUX
    const string DllName = "libstaplesupport.so";
#else
    const string DllName = "invalid";
#endif

    [DllImport(DllName, EntryPoint = "MacWindow", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint MacWindow(nint windowHandle);
}
