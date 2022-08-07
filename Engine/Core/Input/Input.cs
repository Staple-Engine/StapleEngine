using GLFW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public static class Input
    {
        private enum InputState
        {
            Press,
            FirstPress,
            Release,
            FirstRelease
        }

        private static Dictionary<KeyCode, InputState> keyStates = new Dictionary<KeyCode, InputState>();

        private static Dictionary<MouseButton, InputState> mouseButtonStates = new Dictionary<MouseButton, InputState>();

        public static uint Character { get; internal set; }

        public static Vector2 MousePosition { get; private set; }

        public static Vector2 MouseDelta { get; private set; }

        internal static Window window;

        internal static void MouseScrollCallback(float xOffset, float yOffset)
        {
            MouseDelta = new Vector2(xOffset, yOffset);
        }

        internal static void MouseButtonCallback(GLFW.MouseButton button, GLFW.InputState state, ModifierKeys modifiers)
        {
            MouseButton mouseButton = (MouseButton)button;

            bool pressed = state == GLFW.InputState.Press;

            InputState mouseButtonState = pressed ? InputState.FirstPress : InputState.Release;

            if (mouseButtonStates.ContainsKey(mouseButton))
            {
                mouseButtonState = mouseButtonStates[mouseButton];

                if (pressed)
                {
                    if (mouseButtonState == InputState.FirstPress)
                    {
                        mouseButtonState = InputState.Press;
                    }
                    else
                    {
                        mouseButtonState = InputState.FirstPress;
                    }
                }
                else
                {
                    if (mouseButtonState == InputState.FirstRelease)
                    {
                        mouseButtonState = InputState.Release;
                    }
                    else
                    {
                        mouseButtonState = InputState.FirstRelease;
                    }
                }

                mouseButtonStates[mouseButton] = mouseButtonState;
            }
            else
            {
                mouseButtonStates.Add(mouseButton, mouseButtonState);
            }
        }

        internal static void CursorPosCallback(float xpos, float ypos)
        {
            MousePosition = new Vector2(xpos, ypos);
        }

        internal static void CharCallback(uint codepoint)
        {
            Character = codepoint;
        }

        internal static void KeyCallback(Keys key, int scancode, GLFW.InputState state, ModifierKeys mods)
        {
            KeyCode code = (KeyCode)key;

            bool pressed = state == GLFW.InputState.Press || state == GLFW.InputState.Repeat;

            InputState keyState = pressed ? InputState.FirstPress : InputState.Release;

            if(keyStates.ContainsKey(code))
            {
                keyState = keyStates[code];

                if(pressed)
                {
                    if(keyState == InputState.FirstPress)
                    {
                        keyState = InputState.Press;
                    }
                    else
                    {
                        keyState = InputState.FirstPress;
                    }
                }
                else
                {
                    if (keyState == InputState.FirstRelease)
                    {
                        keyState = InputState.Release;
                    }
                    else
                    {
                        keyState = InputState.FirstRelease;
                    }
                }

                keyStates[code] = keyState;
            }
            else
            {
                keyStates.Add(code, keyState);
            }
        }

        public static bool GetKey(KeyCode key)
        {
            InputState state = InputState.Release;

            return keyStates.TryGetValue(key, out state) && (state == InputState.Press || state == InputState.FirstPress);
        }

        public static bool GetKeyDown(KeyCode key)
        {
            InputState state = InputState.Release;

            return keyStates.TryGetValue(key, out state) && state == InputState.FirstPress;
        }

        public static bool GetKeyUp(KeyCode key)
        {
            InputState state = InputState.Release;

            return keyStates.TryGetValue(key, out state) && state == InputState.FirstRelease;
        }

        public static bool GetMouseButton(MouseButton button)
        {
            InputState state = InputState.Release;

            return mouseButtonStates.TryGetValue(button, out state) && (state == InputState.Press || state == InputState.FirstPress);
        }

        public static bool GetMouseButtonDown(MouseButton button)
        {
            InputState state = InputState.Release;

            return mouseButtonStates.TryGetValue(button, out state) && state == InputState.FirstPress;
        }

        public static bool GetMouseButtonUp(MouseButton button)
        {
            InputState state = InputState.Release;

            return mouseButtonStates.TryGetValue(button, out state) && state == InputState.FirstRelease;
        }

        public static void LockCursor()
        {
            Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Disabled);
        }

        public static void UnlockCursor()
        {
            Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Normal);
        }

        public static void HideCursor()
        {
            Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Hidden);
        }

        public static void ShowCursor()
        {
            Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Normal);
        }
    }
}
