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
    /// The screen size in pixels as a <see cref="Vector2Int"/>>
    /// </summary>
    public static Vector2Int Size => new(Width, Height);

    /// <summary>
    /// The current render target width in pixels. If there's no active render target, defaults to <see cref="Width"/>
    /// </summary>
    public static int RenderTargetWidth => RenderTarget.Current?.width ?? Width;

    /// <summary>
    /// The current render target height in pixels. If there's no active render target, defaults to <see cref="Height"/>
    /// </summary>
    public static int RenderTargetHeight => RenderTarget.Current?.height ?? Height;

    /// <summary>
    /// The current render target size in pixels. If there's no active render target, defaults to <see cref="Size"/>
    /// </summary>
    public static Vector2Int RenderTargetSize => new(RenderTargetWidth, RenderTargetHeight);

    /// <summary>
    /// The current window mode for the game/app.
    /// </summary>
    public static WindowMode WindowMode => AppPlayer.instance?.playerSettings?.windowMode ?? WindowMode.Windowed;

    /// <summary>
    /// The refresh rate of the screen
    /// </summary>
    public static int RefreshRate { get; internal set; }

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
            !AppPlayer.instance.renderWindow.SetResolution(width, height, mode))
        {
            return;
        }

        AppPlayer.instance.playerSettings.screenWidth = width;
        AppPlayer.instance.playerSettings.screenHeight = height;
        AppPlayer.instance.playerSettings.windowMode = mode;

        PlayerSettings.Save(AppPlayer.instance.playerSettings);
    }
}
