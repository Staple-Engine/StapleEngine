namespace Bgfx
{
    public static partial class bgfx
    {
#if STAPLE_WINDOWS
        const string DllName = "bgfx.dll";
#elif STAPLE_LINUX
        const string DllName = "libbgfx.so";
#elif STAPLE_OSX
        const string DllName = "libbgfx.dylib";
#elif ANDROID
        const string DllName = "libbgfx.so";
#elif WINDOWS
        const string DllName = "bgfx.dll";
#elif OSX
        const string DllName = "libbgfx.dylib";
#elif LINUX
        const string DllName = "libbgfx.so";
#else
        const string DllName = "invalid";
#endif
    }
}
