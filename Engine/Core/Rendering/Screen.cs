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
    /// Get or set the current window mode for the game/app.
    /// Setting will immediately change the window mode.
    /// Applies only to PC platforms (windows, linux, mac) for built games/apps (editor will ignore this property)
    /// </summary>
    public static WindowMode WindowMode
    {
        get => AppPlayer.instance == null ? WindowMode.Windowed : AppPlayer.instance.playerSettings.windowMode;

        set
        {
            if (AppPlayer.instance == null ||
                AppPlayer.instance.playerSettings.windowMode == value ||
                AppPlayer.instance.renderWindow.SetWindowMode(value) == false)
            {
                return;
            }

            AppPlayer.instance.playerSettings.windowMode = value;

            PlayerSettings.Save(AppPlayer.instance.playerSettings);
        }
    }
}
