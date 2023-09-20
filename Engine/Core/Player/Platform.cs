using System.Runtime.InteropServices;

namespace Staple
{
    /// <summary>
    /// Platform information
    /// </summary>
    public static class Platform
    {
        /// <summary>
        /// Gets the current platform. If it's unsupported, it'll return null.
        /// </summary>
        public static AppPlatform? CurrentPlatform
        {
            get
            {
                if(IsWindows)
                {
                    return AppPlatform.Windows;
                }

                if(IsLinux)
                {
                    return AppPlatform.Linux;
                }

                if(IsMacOS)
                {
                    return AppPlatform.MacOSX;
                }

                if(IsAndroid)
                {
                    return AppPlatform.Android;
                }

                if(IsiOS)
                {
                    return AppPlatform.iOS;
                }

                return null;
            }
        }

        /// <summary>
        /// Whether we're running on windows
        /// </summary>
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        /// <summary>
        /// Whether we're running on linux
        /// </summary>
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);

        /// <summary>
        /// Whether we're running on macOS
        /// </summary>
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        /// <summary>
        /// Whether we're running on android
        /// </summary>
#if ANDROID
        public static bool IsAndroid => true;
#else
        public static bool IsAndroid => false;
#endif

        /// <summary>
        /// Whether we're running on iOS
        /// </summary>
#if IOS
        public static bool IsiOS => true;
#else
        public static bool IsiOS => false;
#endif
    }
}
