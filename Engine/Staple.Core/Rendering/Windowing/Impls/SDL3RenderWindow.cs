using SDL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Internal;

internal unsafe class SDL3RenderWindow : IRenderWindow
{
    private const short AxisDeadzone = 8000;

    private unsafe class SDL3Cursor : CursorImage
    {
        public Color32[] pixels;
        public SDL_Surface *surface = null;
        public SDL_Cursor *cursor = null;

        public override void Dispose()
        {
            if (cursor != null)
            {
                SDL3.SDL_DestroyCursor(cursor);

                cursor = null;
            }

            if (surface != null)
            {
                SDL3.SDL_DestroySurface(surface);

                surface = null;
            }

            pixels = [];
        }
    }

    private unsafe class GamepadState
    {
        public SDL_Gamepad *instance;
        public int playerIndex;
    }

    public SDL_Window *window = null;

    private readonly Dictionary<SDL_JoystickID, GamepadState> gamepads = [];

    private readonly List<SDL3Cursor> cursors = [];

    private bool movedWindow = false;
    private DateTime movedWindowTimer;
    private Vector2Int previousWindowPosition;
    private SDL_Cursor *defaultCursor = null;
    private SDL_DisplayID[] displays = [];

    public bool ContextLost { get; set; } = false;

    public bool IsFocused { get; private set; } = true;

    public bool ShouldClose { get; private set; } = false;

    public bool Unavailable => false;

    public bool Maximized { get; private set; } = false;

    public int RefreshRate { get; private set; } = 60;

    public string Title
    {
        get => SDL3.SDL_GetWindowTitle(window);

        set => SDL3.SDL_SetWindowTitle(window, value);
    }

    public Vector2Int Position
    {
        get
        {
            var x = 0;
            var y = 0;

            SDL3.SDL_GetWindowPosition(window, &x, &y);

            return new(x, y);
        }

        set
        {
            SDL3.SDL_SetWindowPosition(window, value.X, value.Y);
        }
    }

    public Vector2Int Size
    {
        get
        {
            var w = 0;
            var h = 0;

            SDL3.SDL_GetWindowSize(window, &w, &h);

            return new(w, h);
        }
    }

    public int MonitorIndex
    {
        get
        {
            if (window == null)
            {
                return 0;
            }

            return Array.IndexOf(displays, SDL3.SDL_GetDisplayForWindow(window));
        }
    }

    private static int CenteredDisplay(SDL_DisplayID monitor)
    {
        return (int)(0x2FFF0000u | (int)monitor);
    }

    private static int UndefinedDisplay(SDL_DisplayID monitor)
    {
        return (int)(0x1FFF0000u | (int)monitor);
    }

