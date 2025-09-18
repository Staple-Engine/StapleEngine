namespace Staple.Internal;

/// <summary>
/// Abstraction for window implementations
/// </summary>
internal interface IRenderWindow
{
    /// <summary>
    /// Whether the window context has been lost and the engine needs to recreate resources
    /// </summary>
    bool ContextLost { get; set; }

    /// <summary>
    /// Whether the window is currently focused
    /// </summary>
    bool IsFocused { get; }

    /// <summary>
    /// Whether the window wants to close
    /// </summary>
    bool ShouldClose { get; }

    /// <summary>
    /// Whether the window is unavailable (e.g., in the background on mobile platforms)
    /// </summary>
    bool Unavailable { get; }

    /// <summary>
    /// The index of the monitor the window is at
    /// </summary>
    int MonitorIndex { get; }

    /// <summary>
    /// Whether the window is maximized
    /// </summary>
    bool Maximized { get; }

    /// <summary>
    /// The screen refresh rate
    /// </summary>
    int RefreshRate { get; }

    /// <summary>
    /// The window's position
    /// </summary>
    Vector2Int Position { get; set; }

    /// <summary>
    /// The window's size
    /// </summary>
    Vector2Int Size { get; }

    /// <summary>
    /// The window title
    /// </summary>
    string Title { get; set; }

    /// <summary>
    /// Attempt to create a window
    /// </summary>
    /// <param name="width">The window width</param>
    /// <param name="height">The window height</param>
    /// <param name="title">The window title</param>
    /// <param name="resizable">Whether the window should be resizable</param>
    /// <param name="windowMode">The mode for the window</param>
    /// <param name="position">The position of the window</param>
    /// <param name="maximized">Whether the window should be maximized</param>
    /// <param name="monitorIndex">The index of the monitor to put the window in</param>
    /// <returns></returns>
    bool Create(ref int width, ref int height, string title, bool resizable, WindowMode windowMode, Vector2Int? position,
        bool maximized, int monitorIndex);

    /// <summary>
    /// Process window events
    /// </summary>
    void PollEvents();

    /// <summary>
    /// Initializes the window
    /// </summary>
    void Init();

    /// <summary>
    /// Terminates the window
    /// </summary>
    void Terminate();

    /// <summary>
    /// Gets the native platform data of the window
    /// </summary>
    /// <param name="platform">The current platform</param>
    /// <param name="nativeWindowType">The native window type for this window</param>
    /// <param name="windowPointer">The pointer to the window itself</param>
    /// <param name="monitorPointer">The pointer to the monitor data, if any</param>
    void GetNativePlatformData(AppPlatform platform, out NativeWindowType nativeWindowType, out nint windowPointer, out nint monitorPointer);

    /// <summary>
    /// Locks the cursor to the window
    /// </summary>
    void LockCursor();

    /// <summary>
    /// Unlocks the cursor from the window
    /// </summary>
    void UnlockCursor();

    /// <summary>
    /// Makes the cursor visible
    /// </summary>
    void ShowCursor();

    /// <summary>
    /// Makes the cursor hide
    /// </summary>
    void HideCursor();

    /// <summary>
    /// Tries to create a cursor image
    /// </summary>
    /// <param name="pixels">The pixels of the cursor</param>
    /// <param name="width">The width of the image</param>
    /// <param name="height">The height of the image</param>
    /// <param name="hotX">The hotspot (X)</param>
    /// <param name="hotY">The hotspot (Y)</param>
    /// <param name="image">The cursor image, if successful</param>
    /// <returns>Whether created successfully</returns>
    bool TryCreateCursorImage(Color32[] pixels, int width, int height, int hotX, int hotY, out CursorImage image);

    /// <summary>
    /// Sets the current cursor to a specific image
    /// </summary>
    /// <param name="image">The cursor image</param>
    void SetCursor(CursorImage image);

    /// <summary>
    /// Sets the window's icon
    /// </summary>
    /// <param name="icon">Raw image data for the icon</param>
    void SetIcon(RawTextureData icon);

    /// <summary>
    /// Sets the current resolution for the window
    /// </summary>
    /// <param name="width">The width</param>
    /// <param name="height">The height</param>
    /// <param name="windowMode">The window mode</param>
    /// <returns>Whether it was successfully set</returns>
    bool SetResolution(int width, int height, WindowMode windowMode);

    /// <summary>
    /// Shows text input UI. Call when you need the user to write something.
    /// </summary>
    /// <remarks>May not show anything depending on platform.</remarks>
    void ShowTextInput();

    /// <summary>
    /// Hides text input UI.
    /// </summary>
    void HideTextInput();
}
