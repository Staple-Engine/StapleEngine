using System;

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
        public static bool IsWindows => OperatingSystem.IsWindows();

        /// <summary>
        /// Whether we're running on linux
        /// </summary>
        public static bool IsLinux => OperatingSystem.IsLinux() || OperatingSystem.IsFreeBSD();

        /// <summary>
        /// Whether we're running on macOS
        /// </summary>
        public static bool IsMacOS => OperatingSystem.IsMacOS();

        /// <summary>
        /// Whether we're running on android
        /// </summary>
        public static bool IsAndroid => OperatingSystem.IsAndroid();

        /// <summary>
        /// Whether we're running on iOS
        /// </summary>
        public static bool IsiOS => OperatingSystem.IsIOS();
    }
}