    public bool Create(ref int width, ref int height, string title, bool resizable, WindowMode windowMode, Vector2Int? position,
        bool maximized, int monitorIndex)
    {
        var displayCount = 0;

        var d = SDL3.SDL_GetDisplays(&displayCount);

        displays = new SDL_DisplayID[displayCount];

        var from = new Span<SDL_DisplayID>(d, displayCount);
        var to = displays.AsSpan();

        from.CopyTo(to);

        if (displays == null)
        {
            return false;
        }

        var monitor = monitorIndex >= 0 && monitorIndex < displayCount ? this.displays[monitorIndex] : 0;

        SDL_Rect displayBounds;

        SDL3.SDL_GetDisplayBounds(monitor, &displayBounds);

        var windowFlags = SDL_WindowFlags.SDL_WINDOW_HIGH_PIXEL_DENSITY;

        if (resizable && windowMode == WindowMode.Windowed)
        {
            windowFlags |= SDL_WindowFlags.SDL_WINDOW_RESIZABLE;
        }

        if (maximized)
        {
            windowFlags |= SDL_WindowFlags.SDL_WINDOW_MAXIMIZED;
        }

        var windowPosition = new Vector2Int();

        switch (windowMode)
        {
            case WindowMode.Windowed:

                if (position.HasValue)
                {
                    windowPosition = position.Value;
                }
                else
                {
                    windowPosition = new Vector2Int(CenteredDisplay(monitor), CenteredDisplay(monitor));
                }

                break;

            case WindowMode.ExclusiveFullscreen:

                windowFlags |= SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;

                windowPosition = new Vector2Int(UndefinedDisplay(monitor), UndefinedDisplay(monitor));

                break;

            case WindowMode.BorderlessFullscreen:

                windowFlags |= SDL_WindowFlags.SDL_WINDOW_FULLSCREEN;

                windowPosition = new Vector2Int(UndefinedDisplay(monitor), UndefinedDisplay(monitor));

                width = displayBounds.w;
                height = displayBounds.h;

                break;
        }

        var props = SDL3.SDL_CreateProperties();

        SDL3.SDL_SetStringProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_TITLE_STRING, title);
        SDL3.SDL_SetNumberProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_X_NUMBER, windowPosition.X);
        SDL3.SDL_SetNumberProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_Y_NUMBER, windowPosition.Y);
        SDL3.SDL_SetNumberProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_WIDTH_NUMBER, width);
        SDL3.SDL_SetNumberProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_HEIGHT_NUMBER, height);
        SDL3.SDL_SetNumberProperty(props, SDL3.SDL_PROP_WINDOW_CREATE_FLAGS_NUMBER, (long)windowFlags);

        window = SDL3.SDL_CreateWindowWithProperties(props);

        SDL3.SDL_DestroyProperties(props);

        if (window == null)
        {
            return false;
        }

        if (windowMode == WindowMode.BorderlessFullscreen)
        {
            SDL3.SDL_SetWindowFullscreen(window, true);
        }

        SDL_DisplayMode* mode = SDL3.SDL_GetCurrentDisplayMode(monitor);

        if (mode != null)
        {
            RefreshRate = (int)mode->refresh_rate;
        }

        if (maximized)
        {
            Maximized = true;
        }

        defaultCursor = SDL3.SDL_CreateSystemCursor(SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT);

        return true;
    }

    private static KeyCode MapSDLKey(SDL_Keycode sym)
    {
        return sym switch
        {
            SDL_Keycode.SDLK_0 => KeyCode.Alpha0,
            SDL_Keycode.SDLK_1 => KeyCode.Alpha1,
            SDL_Keycode.SDLK_2 => KeyCode.Alpha2,
            SDL_Keycode.SDLK_3 => KeyCode.Alpha3,
            SDL_Keycode.SDLK_4 => KeyCode.Alpha4,
            SDL_Keycode.SDLK_5 => KeyCode.Alpha5,
            SDL_Keycode.SDLK_6 => KeyCode.Alpha6,
            SDL_Keycode.SDLK_7 => KeyCode.Alpha7,
            SDL_Keycode.SDLK_8 => KeyCode.Alpha8,
            SDL_Keycode.SDLK_9 => KeyCode.Alpha9,
            SDL_Keycode.SDLK_A => KeyCode.A,
            SDL_Keycode.SDLK_B => KeyCode.B,
            SDL_Keycode.SDLK_C => KeyCode.C,
            SDL_Keycode.SDLK_D => KeyCode.D,
            SDL_Keycode.SDLK_E => KeyCode.E,
            SDL_Keycode.SDLK_F => KeyCode.F,
            SDL_Keycode.SDLK_G => KeyCode.G,
            SDL_Keycode.SDLK_H => KeyCode.H,
            SDL_Keycode.SDLK_I => KeyCode.I,
            SDL_Keycode.SDLK_J => KeyCode.J,
            SDL_Keycode.SDLK_K => KeyCode.K,
            SDL_Keycode.SDLK_L => KeyCode.L,
            SDL_Keycode.SDLK_M => KeyCode.M,
            SDL_Keycode.SDLK_N => KeyCode.N,
            SDL_Keycode.SDLK_O => KeyCode.O,
            SDL_Keycode.SDLK_P => KeyCode.P,
            SDL_Keycode.SDLK_Q => KeyCode.Q,
            SDL_Keycode.SDLK_R => KeyCode.R,
            SDL_Keycode.SDLK_S => KeyCode.S,
            SDL_Keycode.SDLK_T => KeyCode.T,
            SDL_Keycode.SDLK_U => KeyCode.U,
            SDL_Keycode.SDLK_V => KeyCode.V,
            SDL_Keycode.SDLK_W => KeyCode.W,
            SDL_Keycode.SDLK_X => KeyCode.X,
            SDL_Keycode.SDLK_Y => KeyCode.Y,
            SDL_Keycode.SDLK_Z => KeyCode.Z,
            SDL_Keycode.SDLK_BACKSLASH => KeyCode.Backslash,
            SDL_Keycode.SDLK_BACKSPACE => KeyCode.Backspace,
            SDL_Keycode.SDLK_CAPSLOCK => KeyCode.CapsLock,
            SDL_Keycode.SDLK_COMMA => KeyCode.Comma,
            SDL_Keycode.SDLK_DELETE => KeyCode.Delete,
            SDL_Keycode.SDLK_DOWN => KeyCode.Down,
            SDL_Keycode.SDLK_UP => KeyCode.Up,
            SDL_Keycode.SDLK_LEFT => KeyCode.Left,
            SDL_Keycode.SDLK_RIGHT => KeyCode.Right,
            SDL_Keycode.SDLK_END => KeyCode.End,
            SDL_Keycode.SDLK_EQUALS => KeyCode.Equal,
            SDL_Keycode.SDLK_ESCAPE => KeyCode.Escape,
            SDL_Keycode.SDLK_F1 => KeyCode.F1,
            SDL_Keycode.SDLK_F2 => KeyCode.F2,
            SDL_Keycode.SDLK_F3 => KeyCode.F3,
            SDL_Keycode.SDLK_F4 => KeyCode.F4,
            SDL_Keycode.SDLK_F5 => KeyCode.F5,
            SDL_Keycode.SDLK_F6 => KeyCode.F6,
            SDL_Keycode.SDLK_F7 => KeyCode.F7,
            SDL_Keycode.SDLK_F8 => KeyCode.F8,
            SDL_Keycode.SDLK_F9 => KeyCode.F9,
            SDL_Keycode.SDLK_F10 => KeyCode.F10,
            SDL_Keycode.SDLK_F11 => KeyCode.F11,
            SDL_Keycode.SDLK_F12 => KeyCode.F12,
            SDL_Keycode.SDLK_F13 => KeyCode.F13,
            SDL_Keycode.SDLK_F14 => KeyCode.F14,
            SDL_Keycode.SDLK_F15 => KeyCode.F15,
            SDL_Keycode.SDLK_F16 => KeyCode.F16,
            SDL_Keycode.SDLK_F17 => KeyCode.F17,
            SDL_Keycode.SDLK_F18 => KeyCode.F18,
            SDL_Keycode.SDLK_F19 => KeyCode.F19,
            SDL_Keycode.SDLK_F20 => KeyCode.F20,
            SDL_Keycode.SDLK_F21 => KeyCode.F21,
            SDL_Keycode.SDLK_F22 => KeyCode.F22,
            SDL_Keycode.SDLK_F23 => KeyCode.F23,
            SDL_Keycode.SDLK_F24 => KeyCode.F24,
            SDL_Keycode.SDLK_HOME => KeyCode.Home,
            SDL_Keycode.SDLK_INSERT => KeyCode.Insert,
            SDL_Keycode.SDLK_KP_0 => KeyCode.Numpad0,
            SDL_Keycode.SDLK_KP_1 => KeyCode.Numpad1,
            SDL_Keycode.SDLK_KP_2 => KeyCode.Numpad2,
            SDL_Keycode.SDLK_KP_3 => KeyCode.Numpad3,
            SDL_Keycode.SDLK_KP_4 => KeyCode.Numpad4,
            SDL_Keycode.SDLK_KP_5 => KeyCode.Numpad5,
            SDL_Keycode.SDLK_KP_6 => KeyCode.Numpad6,
            SDL_Keycode.SDLK_KP_7 => KeyCode.Numpad7,
            SDL_Keycode.SDLK_KP_8 => KeyCode.Numpad8,
            SDL_Keycode.SDLK_KP_9 => KeyCode.Numpad9,
            SDL_Keycode.SDLK_LALT => KeyCode.LeftAlt,
            SDL_Keycode.SDLK_LCTRL => KeyCode.LeftControl,
            SDL_Keycode.SDLK_LEFTBRACKET => KeyCode.LeftBracket,
            SDL_Keycode.SDLK_LSHIFT => KeyCode.LeftShift,
            SDL_Keycode.SDLK_RALT => KeyCode.RightAlt,
            SDL_Keycode.SDLK_RCTRL => KeyCode.RightControl,
            SDL_Keycode.SDLK_RIGHTBRACKET => KeyCode.RightBracket,
            SDL_Keycode.SDLK_RSHIFT => KeyCode.RightShift,
            SDL_Keycode.SDLK_MINUS => KeyCode.Minus,
            SDL_Keycode.SDLK_PAGEDOWN => KeyCode.PageDown,
            SDL_Keycode.SDLK_PAGEUP => KeyCode.PageUp,
            SDL_Keycode.SDLK_PERIOD => KeyCode.Period,
            SDL_Keycode.SDLK_PRINTSCREEN => KeyCode.PrintScreen,
            SDL_Keycode.SDLK_RETURN => KeyCode.Enter,
            SDL_Keycode.SDLK_RETURN2 => KeyCode.Enter,
            SDL_Keycode.SDLK_SEMICOLON => KeyCode.SemiColon,
            SDL_Keycode.SDLK_SLASH => KeyCode.Slash,
            SDL_Keycode.SDLK_SPACE => KeyCode.Space,
            SDL_Keycode.SDLK_TAB => KeyCode.Tab,
            _ => KeyCode.Unknown,
        };
    }

    private static AppEventModifierKeys GetModifiers(SDL_Keymod mod)
    {
        AppEventModifierKeys modifiers = 0;

        if (mod.HasFlag(SDL_Keymod.SDL_KMOD_CAPS))
        {
            modifiers |= AppEventModifierKeys.CapsLock;
        }

        if (mod.HasFlag(SDL_Keymod.SDL_KMOD_ALT))
        {
            modifiers |= AppEventModifierKeys.Alt;
        }

        if (mod.HasFlag(SDL_Keymod.SDL_KMOD_CTRL))
        {
            modifiers |= AppEventModifierKeys.Control;
        }

        if (mod.HasFlag(SDL_Keymod.SDL_KMOD_SHIFT))
        {
            modifiers |= AppEventModifierKeys.Shift;
        }

        if (mod.HasFlag(SDL_Keymod.SDL_KMOD_NUM))
        {
            modifiers |= AppEventModifierKeys.NumLock;
        }

        return modifiers;
    }

    public void PollEvents()
    {
        SDL_Event _event;

        unsafe
        {
            while (SDL3.SDL_PollEvent(&_event))
            {
                switch (_event.Type)
                {
                    case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED:

                        IsFocused = true;

                        break;

                    case SDL_EventType.SDL_EVENT_WINDOW_FOCUS_LOST:

                        IsFocused = false;

                        break;

                    case SDL_EventType.SDL_EVENT_WINDOW_MAXIMIZED:

                        Maximized = true;

                        AppEventQueue.instance.Add(AppEvent.Maximize(Maximized));

                        break;

                    case SDL_EventType.SDL_EVENT_WINDOW_RESTORED:

                        Maximized = false;

                        AppEventQueue.instance.Add(AppEvent.Maximize(Maximized));

                        break;

                    case SDL_EventType.SDL_EVENT_WINDOW_MOVED:

                        unsafe
                        {
                            var winX = 0;
                            var winY = 0;

                            SDL3.SDL_GetWindowPosition(window, &winX, &winY);

                            AppEventQueue.instance.Add(AppEvent.MoveWindow(new Vector2Int(winX, winY)));
                        }

                        break;

                    case SDL_EventType.SDL_EVENT_KEY_DOWN:
                    case SDL_EventType.SDL_EVENT_KEY_UP:

                        AppEventQueue.instance.Add(AppEvent.Key(MapSDLKey((SDL_Keycode)_event.key.key), (int)_event.key.scancode,
                            _event.key.down ? AppEventInputState.Press : AppEventInputState.Release,
                            GetModifiers(_event.key.mod)));

                        break;

                    case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_DOWN:
                    case SDL_EventType.SDL_EVENT_MOUSE_BUTTON_UP:

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
                            GetModifiers(SDL3.SDL_GetModState())));

                        break;

                    case SDL_EventType.SDL_EVENT_MOUSE_MOTION:

                        if (SDL3.SDL_GetWindowRelativeMouseMode(window))
                        {
                            Input.CursorPosCallback(_event.motion.xrel, _event.motion.yrel);
                        }
                        else
                        {
                            Input.CursorPosCallback(_event.motion.x, _event.motion.y);
                        }

                        break;

                    case SDL_EventType.SDL_EVENT_MOUSE_WHEEL:

                        Input.MouseScrollCallback(_event.wheel.x, _event.wheel.y);

                        break;

                    case SDL_EventType.SDL_EVENT_GAMEPAD_ADDED:

                        {
                            var instance = SDL3.SDL_OpenGamepad(_event.gdevice.which);

                            var playerIndex = SDL3.SDL_GetGamepadPlayerIndex(instance);

                            gamepads.Add(_event.gdevice.which, new()
                            {
                                instance = instance,
                                playerIndex = playerIndex,
                            });

                            Input.GamepadConnect(AppEvent.GamepadConnect(playerIndex, GamepadConnectionState.Connected));
                        }

                        break;

                    case SDL_EventType.SDL_EVENT_GAMEPAD_REMOVED:

                        {
                            if (gamepads.TryGetValue(_event.gdevice.which, out var state))
                            {
                                SDL3.SDL_CloseGamepad(state.instance);

                                gamepads.Remove(_event.gdevice.which);

                                Input.GamepadConnect(AppEvent.GamepadConnect(state.playerIndex, GamepadConnectionState.Disconnected));
                            }
                        }

                        break;

                    case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_DOWN:
                    case SDL_EventType.SDL_EVENT_GAMEPAD_BUTTON_UP:

                        {
                            if (gamepads.TryGetValue(_event.gdevice.which, out var state))
                            {
                                Input.GamepadButton(AppEvent.GamepadButton(state.playerIndex,
                                    (SDL_GamepadButton)_event.gbutton.button switch
                                    {
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH => GamepadButton.A,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST => GamepadButton.B,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST => GamepadButton.X,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH => GamepadButton.Y,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK => GamepadButton.Back,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE => GamepadButton.Guide,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START => GamepadButton.Start,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK => GamepadButton.LeftStick,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK => GamepadButton.RightStick,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER => GamepadButton.LeftShoulder,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER => GamepadButton.RightShoulder,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP => GamepadButton.DPadUp,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN => GamepadButton.DPadDown,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT => GamepadButton.DPadLeft,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT => GamepadButton.DPadRight,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_MISC1 => GamepadButton.Misc1,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_PADDLE1 => GamepadButton.Paddle1,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_PADDLE1 => GamepadButton.Paddle2,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_PADDLE2 => GamepadButton.Paddle3,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_PADDLE2 => GamepadButton.Paddle4,
                                        SDL_GamepadButton.SDL_GAMEPAD_BUTTON_TOUCHPAD => GamepadButton.TouchPad,
                                        _ => GamepadButton.Invalid,
                                    },
                                    _event.gbutton.down ? AppEventInputState.Press : AppEventInputState.Release));
                            }
                        }

                        break;

                    case SDL_EventType.SDL_EVENT_GAMEPAD_AXIS_MOTION:

                        {
                            if (gamepads.TryGetValue(_event.gdevice.which, out var state))
                            {
                                var value = _event.gaxis.value;

                                if (Math.Abs(value) <= AxisDeadzone)
                                {
                                    value = 0;
                                }

                                var floatValue = value / (float)short.MaxValue;

                                var axis = (SDL_GamepadAxis)_event.gaxis.axis switch
                                {
                                    SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX => GamepadAxis.LeftX,
                                    SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY => GamepadAxis.LeftY,
                                    SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX => GamepadAxis.RightX,
                                    SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY => GamepadAxis.RightY,
                                    SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER => GamepadAxis.TriggerLeft,
                                    SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER => GamepadAxis.TriggerRight,
                                    _ => GamepadAxis.Invalid,
                                };

                                if (axis == GamepadAxis.LeftY || axis == GamepadAxis.RightY)
                                {
                                    floatValue *= -1;
                                }

                                Input.GamepadMovement(AppEvent.GamepadMovement(state.playerIndex, axis, floatValue));
                            }
                        }

                        break;

                    case SDL_EventType.SDL_EVENT_QUIT:

                        ShouldClose = true;

                        break;

                    case SDL_EventType.SDL_EVENT_TEXT_INPUT:

                        unsafe
                        {
                            var ptr = _event.text.text;

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
        }

        var windowPosition = new Vector2Int();

        unsafe
        {
            SDL3.SDL_GetWindowPosition(window, &windowPosition.X, &windowPosition.Y);
        }

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
        SDL3.SDL_Init(SDL_InitFlags.SDL_INIT_VIDEO | SDL_InitFlags.SDL_INIT_HAPTIC | SDL_InitFlags.SDL_INIT_GAMEPAD);
    }

    public void Terminate()
    {
        unsafe
        {
            foreach (var pair in gamepads)
            {
                if (pair.Value.instance != null)
                {
                    SDL3.SDL_CloseGamepad(pair.Value.instance);

                    pair.Value.instance = null;
                }
            }

            foreach (var cursor in cursors)
            {
                cursor.Dispose();
            }

            if (window != null)
            {
                SDL3.SDL_DestroyWindow(window);
            }

            SDL3.SDL_Quit();
        }
    }

    public void LockCursor()
    {
        SDL3.SDL_SetWindowRelativeMouseMode(window, true);

        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        SDL3.SDL_SetWindowRelativeMouseMode(window, false);

        Cursor.visible = true;
    }

    public void HideCursor()
    {
        SDL3.SDL_HideCursor();
    }

    public void ShowCursor()
    {
        SDL3.SDL_ShowCursor();
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

            var surface = SDL3.SDL_CreateSurfaceFrom(icon.width, icon.height,
                SDL3.SDL_GetPixelFormatForMasks(32, 0x000000FF, 0x0000FF00, 0x00FF0000, 0xFF000000),
                ptr, icon.width * 4);

            SDL3.SDL_SetWindowIcon(window, surface);

            SDL3.SDL_DestroySurface(surface);
        }

        pinnedArray.Free();
    }

    public bool SetResolution(int width, int height, WindowMode windowMode)
    {
        switch (windowMode)
        {
            case WindowMode.Windowed:

                if (!SDL3.SDL_SetWindowFullscreen(window, false))
                {
                    return false;
                }

                SDL3.SDL_SetWindowSize(window, width, height);

                break;

            case WindowMode.ExclusiveFullscreen:

                SDL3.SDL_SetWindowSize(window, width, height);

                if (!SDL3.SDL_SetWindowFullscreen(window, true))
                {
                    return false;
                }

                break;

            case WindowMode.BorderlessFullscreen:

                if (!SDL3.SDL_SetWindowFullscreen(window, true))
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

            fixed (void* ptr = outValue.pixels)
            {
                var surface = SDL3.SDL_CreateSurfaceFrom(width, height, SDL_PixelFormat.SDL_PIXELFORMAT_ARGB8888, (nint)ptr,
                    width * 4);

                if (surface == null)
                {
                    image = default;

                    return false;
                }

                var cursor = SDL3.SDL_CreateColorCursor(surface, hotX, hotY);

                if (cursor == null)
                {
                    SDL3.SDL_DestroySurface(surface);

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
        if (image is not SDL3Cursor cursor ||
            cursor.cursor == null)
        {
            SDL3.SDL_SetCursor(defaultCursor);

            return;
        }

        SDL3.SDL_SetCursor(cursor.cursor);
    }

    public void ShowTextInput()
    {
        if (window == null)
        {
            return;
        }

        SDL3.SDL_StartTextInput(window);
    }

    public void HideTextInput()
    {
        if (window == null)
        {
            return;
        }

        SDL3.SDL_StopTextInput(window);
    }
}
