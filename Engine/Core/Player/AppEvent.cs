using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.Internal
{
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
    }

    internal enum InputState
    {
        Release,
        Press,
        Repeat,
    }

    internal enum MouseButton
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
    internal enum ModifierKeys
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
        public MouseButton button;
        public InputState state;
        public ModifierKeys modifiers;
    }

    internal struct KeyboardEvent
    {
        public KeyCode key;
        public int scancode;
        public InputState state;
        public ModifierKeys mods;
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
        public Bgfx.bgfx.ResetFlags resetFlags;
        public Vector2 mouseDelta;
        public bool maximized;
        public Vector2Int windowPosition;

        public static AppEvent MoveWindow(Vector2Int position)
        {
            return new()
            {
                type = AppEventType.MoveWindow,
                windowPosition = position,
            };
        }

        public static AppEvent Maximize(bool maximized)
        {
            return new()
            {
                type = AppEventType.MaximizeWindow,
                maximized = maximized,
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
                resetFlags = flags
            };
        }

        public static AppEvent Key(KeyCode key, int scancode, InputState state, ModifierKeys mods)
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
                type = state != InputState.Release ? AppEventType.KeyDown : AppEventType.KeyUp,
                key = keyEvent,
            };
        }

        public static AppEvent Mouse(MouseButton button, InputState state, ModifierKeys modifiers)
        {
            var mouseEvent = new MouseEvent()
            {
                button = button,
                state = state,
                modifiers = modifiers
            };

            return new()
            {
                type = state == InputState.Press ? AppEventType.MouseDown : AppEventType.MouseUp,
                mouse = mouseEvent,
            };
        }

        public static AppEvent MouseDelta(Vector2 delta)
        {
            return new()
            {
                type = AppEventType.MouseDelta,
                mouseDelta = delta,
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
}
