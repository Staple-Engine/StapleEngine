#if ANDROID
using Android.App;
using Android.Views;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Player")]

namespace Staple.Internal
{
    internal class AndroidRenderWindow : IRenderWindow
    {
        public bool isInBackground = false;
        public bool shouldClose = false;
        public int screenWidth;
        public int screenHeight;
        public nint window;

        public static AndroidRenderWindow Instance = new();

        public bool IsFocused => isInBackground == false;

        public bool ShouldClose => shouldClose;

        public bool Create(ref int width, ref int height, string title, bool resizable, WindowMode windowMode, int monitorIndex)
        {
            return true;
        }

        public void Destroy()
        {
        }

        public void EnterBackground()
        {
            isInBackground = true;
        }

        public void EnterForeground()
        {
            isInBackground = false;
        }

        public void GetWindowSize(out int width, out int height)
        {
            width = screenWidth;
            height = screenHeight;
        }

        public void HideCursor()
        {
        }

        public void Init()
        {
        }

        public void LockCursor()
        {
        }

        public nint MonitorPointer(AppPlatform platform)
        {
            return nint.Zero;
        }

        public void PollEvents()
        {
        }

        public void ShowCursor()
        {
        }

        public void Terminate()
        {
        }

        public void UnlockCursor()
        {
        }

        public nint WindowPointer(AppPlatform platform)
        {
            return window;
        }
    }
}
#endif