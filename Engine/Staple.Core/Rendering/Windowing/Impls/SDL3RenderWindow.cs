#if !ANDROID && !IOS
using SDL3;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Internal;

internal class SDL3RenderWindow : IRenderWindow
{
    private const short AxisDeadzone = 8000;

    private class SDL3Cursor : CursorImage
    {
        public Color32[] pixels;
        public nint surface = nint.Zero;
        public nint cursor = nint.Zero;

        public override void Dispose()
        {
            if (cursor != nint.Zero)
            {
                SDL.DestroyCursor(cursor);

                cursor = nint.Zero;
            }

            if(surface != nint.Zero)
            {
                SDL.DestroySurface(surface);

                surface = nint.Zero;
            }

            pixels = [];
        }
    }

    private class GamepadState
    {
        public nint instance;
        public int playerIndex;
    }

    public nint window;

    private readonly Dictionary<uint, GamepadState> gamepads = [];

    private readonly List<SDL3Cursor> cursors = [];

    private bool movedWindow = false;
    private DateTime movedWindowTimer;
    private Vector2Int previousWindowPosition;
    private bool closedWindow = false;
    private bool windowFocused = true;
    private bool windowMaximized = false;
    private nint defaultCursor = nint.Zero;
    private int refreshRate = 60;
    private uint[] displays = [];

    private nint metalView = nint.Zero;

    public bool ContextLost { get; set; } = false;

    public bool IsFocused => windowFocused;

    public bool ShouldClose => closedWindow;

    public bool Unavailable => false;

    public bool Maximized => windowMaximized;

    public int RefreshRate => refreshRate;

    public string Title
    {
        get => SDL.GetWindowTitle(window);

        set => SDL.SetWindowTitle(window, value);
    }

    public Vector2Int Position
    {
        get
        {
            SDL.GetWindowPosition(window, out var x, out var y);

            return new(x, y);
        }

        set
        {
            SDL.SetWindowPosition(window, value.X, value.Y);
        }
    }

    public Vector2Int Size
    {
        get
        {
            SDL.GetWindowSize(window, out var w, out var h);

            return new(w, h);
        }
    }

    public int MonitorIndex
    {
        get
        {
            if(window == nint.Zero)
            {
                return 0;
            }

            return Array.IndexOf(displays, SDL.GetDisplayForWindow(window));
        }
    }

    private static int CenteredDisplay(uint monitor)
    {
        return (int)(0x2FFF0000u | monitor);
    }

    private static int UndefinedDisplay(uint monitor)
    {
        return (int)(0x1FFF0000u | monitor);
    }

    public bool Create(ref int width, ref int height, string title, bool resizable, WindowMode windowMode, Vector2Int? position,
        bool maximized, int monitorIndex)
    {
        displays = SDL.GetDisplays(out var displayCount);

        if(displays == null)
        {
            return false;
        }

        var monitor = monitorIndex >= 0 && monitorIndex < displayCount ? this.displays[monitorIndex] : 0;

        SDL.GetDisplayBounds(monitor, out var displayBounds);

        var windowFlags = SDL.WindowFlags.HighPixelDensity;

        if(resizable && windowMode == WindowMode.Windowed)
        {
            windowFlags |= SDL.WindowFlags.Resizable;
        }

        if(maximized)
        {
            windowFlags |= SDL.WindowFlags.Maximized;
        }

        var windowPosition = new Vector2Int();

        switch(windowMode)
        {
            case WindowMode.Windowed:

                if(position.HasValue)
                {
                    windowPosition = position.Value;
                }
                else
                {
                    windowPosition = new Vector2Int(CenteredDisplay(monitor), CenteredDisplay(monitor));
                }

                break;

            case WindowMode.ExclusiveFullscreen:

                windowFlags |= SDL.WindowFlags.Fullscreen;

                windowPosition = new Vector2Int(UndefinedDisplay(monitor), UndefinedDisplay(monitor));

                break;

            case WindowMode.BorderlessFullscreen:

                windowFlags |= SDL.WindowFlags.Fullscreen;

                windowPosition = new Vector2Int(UndefinedDisplay(monitor), UndefinedDisplay(monitor));

                width = displayBounds.W;
                height = displayBounds.H;

                break;
        }

        var props = SDL.CreateProperties();

        SDL.SetStringProperty(props, SDL.Props.WindowCreateTitleString, title);
        SDL.SetNumberProperty(props, SDL.Props.WindowCreateXNumber, windowPosition.X);
        SDL.SetNumberProperty(props, SDL.Props.WindowCreateYNumber, windowPosition.Y);
        SDL.SetNumberProperty(props, SDL.Props.WindowCreateWidthNumber, width);
        SDL.SetNumberProperty(props, SDL.Props.WindowCreateHeightNumber, height);
        SDL.SetNumberProperty(props, SDL.Props.WindowCreateFlagsNumber, (long)windowFlags);

        window = SDL.CreateWindowWithProperties(props);

        SDL.DestroyProperties(props);

        if (window == nint.Zero)
        {
            return false;
        }

        if(windowMode == WindowMode.BorderlessFullscreen)
        {
            SDL.SetWindowFullscreen(window, true);
        }

        SDL.DisplayMode? mode = SDL.GetCurrentDisplayMode(monitor);

        if (mode != null)
        {
            refreshRate = (int)mode.Value.RefreshRate;
        }

        if (maximized)
        {
            windowMaximized = true;
        }

        defaultCursor = SDL.CreateSystemCursor(SDL.SystemCursor.Default);

        return true;
    }

