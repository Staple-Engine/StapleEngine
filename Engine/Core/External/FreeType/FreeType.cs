using System.Runtime.InteropServices;

namespace Staple.Internal
{
    [AdditionalLibrary(AppPlatform.Android, "StapleSupport")]
    [AdditionalLibrary(AppPlatform.Android, "freetype")]
    public static partial class FreeType
    {
#if STAPLE_WINDOWS
        const string DllName = "StapleSupport.dll";
#elif STAPLE_LINUX
        const string DllName = "libStapleSupport.so";
#elif STAPLE_OSX
        const string DllName = "libStapleSupport.dylib";
#elif ANDROID
        const string DllName = "libStapleSupport.so";
#elif WINDOWS
        const string DllName = "StapleSupport.dll";
#elif OSX
        const string DllName = "libStapleSupport.dylib";
#elif LINUX
        const string DllName = "libStapleSupport.so";
#else
        const string DllName = "invalid";
#endif

        [StructLayout(LayoutKind.Sequential, Pack = 0)]
        public unsafe struct Glyph
        {
            public byte* bitmap;
            public uint xOffset;
            public uint yOffset;
            public uint width;
            public uint height;
            public uint xAdvance;
        }

        [LibraryImport(DllName, EntryPoint = "FreeTypeLoadFont")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial nint LoadFont(byte* ptr, int size);

        [LibraryImport(DllName, EntryPoint = "FreeTypeLineSpacing")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial int LineSpacing(nint ptr, uint fontSize);

        [LibraryImport(DllName, EntryPoint = "FreeTypeKerning")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial int Kerning(nint ptr, uint from, uint to, uint fontSize);

        [LibraryImport(DllName, EntryPoint = "FreeTypeLoadGlyph")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial nint LoadGlyph(nint ptr, uint character, uint fontSize, Color textColor, Color secondaryTextColor,
            int borderSize, Color borderColor);

        [LibraryImport(DllName, EntryPoint = "FreeTypeFreeGlyph")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial void FreeGlyph(nint ptr);

        [LibraryImport(DllName, EntryPoint = "FreeTypeFreeFont")]
        [UnmanagedCallConv(CallConvs = [typeof(System.Runtime.CompilerServices.CallConvCdecl)])]
        public static unsafe partial void FreeFont(nint ptr);
    }
}
