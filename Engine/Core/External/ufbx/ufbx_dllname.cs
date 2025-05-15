namespace ufbx
{
#if UFBX_INTERNAL
internal
#else
public
#endif
    partial class ufbx
    {
#if STAPLE_WINDOWS
        const string DllName = "ufbx.dll";
#elif STAPLE_LINUX
        const string DllName = "libufbx.so";
#elif STAPLE_OSX
        const string DllName = "libufbx.dylib";
#elif ANDROID
        const string DllName = "libufbx.so";
#elif WINDOWS
        const string DllName = "ufbx.dll";
#elif OSX
        const string DllName = "libufbx.dylib";
#elif LINUX
        const string DllName = "libufbx.so";
#else
        const string DllName = "invalid";
#endif
    }
}
