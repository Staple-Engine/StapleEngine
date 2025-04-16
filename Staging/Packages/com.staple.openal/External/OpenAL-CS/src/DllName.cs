namespace OpenAL
{
    public static partial class AL10
    {
#if STAPLE_WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif STAPLE_LINUX
        const string nativeLibName = "libopenal.so.1";
#elif STAPLE_OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif ANDROID
        const string nativeLibName = "libopenal32.so";
#elif WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif LINUX
        const string nativeLibName = "libopenal.so.1";
#else
        const string nativeLibName = "invalid";
#endif
    }

    public static partial class AL11
    {
#if STAPLE_WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif STAPLE_LINUX
        const string nativeLibName = "libopenal.so.1";
#elif STAPLE_OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif ANDROID
        const string nativeLibName = "libopenal32.so";
#elif WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif LINUX
        const string nativeLibName = "libopenal.so.1";
#else
        const string nativeLibName = "invalid";
#endif
    }

    public static partial class ALC10
    {
#if STAPLE_WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif STAPLE_LINUX
        const string nativeLibName = "libopenal.so.1";
#elif STAPLE_OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif ANDROID
        const string nativeLibName = "libopenal32.so";
#elif WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif LINUX
        const string nativeLibName = "libopenal.so.1";
#else
        const string nativeLibName = "invalid";
#endif
    }

    public static partial class ALC11
    {
#if STAPLE_WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif STAPLE_LINUX
        const string nativeLibName = "libopenal.so.1";
#elif STAPLE_OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif ANDROID
        const string nativeLibName = "libopenal32.so";
#elif WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif LINUX
        const string nativeLibName = "libopenal.so.1";
#else
        const string nativeLibName = "invalid";
#endif
    }

    public static partial class ALEXT
    {
#if STAPLE_WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif STAPLE_LINUX
        const string nativeLibName = "libopenal.so.1";
#elif STAPLE_OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif ANDROID
        const string nativeLibName = "libopenal32.so";
#elif WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif LINUX
        const string nativeLibName = "libopenal.so.1";
#else
        const string nativeLibName = "invalid";
#endif
    }

    public static partial class EFX
    {
#if STAPLE_WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif STAPLE_LINUX
        const string nativeLibName = "libopenal.so.1";
#elif STAPLE_OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif ANDROID
        const string nativeLibName = "libopenal32.so";
#elif WINDOWS
        const string nativeLibName = "soft_oal.dll";
#elif OSX
        const string nativeLibName = "libopenal.1.dylib";
#elif LINUX
        const string nativeLibName = "libopenal.so.1";
#else
        const string nativeLibName = "invalid";
#endif
    }
}