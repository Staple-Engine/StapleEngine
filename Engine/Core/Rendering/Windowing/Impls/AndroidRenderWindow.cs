﻿#if ANDROID
using Android.App;
using Android.Views;
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Player")]

namespace Staple.Internal
{
    internal class AndroidRenderWindow : IRenderWindow
    {
        internal bool isInBackground = false;
        internal bool shouldClose = false;
        internal int screenWidth;
        internal int screenHeight;
        internal nint window;
        internal bool contextLost = false;
        internal object lockObject = new();

        public static AndroidRenderWindow Instance = new();

        public bool IsFocused
        {
            get
            {
                lock(lockObject)
                {
                    return isInBackground == false;
                }
            }
        }

        public bool ShouldClose
        {
            get
            {
                lock(lockObject)
                {
                    return shouldClose;
                }
            }
        }

        public bool ContextLost
        {
            get
            {
                lock(lockObject)
                {
                    return contextLost;
                }
            }

            set
            {
                lock(lockObject)
                {
                    contextLost = value;
                }
            }
        }

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