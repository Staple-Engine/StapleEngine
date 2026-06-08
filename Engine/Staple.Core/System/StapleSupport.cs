using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Support library for some internal things.
/// </summary>
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

    /// <summary>
    /// Gets the native handle for a window on macOS
    /// </summary>
    /// <param name="windowHandle">The window handle</param>
    /// <returns>The native handle</returns>
    [DllImport(DllName, EntryPoint = "MacWindow", CallingConvention = CallingConvention.Cdecl)]
    public static extern nint MacWindow(nint windowHandle);
}