    private static KeyCode MapSDLKey(SDL.Keycode sym)
    {
        return sym switch
        {
            SDL.Keycode.Alpha0 => KeyCode.Alpha0,
            SDL.Keycode.Alpha1 => KeyCode.Alpha1,
            SDL.Keycode.Alpha2 => KeyCode.Alpha2,
            SDL.Keycode.Alpha3 => KeyCode.Alpha3,
            SDL.Keycode.Alpha4 => KeyCode.Alpha4,
            SDL.Keycode.Alpha5 => KeyCode.Alpha5,
            SDL.Keycode.Alpha6 => KeyCode.Alpha6,
            SDL.Keycode.Alpha7 => KeyCode.Alpha7,
            SDL.Keycode.Alpha8 => KeyCode.Alpha8,
            SDL.Keycode.Alpha9 => KeyCode.Alpha9,
            SDL.Keycode.A => KeyCode.A,
            SDL.Keycode.B => KeyCode.B,
            SDL.Keycode.C => KeyCode.C,
            SDL.Keycode.D => KeyCode.D,
            SDL.Keycode.E => KeyCode.E,
            SDL.Keycode.F => KeyCode.F,
            SDL.Keycode.G => KeyCode.G,
            SDL.Keycode.H => KeyCode.H,
            SDL.Keycode.I => KeyCode.I,
            SDL.Keycode.J => KeyCode.J,
            SDL.Keycode.K => KeyCode.K,
            SDL.Keycode.L => KeyCode.L,
            SDL.Keycode.M => KeyCode.M,
            SDL.Keycode.N => KeyCode.N,
            SDL.Keycode.O => KeyCode.O,
            SDL.Keycode.P => KeyCode.P,
            SDL.Keycode.Q => KeyCode.Q,
            SDL.Keycode.R => KeyCode.R,
            SDL.Keycode.S => KeyCode.S,
            SDL.Keycode.T => KeyCode.T,
            SDL.Keycode.U => KeyCode.U,
            SDL.Keycode.V => KeyCode.V,
            SDL.Keycode.W => KeyCode.W,
            SDL.Keycode.X => KeyCode.X,
            SDL.Keycode.Y => KeyCode.Y,
            SDL.Keycode.Z => KeyCode.Z,
            SDL.Keycode.Backslash => KeyCode.Backslash,
            SDL.Keycode.Backspace => KeyCode.Backspace,
            SDL.Keycode.Capslock => KeyCode.CapsLock,
            SDL.Keycode.Comma => KeyCode.Comma,
            SDL.Keycode.Delete => KeyCode.Delete,
            SDL.Keycode.Down => KeyCode.Down,
            SDL.Keycode.Up => KeyCode.Up,
            SDL.Keycode.Left => KeyCode.Left,
            SDL.Keycode.Right => KeyCode.Right,
            SDL.Keycode.End => KeyCode.End,
            SDL.Keycode.Equals => KeyCode.Equal,
            SDL.Keycode.Escape => KeyCode.Escape,
            SDL.Keycode.F1 => KeyCode.F1,
            SDL.Keycode.F2 => KeyCode.F2,
            SDL.Keycode.F3 => KeyCode.F3,
            SDL.Keycode.F4 => KeyCode.F4,
            SDL.Keycode.F5 => KeyCode.F5,
            SDL.Keycode.F6 => KeyCode.F6,
            SDL.Keycode.F7 => KeyCode.F7,
            SDL.Keycode.F8 => KeyCode.F8,
            SDL.Keycode.F9 => KeyCode.F9,
            SDL.Keycode.F10 => KeyCode.F10,
            SDL.Keycode.F11 => KeyCode.F11,
            SDL.Keycode.F12 => KeyCode.F12,
            SDL.Keycode.F13 => KeyCode.F13,
            SDL.Keycode.F14 => KeyCode.F14,
            SDL.Keycode.F15 => KeyCode.F15,
            SDL.Keycode.F16 => KeyCode.F16,
            SDL.Keycode.F17 => KeyCode.F17,
            SDL.Keycode.F18 => KeyCode.F18,
            SDL.Keycode.F19 => KeyCode.F19,
            SDL.Keycode.F20 => KeyCode.F20,
            SDL.Keycode.F21 => KeyCode.F21,
            SDL.Keycode.F22 => KeyCode.F22,
            SDL.Keycode.F23 => KeyCode.F23,
            SDL.Keycode.F24 => KeyCode.F24,
            SDL.Keycode.Home => KeyCode.Home,
            SDL.Keycode.Insert => KeyCode.Insert,
            SDL.Keycode.Kp0 => KeyCode.Numpad0,
            SDL.Keycode.Kp1 => KeyCode.Numpad1,
            SDL.Keycode.Kp2 => KeyCode.Numpad2,
            SDL.Keycode.Kp3 => KeyCode.Numpad3,
            SDL.Keycode.Kp4 => KeyCode.Numpad4,
            SDL.Keycode.Kp5 => KeyCode.Numpad5,
            SDL.Keycode.Kp6 => KeyCode.Numpad6,
            SDL.Keycode.Kp7 => KeyCode.Numpad7,
            SDL.Keycode.Kp8 => KeyCode.Numpad8,
            SDL.Keycode.Kp9 => KeyCode.Numpad9,
            SDL.Keycode.LAlt => KeyCode.LeftAlt,
            SDL.Keycode.LCtrl => KeyCode.LeftControl,
            SDL.Keycode.LeftBracket => KeyCode.LeftBracket,
            SDL.Keycode.LShift => KeyCode.LeftShift,
            SDL.Keycode.RAlt => KeyCode.RightAlt,
            SDL.Keycode.RCtrl => KeyCode.RightControl,
            SDL.Keycode.RightBracket => KeyCode.RightBracket,
            SDL.Keycode.RShift => KeyCode.RightShift,
            SDL.Keycode.Minus => KeyCode.Minus,
            SDL.Keycode.Pagedown => KeyCode.PageDown,
            SDL.Keycode.Pageup => KeyCode.PageUp,
            SDL.Keycode.Period => KeyCode.Period,
            SDL.Keycode.PrintScreen => KeyCode.PrintScreen,
            SDL.Keycode.Return => KeyCode.Enter,
            SDL.Keycode.Return2 => KeyCode.Enter,
            SDL.Keycode.Semicolon => KeyCode.SemiColon,
            SDL.Keycode.Slash => KeyCode.Slash,
            SDL.Keycode.Space => KeyCode.Space,
            SDL.Keycode.Tab => KeyCode.Tab,
            _ => KeyCode.Unknown,
        };
    }

