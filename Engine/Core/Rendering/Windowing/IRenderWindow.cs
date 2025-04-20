namespace Staple.Internal;

internal interface IRenderWindow
{
    bool ContextLost { get; set; }

    bool IsFocused { get; }

    bool ShouldClose { get; }

    bool Unavailable { get; }

    int MonitorIndex { get; }

    bool Maximized { get; }

    int RefreshRate { get; }

    Vector2Int Position { get; set; }

    string Title { get; set; }

    bool Create(ref int width, ref int height, string title, bool resizable, WindowMode windowMode, Vector2Int? position,
        bool maximized, int monitorIndex);

    void PollEvents();

    void GetWindowSize(out int width, out int height);

    void Destroy();

    void Init();

    void Terminate();

    nint MonitorPointer(AppPlatform platform);

    nint WindowPointer(AppPlatform platform);

    void LockCursor();

    void UnlockCursor();

    void ShowCursor();

    void HideCursor();

    bool TryCreateCursorImage(Color32[] pixels, int width, int height, int hotX, int hotY, out CursorImage image);

    void SetCursor(CursorImage image);

    void SetIcon(RawTextureData icon);

    bool SetResolution(int width, int height, WindowMode windowMode);
}
