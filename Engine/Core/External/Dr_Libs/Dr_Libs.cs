using Staple;
using System.Runtime.InteropServices;

namespace DrLibs
{
    [AdditionalLibrary(AppPlatform.Android, "dr_libs")]
    public static partial class DrMp3
    {
#if STAPLE_WINDOWS
        const string DllName = "dr_libs.dll";
#elif STAPLE_LINUX
        const string DllName = "libdr_libs.so";
#elif STAPLE_OSX
        const string DllName = "libdr_libs.dylib";
#elif ANDROID
        const string DllName = "libdr_libs.so";
#elif WINDOWS
        const string DllName = "dr_libs.dll";
#elif OSX
        const string DllName = "libdr_libs.dylib";
#elif LINUX
        const string DllName = "libdr_libs.so";
#else
        const string DllName = "invalid";
#endif

        [LibraryImport(DllName, EntryPoint = "LoadMP3")]
        [UnmanagedCallConv(CallConvs = new System.Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static unsafe partial nint LoadMP3(byte *ptr, int size, int* channels, int* bitsPerChannel, int* sampleRate, float* duration, int* requiredSize);

        [LibraryImport(DllName, EntryPoint = "GetMP3Buffer")]
        [UnmanagedCallConv(CallConvs = new System.Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static unsafe partial nint GetMP3Buffer(nint ptr);

        [LibraryImport(DllName, EntryPoint = "FreeMP3")]
        [UnmanagedCallConv(CallConvs = new System.Type[] { typeof(System.Runtime.CompilerServices.CallConvCdecl) })]
        public static unsafe partial void FreeMP3(nint ptr);
    }
}