    private static AppEventModifierKeys GetModifiers(SDL.Keymod mod)
    {
        AppEventModifierKeys modifiers = 0;

        if (mod.HasFlag(SDL.Keymod.Caps))
        {
            modifiers |= AppEventModifierKeys.CapsLock;
        }

        if(mod.HasFlag(SDL.Keymod.Alt))
        {
            modifiers |= AppEventModifierKeys.Alt;
        }

        if (mod.HasFlag(SDL.Keymod.Ctrl))
        {
            modifiers |= AppEventModifierKeys.Control;
        }

        if (mod.HasFlag(SDL.Keymod.Shift))
        {
            modifiers |= AppEventModifierKeys.Shift;
        }

        if (mod.HasFlag(SDL.Keymod.Num))
        {
            modifiers |= AppEventModifierKeys.NumLock;
        }

        return modifiers;
    }

    public void PollEvents()
    {
        while(SDL.PollEvent(out var _event))
        {
            switch((SDL.EventType)_event.Type)
            {
                case SDL.EventType.WindowFocusGained:

                    windowFocused = true;

                    break;

                case SDL.EventType.WindowFocusLost:

                    windowFocused = false;

                    break;

                case SDL.EventType.WindowMaximized:

                    windowMaximized = true;

                    AppEventQueue.instance.Add(AppEvent.Maximize(windowMaximized));

                    break;

                case SDL.EventType.WindowRestored:

                    windowMaximized = false;

                    AppEventQueue.instance.Add(AppEvent.Maximize(windowMaximized));

                    break;

                case SDL.EventType.WindowMoved:

                    SDL.GetWindowPosition(window, out var winX, out var winY);

                    AppEventQueue.instance.Add(AppEvent.MoveWindow(new Vector2Int(winX, winY)));

                    break;
                
                case SDL.EventType.WindowResized:
                    
                    AppEventQueue.instance.Add(AppEvent.ResizeWindow());

                    break;

                case SDL.EventType.KeyDown:
                case SDL.EventType.KeyUp:

                    AppEventQueue.instance.Add(AppEvent.Key(MapSDLKey((SDL.Keycode)_event.Key.Key), (int)_event.Key.Scancode,
                        _event.Key.Down ? AppEventInputState.Press : AppEventInputState.Release,
                        GetModifiers(_event.Key.Mod)));

                    break;

                case SDL.EventType.MouseButtonDown:
                case SDL.EventType.MouseButtonUp:

                    AppEventQueue.instance.Add(AppEvent.Mouse(_event.Button.Button switch
                        {
                            1 => AppEventMouseButton.Left,
                            2 => AppEventMouseButton.Middle,
                            3 => AppEventMouseButton.Right,
                            4 => AppEventMouseButton.Button1,
                            5 => AppEventMouseButton.Button2,
                            _ => 0,
                        },
                        _event.Button.Down ? AppEventInputState.Press : AppEventInputState.Release,
                        GetModifiers(SDL.GetModState())));

                    break;

                case SDL.EventType.MouseMotion:

                    if(SDL.GetWindowRelativeMouseMode(window))
                    {
                        Input.CursorPosCallback(_event.Motion.XRel, _event.Motion.YRel);
                    }
                    else
                    {
                        Input.CursorPosCallback(_event.Motion.X, _event.Motion.Y);
                    }

                    break;

                case SDL.EventType.MouseWheel:

                    Input.MouseScrollCallback(_event.Wheel.X, _event.Wheel.Y);

                    break;

                case SDL.EventType.GamepadAdded:

                    {
                        var instance = SDL.OpenGamepad(_event.CDevice.Which);

                        var playerIndex = SDL.GetGamepadPlayerIndex(instance);

                        gamepads.Add(_event.CDevice.Which, new()
                        {
                            instance = instance,
                            playerIndex = playerIndex,
                        });

                        Input.GamepadConnect(AppEvent.GamepadConnect(playerIndex, GamepadConnectionState.Connected));
                    }

                    break;

                case SDL.EventType.GamepadRemoved:

                    {
                        if(gamepads.TryGetValue(_event.CDevice.Which, out var state))
                        {
                            SDL.CloseGamepad(state.instance);

                            gamepads.Remove(_event.CDevice.Which);

                            Input.GamepadConnect(AppEvent.GamepadConnect(state.playerIndex, GamepadConnectionState.Disconnected));
                        }
                    }

                    break;

                case SDL.EventType.GamepadButtonDown:
                case SDL.EventType.GamepadButtonUp:

                    {
                        if (gamepads.TryGetValue(_event.CDevice.Which, out var state))
                        {
                            Input.GamepadButton(AppEvent.GamepadButton(state.playerIndex,
                                (SDL.GamepadButton)_event.GButton.Button switch
                                {
                                    SDL.GamepadButton.South => GamepadButton.A,
                                    SDL.GamepadButton.East => GamepadButton.B,
                                    SDL.GamepadButton.West => GamepadButton.X,
                                    SDL.GamepadButton.North => GamepadButton.Y,
                                    SDL.GamepadButton.Back => GamepadButton.Back,
                                    SDL.GamepadButton.Guide => GamepadButton.Guide,
                                    SDL.GamepadButton.Start => GamepadButton.Start,
                                    SDL.GamepadButton.LeftStick => GamepadButton.LeftStick,
                                    SDL.GamepadButton.RightStick => GamepadButton.RightStick,
                                    SDL.GamepadButton.LeftShoulder => GamepadButton.LeftShoulder,
                                    SDL.GamepadButton.RightShoulder => GamepadButton.RightShoulder,
                                    SDL.GamepadButton.DPadUp => GamepadButton.DPadUp,
                                    SDL.GamepadButton.DPadDown => GamepadButton.DPadDown,
                                    SDL.GamepadButton.DPadLeft => GamepadButton.DPadLeft,
                                    SDL.GamepadButton.DPadRight => GamepadButton.DPadRight,
                                    SDL.GamepadButton.Misc1 => GamepadButton.Misc1,
                                    SDL.GamepadButton.RightPaddle1 => GamepadButton.Paddle1,
                                    SDL.GamepadButton.LeftPaddle1 => GamepadButton.Paddle2,
                                    SDL.GamepadButton.RightPaddle2 => GamepadButton.Paddle3,
                                    SDL.GamepadButton.LeftPaddle2 => GamepadButton.Paddle4,
                                    SDL.GamepadButton.Touchpad => GamepadButton.TouchPad,
                                    _ => GamepadButton.Invalid,
                                },
                                _event.GButton.Down ? AppEventInputState.Press : AppEventInputState.Release));
                        }
                    }

                    break;

                case SDL.EventType.GamepadAxisMotion:

                    {
                        if(gamepads.TryGetValue(_event.CDevice.Which, out var state))
                        {
                            var value = _event.GAxis.Value;

                            if (Math.Abs(value) <= AxisDeadzone)
                            {
                                value = 0;
                            }

                            var floatValue = value / (float)short.MaxValue;

                            var axis = (SDL.GamepadAxis)_event.GAxis.Axis switch
                            {
                                SDL.GamepadAxis.LeftX => GamepadAxis.LeftX,
                                SDL.GamepadAxis.LeftY => GamepadAxis.LeftY,
                                SDL.GamepadAxis.RightX => GamepadAxis.RightX,
                                SDL.GamepadAxis.RightY => GamepadAxis.RightY,
                                SDL.GamepadAxis.LeftTrigger => GamepadAxis.TriggerLeft,
                                SDL.GamepadAxis.RightTrigger => GamepadAxis.TriggerRight,
                                _ => GamepadAxis.Invalid,
                            };

                            if(axis == GamepadAxis.LeftY || axis == GamepadAxis.RightY)
                            {
                                floatValue *= -1;
                            }

                            Input.GamepadMovement(AppEvent.GamepadMovement(state.playerIndex, axis, floatValue));
                        }
                    }

                    break;

                case SDL.EventType.Quit:

                    closedWindow = true;

                    break;

                case SDL.EventType.TextInput:

                    unsafe
                    {
                        byte* ptr = (byte*)_event.Text.Text;

                        var len = 0;

                        while (ptr[len] != '\0')
                        {
                            len++;
                        }

                        var text = Encoding.UTF8.GetString(ptr, len);

                        Input.HandleTextEvent(AppEvent.Text(text.Length > 0 ? (uint)text[0] : 0));
                    }

                    break;
            }
        }

        var windowPosition = new Vector2Int();

        SDL.GetWindowPosition(window, out windowPosition.X, out windowPosition.Y);

        if (previousWindowPosition != windowPosition)
        {
            previousWindowPosition = windowPosition;

            movedWindow = true;
            movedWindowTimer = DateTime.UtcNow;
        }

        if (movedWindow && (DateTime.UtcNow - movedWindowTimer).TotalSeconds >= 1.0f)
        {
            movedWindow = false;

            AppEventQueue.instance.Add(AppEvent.MoveWindow(windowPosition));
        }
    }

