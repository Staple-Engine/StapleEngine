namespace Staple;

/// <summary>
/// Represents the screen for which we're rendering
/// </summary>
public static class Screen
{
    /// <summary>
    /// The screen width in pixels
    /// </summary>
    public static int Width { get; internal set; }

    /// <summary>
    /// The screen height in pixels
    /// </summary>
    public static int Height { get; internal set; }

    /// <summary>
    /// The current window mode for the game/app.
    /// </summary>
    public static WindowMode WindowMode => AppPlayer.instance == null ? WindowMode.Windowed : AppPlayer.instance.playerSettings.windowMode;

    /// <summary>
    /// Sets the current screen resolution and window mode
    /// </summary>
    /// <param name="width">The new screen width</param>
    /// <param name="height">The new screen height</param>
    /// <param name="mode">The new window Mode</param>
    public static void SetResolution(int width, int height, WindowMode mode)
    {
        if(AppPlayer.instance == null ||
            AppPlayer.instance.renderWindow == null ||
            AppPlayer.instance.renderWindow.SetResolution(width, height, mode) == false)
        {
            return;
        }

        AppPlayer.instance.playerSettings.screenWidth = width;
        AppPlayer.instance.playerSettings.screenHeight = height;
        AppPlayer.instance.playerSettings.windowMode = mode;

        PlayerSettings.Save(AppPlayer.instance.playerSettings);
    }
}
