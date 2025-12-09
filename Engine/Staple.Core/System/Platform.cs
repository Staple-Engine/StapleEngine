using Staple.Internal;
using System;

namespace Staple;

/// <summary>
/// Platform information
/// </summary>
public static class Platform
{
    public const int StapleVersionMajor = 0;
    public const int StapleVersionMinor = 2;

    internal static IPlatformProvider platformProvider;

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

    private static readonly Lazy<bool> isSteamDeck = new(() =>
    {
        var steamDeckVariable = Environment.GetEnvironmentVariable("SteamDeck");

        return (steamDeckVariable?.Length ?? 0) > 0 && int.TryParse(steamDeckVariable, out var steamDeckValue) && steamDeckValue > 0;
    });

    /// <summary>
    /// Whether we're running as a game
    /// </summary>
    public static bool IsPlaying { get; internal set; } = true;

    /// <summary>
    /// Whether we're running as an editor
    /// </summary>
    public static bool IsEditor { get; internal set; } = false;

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

    /// <summary>
    /// Whether we're running on steam deck
    /// </summary>
    public static bool IsSteamDeck => isSteamDeck.Value;

    /// <summary>
    /// Whether we're running on a mobile platform
    /// </summary>
    public static bool IsMobilePlatform => IsAndroid || IsiOS;

    /// <summary>
    /// Whether we're running on a desktop platform
    /// </summary>
    public static bool IsDesktopPlatform => IsWindows || IsLinux || IsMacOS || IsSteamDeck;

    public static string ClipboardText
    {
        get
        {
            #if !ANDROID && !IOS
            return SDL3.SDL.GetClipboardText();
            #else
            return "";
            #endif
        }
    }

    public static void SetClipboardText(string text)
    {
#if !ANDROID && !IOS
        SDL3.SDL.SetClipboardText(text);
#endif
    }
}