    public void Init()
    {
        SDL.Init(SDL.InitFlags.Video | SDL.InitFlags.Haptic | SDL.InitFlags.Gamepad);
    }

    public void Terminate()
    {
        foreach(var pair in gamepads)
        {
            if(pair.Value.instance != nint.Zero)
            {
                SDL.CloseGamepad(pair.Value.instance);

                pair.Value.instance = nint.Zero;
            }
        }

        foreach (var cursor in cursors)
        {
            cursor.Dispose();
        }

        if (window != nint.Zero)
        {
            SDL.DestroyWindow(window);
        }

        SDL.Quit();
    }

    public void GetNativePlatformData(AppPlatform platform, out NativeWindowType type, out nint windowPointer, out nint monitorPointer)
    {
        windowPointer = nint.Zero;
        monitorPointer = nint.Zero;
        type = NativeWindowType.Other;

        switch (platform)
        {
            case AppPlatform.Windows:

                windowPointer = SDL.GetPointerProperty(SDL.GetWindowProperties(window), SDL.Props.WindowWin32HWNDPointer, nint.Zero);

                break;

            case AppPlatform.Linux:

                switch(SDL.GetCurrentVideoDriver())
                {
                    case "x11":

                        type = NativeWindowType.X11;

                        windowPointer = (nint)SDL.GetNumberProperty(SDL.GetWindowProperties(window),
                            SDL.Props.WindowX11WindowNumber, 0);
                        monitorPointer = SDL.GetPointerProperty(SDL.GetWindowProperties(window),
                            SDL.Props.WindowX11DisplayPointer, nint.Zero);

                        break;

                    case "wayland":

                        type = NativeWindowType.Wayland;

                        windowPointer = SDL.GetPointerProperty(SDL.GetWindowProperties(window),
                            SDL.Props.WindowWaylandSurfacePointer, nint.Zero);
                        monitorPointer = SDL.GetPointerProperty(SDL.GetWindowProperties(window),
                            SDL.Props.WindowWaylandDisplayPointer, nint.Zero);

                        break;

                    default:

                        break;
                }

                break;

            case AppPlatform.MacOSX:

                if(metalView == nint.Zero)
                {
                    metalView = SDL.MetalCreateView(window);
                }

                windowPointer = SDL.MetalGetLayer(metalView);

                break;
        }
    }

