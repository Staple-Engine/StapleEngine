using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal;

internal enum AppEventType
{
    ResetFlags,
    KeyDown,
    KeyUp,
    MouseDown,
    MouseUp,
    MouseDelta,
    Text,
    MaximizeWindow,
    MoveWindow,
    Touch,
    GamepadConnect,
    GamepadMovement,
    GamepadButton,
}

internal enum AppEventInputState
{
    Release,
    Press,
    Repeat,
}

internal enum AppEventMouseButton
{
    Button1,
    Button2,
    Button3,
    Button4,
    Button5,
    Button6,
    Button7,
    Button8,

    Left = Button1,

    Right = Button2,

    Middle = Button3
}

[Flags]
internal enum AppEventModifierKeys
{
    Shift = 0x0001,

    Control = 0x0002,

    Alt = 0x0004,

    Super = 0x0008,

    CapsLock = 0x0010,

    NumLock = 0x0020
}

internal struct MouseEvent
{
    public AppEventMouseButton button;
    public AppEventInputState state;
    public AppEventModifierKeys modifiers;
}

internal struct KeyboardEvent
{
    public KeyCode key;
    public int scancode;
    public AppEventInputState state;
    public AppEventModifierKeys mods;
}

internal struct ResetEvent
{
    public Bgfx.bgfx.ResetFlags resetFlags;
}

internal struct MouseDeltaEvent
{
    public Vector2 delta;
}

internal struct MaximizeWindowEvent
{
    public bool maximized;
}

internal struct MoveWindowEvent
{
    public Vector2Int windowPosition;
}

internal struct TouchEvent
{
    public int touchID;
    public Vector2 position;
    public AppEventInputState state;
}

internal struct GamepadConnectEvent
{
    public int index;
    public GamepadConnectionState state;
}

internal struct GamepadButtonEvent
{
    public int index;
    public GamepadButton button;
    public AppEventInputState state;
}

internal struct GamepadMovement
{
    public int index;
    public GamepadAxis axis;
    public float movement; //-1 to 1
}

/// <summary>
/// Stores information on an app event
/// </summary>
internal class AppEvent
{
    public AppEventType type;
    public uint character;
    public KeyboardEvent key;
    public MouseEvent mouse;
    public ResetEvent reset;
    public MouseDeltaEvent mouseDelta;
    public MaximizeWindowEvent maximizeWindow;
    public MoveWindowEvent moveWindow;
    public TouchEvent touch;
    public GamepadConnectEvent gamepadConnect;
    public GamepadButtonEvent gamepadButton;
    public GamepadMovement gamepadMovement;

    public static AppEvent Touch(int touchID, Vector2 position, AppEventInputState state)
    {
        return new()
        {
            type = AppEventType.Touch,
            touch = new()
            {
                touchID = touchID,
                position = position,
                state = state,
            },
        };
    }

    public static AppEvent MoveWindow(Vector2Int position)
    {
        return new()
        {
            type = AppEventType.MoveWindow,
            moveWindow = new()
            {
                windowPosition = position,
            },
        };
    }

    public static AppEvent Maximize(bool maximized)
    {
        return new()
        {
            type = AppEventType.MaximizeWindow,
            maximizeWindow = new()
            {
                maximized = maximized,
            },
        };
    }

    public static AppEvent Text(uint codepoint)
    {
        return new()
        {
            type = AppEventType.Text,
            character = codepoint,
        };
    }

    public static AppEvent ResetFlags(Bgfx.bgfx.ResetFlags flags)
    {
        return new()
        {
            type = AppEventType.ResetFlags,
            reset = new()
            {
                resetFlags = flags,
            },
        };
    }

    public static AppEvent Key(KeyCode key, int scancode, AppEventInputState state, AppEventModifierKeys mods)
    {
        var keyEvent = new KeyboardEvent()
        {
            key = key,
            scancode = scancode,
            state = state,
            mods = mods,
        };

        return new()
        {
            type = state != AppEventInputState.Release ? AppEventType.KeyDown : AppEventType.KeyUp,
            key = keyEvent,
        };
    }

    public static AppEvent Mouse(AppEventMouseButton button, AppEventInputState state, AppEventModifierKeys modifiers)
    {
        var mouseEvent = new MouseEvent()
        {
            button = button,
            state = state,
            modifiers = modifiers
        };

        return new()
        {
            type = state == AppEventInputState.Press ? AppEventType.MouseDown : AppEventType.MouseUp,
            mouse = mouseEvent,
        };
    }

    public static AppEvent MouseDelta(Vector2 delta)
    {
        return new()
        {
            type = AppEventType.MouseDelta,
            mouseDelta = new()
            {
                delta = delta,
            },
        };
    }

    public static AppEvent GamepadConnect(int index, GamepadConnectionState state)
    {
        return new()
        {
            type = AppEventType.GamepadConnect,
            gamepadConnect = new()
            {
                index = index,
                state = state,
            }
        };
    }

    public static AppEvent GamepadButton(int index, GamepadButton button, AppEventInputState state)
    {
        return new()
        {
            type = AppEventType.GamepadButton,
            gamepadButton = new()
            {
                index = index,
                button = button,
                state = state,
            }
        };
    }

    public static AppEvent GamepadMovement(int index, GamepadAxis axis, float movement)
    {
        return new()
        {
            type = AppEventType.GamepadMovement,
            gamepadMovement = new()
            {
                index = index,
                axis = axis,
                movement = movement,
            }
        };
    }
}

internal class AppEventQueue
{
    public static readonly AppEventQueue instance = new();

    private readonly Queue<AppEvent> events = new();
    private readonly object stackLock = new();

    public AppEvent Next()
    {
        lock(stackLock)
        {
            if(events.Count == 0)
            {
                return null;
            }

            return events.Dequeue();
        }
    }

    public void Add(AppEvent appEvent)
    {
        lock(stackLock)
        {
            events.Enqueue(appEvent);
        }
    }
}
