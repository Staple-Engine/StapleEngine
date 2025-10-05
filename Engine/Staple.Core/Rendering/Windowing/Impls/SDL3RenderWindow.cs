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
                SDL.SDL_DestroyCursor(cursor);

                cursor = nint.Zero;
            }

            if(surface != nint.Zero)
            {
                SDL.SDL_DestroySurface(surface);

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
        get => SDL.SDL_GetWindowTitle(window);

        set => SDL.SDL_SetWindowTitle(window, value);
    }

    public Vector2Int Position
    {
        get
        {
            SDL.SDL_GetWindowPosition(window, out var x, out var y);

            return new(x, y);
        }

        set
        {
            SDL.SDL_SetWindowPosition(window, value.X, value.Y);
        }
    }

    public Vector2Int Size
    {
        get
        {
            SDL.SDL_GetWindowSize(window, out var w, out var h);

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

            return Array.IndexOf(displays, SDL.SDL_GetDisplayForWindow(window));
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
        var displays = SDL.SDL_GetDisplays(out var displayCount);

        if(displays == nint.Zero)
        {
            return false;
        }

        unsafe
        {
            var displaySpan = new Span<uint>((void *)displays, displayCount);

            this.displays = displaySpan.ToArray();
        }

        var monitor = monitorIndex >= 0 && monitorIndex < displayCount ? this.displays[monitorIndex] : 0;

        SDL.SDL_GetDisplayBounds(monitor, out var displayBounds);

        var windowFlags = SDL.SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY;

        if(resizable && windowMode == WindowMode.Windowed)
        {
            windowFlags |= SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
        }

        if(maximized)
        {
            windowFlags |= SDL.SDL_WindowFlags.SDL_WINDOW_MAXIMIZED;
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

                windowFlags |= SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;

                windowPosition = new Vector2Int(UndefinedDisplay(monitor), UndefinedDisplay(monitor));

                break;

            case WindowMode.BorderlessFullscreen:

                windowFlags |= SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;

                windowPosition = new Vector2Int(UndefinedDisplay(monitor), UndefinedDisplay(monitor));

                width = displayBounds.w;
                height = displayBounds.h;

                break;
        }

        var props = SDL.SDL_CreateProperties();

        SDL.SDL_SetStringProperty(props, SDL.SDL_PROP_WINDOW_CREATE_TITLE_STRING, title);
        SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_WINDOW_CREATE_X_NUMBER, windowPosition.X);
        SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_WINDOW_CREATE_Y_NUMBER, windowPosition.Y);
        SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_WINDOW_CREATE_WIDTH_NUMBER, width);
        SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_WINDOW_CREATE_HEIGHT_NUMBER, height);
        SDL.SDL_SetNumberProperty(props, SDL.SDL_PROP_WINDOW_CREATE_FLAGS_NUMBER, (long)windowFlags);

        window = SDL.SDL_CreateWindowWithProperties(props);

        SDL.SDL_DestroyProperties(props);

        if (window == nint.Zero)
        {
            return false;
        }

        if(windowMode == WindowMode.BorderlessFullscreen)
        {
            SDL.SDL_SetWindowFullscreen(window, true);
        }

        unsafe
        {
            SDL.SDL_DisplayMode* mode = SDL.SDL_GetCurrentDisplayMode(monitor);

            if (mode != null)
            {
                refreshRate = (int)mode->refresh_rate;
            }
        }

        if (maximized)
        {
            windowMaximized = true;
        }

        defaultCursor = SDL.SDL_CreateSystemCursor(SDL.SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT);

        return true;
    }

    private static KeyCode MapSDLKey(SDL.SDL_Keycode sym)
    {
        return sym switch
        {
            SDL.SDL_Keycode.SDLK_0 => KeyCode.Alpha0,
            SDL.SDL_Keycode.SDLK_1 => KeyCode.Alpha1,
            SDL.SDL_Keycode.SDLK_2 => KeyCode.Alpha2,
            SDL.SDL_Keycode.SDLK_3 => KeyCode.Alpha3,
            SDL.SDL_Keycode.SDLK_4 => KeyCode.Alpha4,
            SDL.SDL_Keycode.SDLK_5 => KeyCode.Alpha5,
            SDL.SDL_Keycode.SDLK_6 => KeyCode.Alpha6,
            SDL.SDL_Keycode.SDLK_7 => KeyCode.Alpha7,
            SDL.SDL_Keycode.SDLK_8 => KeyCode.Alpha8,
            SDL.SDL_Keycode.SDLK_9 => KeyCode.Alpha9,
            SDL.SDL_Keycode.SDLK_A => KeyCode.A,
            SDL.SDL_Keycode.SDLK_B => KeyCode.B,
            SDL.SDL_Keycode.SDLK_C => KeyCode.C,
            SDL.SDL_Keycode.SDLK_D => KeyCode.D,
            SDL.SDL_Keycode.SDLK_E => KeyCode.E,
            SDL.SDL_Keycode.SDLK_F => KeyCode.F,
            SDL.SDL_Keycode.SDLK_G => KeyCode.G,
            SDL.SDL_Keycode.SDLK_H => KeyCode.H,
            SDL.SDL_Keycode.SDLK_I => KeyCode.I,
            SDL.SDL_Keycode.SDLK_J => KeyCode.J,
            SDL.SDL_Keycode.SDLK_K => KeyCode.K,
            SDL.SDL_Keycode.SDLK_L => KeyCode.L,
            SDL.SDL_Keycode.SDLK_M => KeyCode.M,
            SDL.SDL_Keycode.SDLK_N => KeyCode.N,
            SDL.SDL_Keycode.SDLK_O => KeyCode.O,
            SDL.SDL_Keycode.SDLK_P => KeyCode.P,
            SDL.SDL_Keycode.SDLK_Q => KeyCode.Q,
            SDL.SDL_Keycode.SDLK_R => KeyCode.R,
            SDL.SDL_Keycode.SDLK_S => KeyCode.S,
            SDL.SDL_Keycode.SDLK_T => KeyCode.T,
            SDL.SDL_Keycode.SDLK_U => KeyCode.U,
            SDL.SDL_Keycode.SDLK_V => KeyCode.V,
            SDL.SDL_Keycode.SDLK_W => KeyCode.W,
            SDL.SDL_Keycode.SDLK_X => KeyCode.X,
            SDL.SDL_Keycode.SDLK_Y => KeyCode.Y,
            SDL.SDL_Keycode.SDLK_Z => KeyCode.Z,
            SDL.SDL_Keycode.SDLK_BACKSLASH => KeyCode.Backslash,
            SDL.SDL_Keycode.SDLK_BACKSPACE => KeyCode.Backspace,
            SDL.SDL_Keycode.SDLK_CAPSLOCK => KeyCode.CapsLock,
            SDL.SDL_Keycode.SDLK_COMMA => KeyCode.Comma,
            SDL.SDL_Keycode.SDLK_DELETE => KeyCode.Delete,
            SDL.SDL_Keycode.SDLK_DOWN => KeyCode.Down,
            SDL.SDL_Keycode.SDLK_UP => KeyCode.Up,
            SDL.SDL_Keycode.SDLK_LEFT => KeyCode.Left,
            SDL.SDL_Keycode.SDLK_RIGHT => KeyCode.Right,
            SDL.SDL_Keycode.SDLK_END => KeyCode.End,
            SDL.SDL_Keycode.SDLK_EQUALS => KeyCode.Equal,
            SDL.SDL_Keycode.SDLK_ESCAPE => KeyCode.Escape,
            SDL.SDL_Keycode.SDLK_F1 => KeyCode.F1,
            SDL.SDL_Keycode.SDLK_F2 => KeyCode.F2,
            SDL.SDL_Keycode.SDLK_F3 => KeyCode.F3,
            SDL.SDL_Keycode.SDLK_F4 => KeyCode.F4,
            SDL.SDL_Keycode.SDLK_F5 => KeyCode.F5,
            SDL.SDL_Keycode.SDLK_F6 => KeyCode.F6,
            SDL.SDL_Keycode.SDLK_F7 => KeyCode.F7,
            SDL.SDL_Keycode.SDLK_F8 => KeyCode.F8,
            SDL.SDL_Keycode.SDLK_F9 => KeyCode.F9,
            SDL.SDL_Keycode.SDLK_F10 => KeyCode.F10,
            SDL.SDL_Keycode.SDLK_F11 => KeyCode.F11,
            SDL.SDL_Keycode.SDLK_F12 => KeyCode.F12,
            SDL.SDL_Keycode.SDLK_F13 => KeyCode.F13,
            SDL.SDL_Keycode.SDLK_F14 => KeyCode.F14,
            SDL.SDL_Keycode.SDLK_F15 => KeyCode.F15,
            SDL.SDL_Keycode.SDLK_F16 => KeyCode.F16,
            SDL.SDL_Keycode.SDLK_F17 => KeyCode.F17,
            SDL.SDL_Keycode.SDLK_F18 => KeyCode.F18,
            SDL.SDL_Keycode.SDLK_F19 => KeyCode.F19,
            SDL.SDL_Keycode.SDLK_F20 => KeyCode.F20,
            SDL.SDL_Keycode.SDLK_F21 => KeyCode.F21,
            SDL.SDL_Keycode.SDLK_F22 => KeyCode.F22,
            SDL.SDL_Keycode.SDLK_F23 => KeyCode.F23,
            SDL.SDL_Keycode.SDLK_F24 => KeyCode.F24,
            SDL.SDL_Keycode.SDLK_HOME => KeyCode.Home,
            SDL.SDL_Keycode.SDLK_INSERT => KeyCode.Insert,
            SDL.SDL_Keycode.SDLK_KP_0 => KeyCode.Numpad0,
            SDL.SDL_Keycode.SDLK_KP_1 => KeyCode.Numpad1,
            SDL.SDL_Keycode.SDLK_KP_2 => KeyCode.Numpad2,
            SDL.SDL_Keycode.SDLK_KP_3 => KeyCode.Numpad3,
            SDL.SDL_Keycode.SDLK_KP_4 => KeyCode.Numpad4,
            SDL.SDL_Keycode.SDLK_KP_5 => KeyCode.Numpad5,
            SDL.SDL_Keycode.SDLK_KP_6 => KeyCode.Numpad6,
            SDL.SDL_Keycode.SDLK_KP_7 => KeyCode.Numpad7,
            SDL.SDL_Keycode.SDLK_KP_8 => KeyCode.Numpad8,
            SDL.SDL_Keycode.SDLK_KP_9 => KeyCode.Numpad9,
            SDL.SDL_Keycode.SDLK_LALT => KeyCode.LeftAlt,
            SDL.SDL_Keycode.SDLK_LCTRL => KeyCode.LeftControl,
            SDL.SDL_Keycode.SDLK_LEFTBRACKET => KeyCode.LeftBracket,
            SDL.SDL_Keycode.SDLK_LSHIFT => KeyCode.LeftShift,
            SDL.SDL_Keycode.SDLK_RALT => KeyCode.RightAlt,
            SDL.SDL_Keycode.SDLK_RCTRL => KeyCode.RightControl,
            SDL.SDL_Keycode.SDLK_RIGHTBRACKET => KeyCode.RightBracket,
            SDL.SDL_Keycode.SDLK_RSHIFT => KeyCode.RightShift,
            SDL.SDL_Keycode.SDLK_MINUS => KeyCode.Minus,
            SDL.SDL_Keycode.SDLK_PAGEDOWN => KeyCode.PageDown,
            SDL.SDL_Keycode.SDLK_PAGEUP => KeyCode.PageUp,
            SDL.SDL_Keycode.SDLK_PERIOD => KeyCode.Period,
            SDL.SDL_Keycode.SDLK_PRINTSCREEN => KeyCode.PrintScreen,
            SDL.SDL_Keycode.SDLK_RETURN => KeyCode.Enter,
            SDL.SDL_Keycode.SDLK_RETURN2 => KeyCode.Enter,
            SDL.SDL_Keycode.SDLK_SEMICOLON => KeyCode.SemiColon,
            SDL.SDL_Keycode.SDLK_SLASH => KeyCode.Slash,
            SDL.SDL_Keycode.SDLK_SPACE => KeyCode.Space,
            SDL.SDL_Keycode.SDLK_TAB => KeyCode.Tab,
            _ => KeyCode.Unknown,
        };
    }

    private static AppEventModifierKeys GetModifiers(SDL.SDL_Keymod mod)
    {
        AppEventModifierKeys modifiers = 0;

        if (mod.HasFlag(SDL.SDL_Keymod.SDL_KMOD_CAPS))
        {
            modifiers |= AppEventModifierKeys.CapsLock;
        }

        if(mod.HasFlag(SDL.SDL_Keymod.SDL_KMOD_ALT))
        {
            modifiers |= AppEventModifierKeys.Alt;
        }

        if (mod.HasFlag(SDL.SDL_Keymod.SDL_KMOD_CTRL))
        {
            modifiers |= AppEventModifierKeys.Control;
        }

        if (mod.HasFlag(SDL.SDL_Keymod.SDL_KMOD_SHIFT))
        {
            modifiers |= AppEventModifierKeys.Shift;
        }

        if (mod.HasFlag(SDL.SDL_Keymod.SDL_KMOD_NUM))
        {
            modifiers |= AppEventModifierKeys.NumLock;
        }

        return modifiers;
    }

    public void PollEvents()
    {
        while(SDL.SDL_PollEvent(out var _event))
        {
            switch((SDL.SDL_EventType)_event.type)
            {
                case SDL.SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:

                    windowFocused = true;

                    break;

                case SDL.SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:

                    windowFocused = false;

                    break;

                case SDL.SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED:

                    windowMaximized = true;

                    AppEventQueue.instance.Add(AppEvent.Maximize(windowMaximized));

                    break;

                case SDL.SDL_EventType.SDL_EVENT_WINDOW_RESTORED:

                    windowMaximized = false;

                    AppEventQueue.instance.Add(AppEvent.Maximize(windowMaximized));

                    break;

                case SDL.SDL_EventType.SDL_EVENT_WINDOW_MOVED:

                    SDL.SDL_GetWindowPosition(window, out var winX, out var winY);

                    AppEventQueue.instance.Add(AppEvent.MoveWindow(new Vector2Int(winX, winY)));

                    break;

                case SDL.SDL_EventType.SDL_EVENT_KEY_DOWN:
                case SDL.SDL_EventType.SDL_EVENT_KEY_UP:

                    AppEventQueue.instance.Add(AppEvent.Key(MapSDLKey((SDL.SDL_Keycode)_event.key.key), (int)_event.key.scancode,
                        _event.key.down ? AppEventInputState.Press : AppEventInputState.Release,
                        GetModifiers(_event.key.mod)));

                    break;

                case SDL.SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                case SDL.SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:

                    AppEventQueue.instance.Add(AppEvent.Mouse(_event.button.button switch
                        {
                            1 => AppEventMouseButton.Left,
                            2 => AppEventMouseButton.Middle,
                            3 => AppEventMouseButton.Right,
                            4 => AppEventMouseButton.Button1,
                            5 => AppEventMouseButton.Button2,
                            _ => 0,
                        },
                        _event.button.down ? AppEventInputState.Press : AppEventInputState.Release,
                        GetModifiers(SDL.SDL_GetModState())));

                    break;

                case SDL.SDL_EventType.SDL_EVENT_MOUSE_MOTION:

                    if(SDL.SDL_GetWindowRelativeMouseMode(window))
                    {
                        Input.CursorPosCallback(_event.motion.xrel, _event.motion.yrel);
                    }
                    else
                    {
                        Input.CursorPosCallback(_event.motion.x, _event.motion.y);
                    }

                    break;

                case SDL.SDL_EventType.SDL_EVENT_MOUSE_WHEEL:

                    Input.MouseScrollCallback(_event.wheel.x, _event.wheel.y);

                    break;

                case SDL.SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:

                    {
                        var instance = SDL.SDL_OpenGamepad(_event.cdevice.which);

                        var playerIndex = SDL.SDL_GetGamepadPlayerIndex(instance);

                        gamepads.Add(_event.cdevice.which, new()
                        {
                            instance = instance,
                            playerIndex = playerIndex,
                        });

                        Input.GamepadConnect(AppEvent.GamepadConnect(playerIndex, GamepadConnectionState.Connected));
                    }

                    break;

                case SDL.SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:

                    {
                        if(gamepads.TryGetValue(_event.cdevice.which, out var state))
                        {
                            SDL.SDL_CloseGamepad(state.instance);

                            gamepads.Remove(_event.cdevice.which);

                            Input.GamepadConnect(AppEvent.GamepadConnect(state.playerIndex, GamepadConnectionState.Disconnected));
                        }
                    }

                    break;

                case SDL.SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                case SDL.SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:

                    {
                        if (gamepads.TryGetValue(_event.cdevice.which, out var state))
                        {
                            Input.GamepadButton(AppEvent.GamepadButton(state.playerIndex,
                                (SDL.SDL_GamepadButton)_event.gbutton.button switch
                                {
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH => GamepadButton.A,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST => GamepadButton.B,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST => GamepadButton.X,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH => GamepadButton.Y,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK => GamepadButton.Back,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE => GamepadButton.Guide,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START => GamepadButton.Start,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK => GamepadButton.LeftStick,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK => GamepadButton.RightStick,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER => GamepadButton.LeftShoulder,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER => GamepadButton.RightShoulder,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP => GamepadButton.DPadUp,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN => GamepadButton.DPadDown,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT => GamepadButton.DPadLeft,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT => GamepadButton.DPadRight,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_MISC1 => GamepadButton.Misc1,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_PADDLE1 => GamepadButton.Paddle1,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_PADDLE1 => GamepadButton.Paddle2,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_PADDLE2 => GamepadButton.Paddle3,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_PADDLE2 => GamepadButton.Paddle4,
                                    SDL.SDL_GamepadButton.SDL_GAMEPAD_BUTTON_TOUCHPAD => GamepadButton.TouchPad,
                                    _ => GamepadButton.Invalid,
                                },
                                _event.gbutton.down ? AppEventInputState.Press : AppEventInputState.Release));
                        }
                    }

                    break;

                case SDL.SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:

                    {
                        if(gamepads.TryGetValue(_event.cdevice.which, out var state))
                        {
                            var value = _event.gaxis.value;

                            if (Math.Abs(value) <= AxisDeadzone)
                            {
                                value = 0;
                            }

                            var floatValue = value / (float)short.MaxValue;

                            var axis = (SDL.SDL_GamepadAxis)_event.gaxis.axis switch
                            {
                                SDL.SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX => GamepadAxis.LeftX,
                                SDL.SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY => GamepadAxis.LeftY,
                                SDL.SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX => GamepadAxis.RightX,
                                SDL.SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY => GamepadAxis.RightY,
                                SDL.SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER => GamepadAxis.TriggerLeft,
                                SDL.SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER => GamepadAxis.TriggerRight,
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

                case SDL.SDL_EventType.SDL_EVENT_QUIT:

                    closedWindow = true;

                    break;

                case SDL.SDL_EventType.SDL_EVENT_TEXT_INPUT:

                    unsafe
                    {
                        var len = 0;

                        while (_event.text.text[len] != '\0')
                        {
                            len++;
                        }

                        var text = Encoding.UTF8.GetString(_event.text.text, len);

                        Input.HandleTextEvent(AppEvent.Text(text.Length > 0 ? (uint)text[0] : 0));
                    }

                    break;
            }
        }

        var windowPosition = new Vector2Int();

        SDL.SDL_GetWindowPosition(window, out windowPosition.X, out windowPosition.Y);

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
        SDL.SDL_Init(SDL.SDL_InitFlags.SDL_INIT_TIMER |
            SDL.SDL_InitFlags.SDL_INIT_VIDEO |
            SDL.SDL_InitFlags.SDL_INIT_HAPTIC |
            SDL.SDL_InitFlags.SDL_INIT_GAMEPAD);
    }

    public void Terminate()
    {
        foreach(var pair in gamepads)
        {
            if(pair.Value.instance != nint.Zero)
            {
                SDL.SDL_CloseGamepad(pair.Value.instance);

                pair.Value.instance = nint.Zero;
            }
        }

        foreach (var cursor in cursors)
        {
            cursor.Dispose();
        }

        if (window != nint.Zero)
        {
            SDL.SDL_DestroyWindow(window);
        }

        SDL.SDL_Quit();
    }

    public void GetNativePlatformData(AppPlatform platform, out NativeWindowType type, out nint windowPointer, out nint monitorPointer)
    {
        windowPointer = nint.Zero;
        monitorPointer = nint.Zero;
        type = NativeWindowType.Other;

        switch (platform)
        {
            case AppPlatform.Windows:

                windowPointer = SDL.SDL_GetPointerProperty(SDL.SDL_GetWindowProperties(window), SDL.SDL_PROP_WINDOW_WIN32_HWND_POINTER, nint.Zero);

                break;

            case AppPlatform.Linux:

                switch(SDL.SDL_GetCurrentVideoDriver())
                {
                    case "x11":

                        type = NativeWindowType.X11;

                        windowPointer = (nint)SDL.SDL_GetNumberProperty(SDL.SDL_GetWindowProperties(window),
                            SDL.SDL_PROP_WINDOW_X11_WINDOW_NUMBER, 0);
                        monitorPointer = SDL.SDL_GetPointerProperty(SDL.SDL_GetWindowProperties(window),
                            SDL.SDL_PROP_WINDOW_X11_DISPLAY_POINTER, nint.Zero);

                        break;

                    case "wayland":

                        type = NativeWindowType.Wayland;

                        windowPointer = SDL.SDL_GetPointerProperty(SDL.SDL_GetWindowProperties(window),
                            SDL.SDL_PROP_WINDOW_WAYLAND_SURFACE_POINTER, nint.Zero);
                        monitorPointer = SDL.SDL_GetPointerProperty(SDL.SDL_GetWindowProperties(window),
                            SDL.SDL_PROP_WINDOW_WAYLAND_DISPLAY_POINTER, nint.Zero);

                        break;

                    default:

                        break;
                }

                break;

            case AppPlatform.MacOSX:

                if(metalView == nint.Zero)
                {
                    metalView = SDL.SDL_Metal_CreateView(window);
                }

                windowPointer = SDL.SDL_Metal_GetLayer(metalView);

                break;
        }
    }

    public void LockCursor()
    {
        SDL.SDL_SetWindowRelativeMouseMode(window, true);

        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        SDL.SDL_SetWindowRelativeMouseMode(window, false);

        Cursor.visible = true;
    }

    public void HideCursor()
    {
        SDL.SDL_HideCursor();
    }

    public void ShowCursor()
    {
        SDL.SDL_ShowCursor();
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

            var surface = SDL.SDL_CreateSurfaceFrom(icon.width, icon.height,
                SDL.SDL_GetPixelFormatForMasks(32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000),
                ptr, icon.width * 4);

            SDL.SDL_SetWindowIcon(window, (nint)surface);

            SDL.SDL_DestroySurface((nint)surface);
        }

        pinnedArray.Free();
    }

    public bool SetResolution(int width, int height, WindowMode windowMode)
    {
        switch (windowMode)
        {
            case WindowMode.Windowed:

                if(SDL.SDL_SetWindowFullscreen(window, false) == false)
                {
                    return false;
                }

                SDL.SDL_SetWindowSize(window, width, height);

                break;

            case WindowMode.ExclusiveFullscreen:

                SDL.SDL_SetWindowSize(window, width, height);

                if(SDL.SDL_SetWindowFullscreen(window, true) == false)
                {
                    return false;
                }

                break;

            case WindowMode.BorderlessFullscreen:

                if (SDL.SDL_SetWindowFullscreen(window, true) == false)
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
                var surface = SDL.SDL_CreateSurfaceFrom(width, height, SDL.SDL_PixelFormat.SDL_PIXELFORMAT_ARGB32, (nint)ptr, width * 4);

                if(surface == null)
                {
                    image = default;

                    return false;
                }

                var cursor = SDL.SDL_CreateColorCursor((nint)surface, hotX, hotY);

                if(cursor == nint.Zero)
                {
                    SDL.SDL_DestroySurface((nint)surface);

                    image = default;

                    return false;
                }

                outValue.surface = (nint)surface;
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
            SDL.SDL_SetCursor(defaultCursor);

            return;
        }

        SDL.SDL_SetCursor(cursor.cursor);
    }

    public void ShowTextInput()
    {
        if (window == nint.Zero)
        {
            return;
        }

        SDL.SDL_StartTextInput(window);
    }

    public void HideTextInput()
    {
        if (window == nint.Zero)
        {
            return;
        }

        SDL.SDL_StopTextInput(window);
    }
}
#endif