    public void LockCursor()
    {
        SDL.SetWindowRelativeMouseMode(window, true);

        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        SDL.SetWindowRelativeMouseMode(window, false);

        Cursor.visible = true;
    }

    public void HideCursor()
    {
        SDL.HideCursor();
    }

    public void ShowCursor()
    {
        SDL.ShowCursor();
    }

    public void SetIcon(RawTextureData icon)
    {
        if (Platform.IsMacOS)
        {
            return;
        }

        var pinnedArray = GCHandle.Alloc(icon.data, GCHandleType.Pinned);

        unsafe
        {
            var ptr = pinnedArray.AddrOfPinnedObject();

            var surface = SDL.CreateSurfaceFrom(icon.width, icon.height,
                SDL.GetPixelFormatForMasks(32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000),
                ptr, icon.width * 4);

            SDL.SetWindowIcon(window, (nint)surface);

            SDL.DestroySurface((nint)surface);
        }

        pinnedArray.Free();
    }

    public bool SetResolution(int width, int height, WindowMode windowMode)
    {
        switch (windowMode)
        {
            case WindowMode.Windowed:

                if(!SDL.SetWindowFullscreen(window, false))
                {
                    return false;
                }

                SDL.SetWindowSize(window, width, height);

                break;

            case WindowMode.ExclusiveFullscreen:

                SDL.SetWindowSize(window, width, height);

                if(!SDL.SetWindowFullscreen(window, true))
                {
                    return false;
                }

                break;

            case WindowMode.BorderlessFullscreen:

                if (!SDL.SetWindowFullscreen(window, true))
                {
                    return false;
                }

                break;
        }

        return true;
    }

