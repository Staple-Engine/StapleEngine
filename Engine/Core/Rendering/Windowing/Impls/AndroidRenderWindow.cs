﻿#if ANDROID
using System;
using System.Runtime.CompilerServices;

namespace Staple.Internal;

internal class AndroidRenderWindow : IRenderWindow
{
    internal bool isInBackground = false;
    internal bool shouldClose = false;
    internal int screenWidth;
    internal int screenHeight;
    internal nint window;
    internal bool contextLost = false;
    internal bool unavailable = false;
    internal int refreshRate = 60;
    internal object lockObject = new();

    public static AndroidRenderWindow Instance = new();

    public int MonitorIndex => 0;

    public bool Maximized => false;

    public Vector2Int Position { get; set; }

    public string Title { get; set; }

    public int RefreshRate => refreshRate;

    public void Mutate(Action<AndroidRenderWindow> callback)
    {
        lock(lockObject)
        {
            callback(this);
        }
    }

    public bool Unavailable
    {
        get
        {
            lock(lockObject)
            {
                return unavailable;
            }
        }
    }

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

    public Vector2Int Size
    {
        get
        {
            return new(screenWidth, screenHeight);
        }
    }

    public bool Create(ref int width, ref int height, string title, bool resizable, WindowMode windowMode, Vector2Int? position, bool maximized, int monitorIndex)
    {
        return true;
    }

    public void EnterBackground()
    {
        lock(lockObject)
        {
            isInBackground = true;
        }

        AudioSystem.Instance.EnterBackground();
    }

    public void EnterForeground()
    {
        lock(lockObject)
        {
            isInBackground = false;
        }

        AudioSystem.Instance.EnterForeground();
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

    public void GetNativePlatformData(AppPlatform platform, out NativeWindowType type, out nint windowPointer, out nint monitorPointer)
    {
        type = NativeWindowType.Other;

        monitorPointer = nint.Zero;

        lock(lockObject)
        {
            windowPointer = window;
        }
    }

    public void SetIcon(RawTextureData icon)
    {
    }

    public bool SetResolution(int width, int height, WindowMode windowMode)
    {
        return false;
    }

    public bool TryCreateCursorImage(Color32[] pixels, int width, int height, int hotX, int hotY, out CursorImage image)
    {
        image = default;

        return false;
    }

    public void SetCursor(CursorImage image)
    {
    }
}
#endif