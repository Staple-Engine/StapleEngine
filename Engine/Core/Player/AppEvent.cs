﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
    }

    internal struct MouseEvent
    {
        public GLFW.MouseButton button;
        public GLFW.InputState state;
        public GLFW.ModifierKeys modifiers;
    }

    internal struct KeyboardEvent
    {
        public GLFW.Keys key;
        public int scancode;
        public GLFW.InputState state;
        public GLFW.ModifierKeys mods;
    }

    internal class AppEvent
    {
        public AppEventType type;
        public uint character;
        public KeyboardEvent key;
        public MouseEvent mouse;
        public Bgfx.bgfx.ResetFlags resetFlags;
        public Vector2 mouseDelta;

        public static AppEvent Text(uint codepoint)
        {
            return new AppEvent()
            {
                type = AppEventType.Text,
                character = codepoint,
            };
        }

        public static AppEvent ResetFlags(Bgfx.bgfx.ResetFlags flags)
        {
            return new AppEvent()
            {
                type = AppEventType.ResetFlags,
                resetFlags = flags
            };
        }

        public static AppEvent Key(GLFW.Keys key, int scancode, GLFW.InputState state, GLFW.ModifierKeys mods)
        {
            var keyEvent = new KeyboardEvent()
            {
                key = key,
                scancode = scancode,
                state = state,
                mods = mods,
            };

            return new AppEvent()
            {
                type = state != GLFW.InputState.Release ? AppEventType.KeyDown : AppEventType.KeyUp,
                key = keyEvent,
            };
        }

        public static AppEvent Mouse(GLFW.MouseButton button, GLFW.InputState state, GLFW.ModifierKeys modifiers)
        {
            var mouseEvent = new MouseEvent()
            {
                button = button,
                state = state,
                modifiers = modifiers
            };

            return new AppEvent()
            {
                type = state == GLFW.InputState.Press ? AppEventType.MouseDown : AppEventType.MouseUp,
                mouse = mouseEvent,
            };
        }
    }

    internal class AppEventQueue
    {
        public static AppEventQueue instance = new AppEventQueue();

        private Stack<AppEvent> events = new Stack<AppEvent>();
        private object stackLock = new object();

        public AppEvent Next()
        {
            lock(stackLock)
            {
                if(events.Count == 0)
                {
                    return null;
                }

                return events.Pop();
            }
        }

        public void Add(AppEvent appEvent)
        {
            lock(stackLock)
            {
                events.Push(appEvent);
            }
        }
    }
}