    public bool TryCreateCursorImage(Color32[] pixels, int width, int height, int hotX, int hotY, out CursorImage image)
    {
        unsafe
        {
            var outValue = new SDL3Cursor
            {
                pixels = pixels,
            };

            fixed (void *ptr = outValue.pixels)
            {
                var surface = SDL.CreateSurfaceFrom(width, height, SDL.PixelFormat.ARGB8888, (nint)ptr, width * 4);

                if(surface == nint.Zero)
                {
                    image = default;

                    return false;
                }

                var cursor = SDL.CreateColorCursor(surface, hotX, hotY);

                if(cursor == nint.Zero)
                {
                    SDL.DestroySurface(surface);

                    image = default;

                    return false;
                }

                outValue.surface = surface;
                outValue.cursor = cursor;

                image = outValue;

                return true;
            }
        }
    }

    public void SetCursor(CursorImage image)
    {
        if(image is not SDL3Cursor cursor ||
            cursor.cursor == nint.Zero)
        {
            SDL.SetCursor(defaultCursor);

            return;
        }

        SDL.SetCursor(cursor.cursor);
    }

    public void ShowTextInput()
    {
        if (window == nint.Zero)
        {
            return;
        }

        SDL.StartTextInput(window);
    }

    public void HideTextInput()
    {
        if (window == nint.Zero)
        {
            return;
        }

        SDL.StopTextInput(window);
    }
}
#endif