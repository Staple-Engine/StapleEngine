namespace Staple.Internal
{
    internal interface IRenderWindow
    {
        bool ContextLost { get; set; }

        bool IsFocused { get; }

        bool ShouldClose { get; }

        bool Unavailable { get; }

        bool Create(ref int width, ref int height, string title, bool resizable, WindowMode windowMode, int monitorIndex);

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
    }
}
