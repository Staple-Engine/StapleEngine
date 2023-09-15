using Staple.Internal;
using System.Collections.Generic;
using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Query input state
    /// </summary>
    public static class Input
    {
        private enum InputState
        {
            Press,
            FirstPress,
            Release,
            FirstRelease
        }

        private static readonly Dictionary<KeyCode, InputState> keyStates = new();

        private static readonly Dictionary<MouseButton, InputState> mouseButtonStates = new();

        /// <summary>
        /// Last input character
        /// </summary>
        public static uint Character { get; internal set; }

        /// <summary>
        /// Current mouse position
        /// </summary>
        public static Vector2 MousePosition { get; private set; }

        /// <summary>
        /// Last movement of the mouse
        /// </summary>
        public static Vector2 MouseRelativePosition { get; internal set; }

        /// <summary>
        /// Current mouse scroll wheel delta
        /// </summary>
        public static Vector2 MouseDelta { get; internal set; }

        internal static IRenderWindow window;

        internal static void HandleMouseDeltaEvent(AppEvent appEvent)
        {
            MouseDelta = appEvent.mouseDelta;
        }

        internal static void MouseScrollCallback(float xOffset, float yOffset)
        {
            AppEventQueue.instance.Add(AppEvent.MouseDelta(new Vector2(xOffset, yOffset)));
        }

        internal static void HandleMouseButtonEvent(AppEvent appEvent)
        {
            MouseButton mouseButton = (MouseButton)appEvent.mouse.button;

            bool pressed = appEvent.type == AppEventType.MouseDown;

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

        internal static void HandleKeyEvent(AppEvent appEvent)
        {
            KeyCode code = (KeyCode)appEvent.key.key;

            bool pressed = appEvent.type == AppEventType.KeyDown;

            InputState keyState = pressed ? InputState.FirstPress : InputState.Release;

            if (keyStates.ContainsKey(code))
            {
                keyState = keyStates[code];

                if (pressed)
                {
                    if (keyState == InputState.FirstPress)
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

        internal static void CursorPosCallback(float xpos, float ypos)
        {
            var newPos = new Vector2(xpos, ypos);

            MouseRelativePosition = newPos - MousePosition;

            MousePosition = newPos;
        }

        internal static void HandleTextEvent(AppEvent appEvent)
        {
            Character = appEvent.character;
        }

        /// <summary>
        /// Check whether a key is currently pressed
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>Whether the key was pressed</returns>
        public static bool GetKey(KeyCode key)
        {
            return keyStates.TryGetValue(key, out var state) && (state == InputState.Press || state == InputState.FirstPress);
        }

        /// <summary>
        /// Check whether a key was just pressed
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>Whether the key was just pressed</returns>
        public static bool GetKeyDown(KeyCode key)
        {
            return keyStates.TryGetValue(key, out var state) && state == InputState.FirstPress;
        }

        /// <summary>
        /// Check whether a key was just released
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>Whether the key was just released</returns>
        public static bool GetKeyUp(KeyCode key)
        {
            return keyStates.TryGetValue(key, out var state) && state == InputState.FirstRelease;
        }

        /// <summary>
        /// Check whether a mouse button is currently pressed
        /// </summary>
        /// <param name="button">The mouse button</param>
        /// <returns>Whether the key was pressed</returns>
        public static bool GetMouseButton(MouseButton button)
        {
            return mouseButtonStates.TryGetValue(button, out var state) && (state == InputState.Press || state == InputState.FirstPress);
        }

        /// <summary>
        /// Check whether a mouse button was just pressed
        /// </summary>
        /// <param name="button">The mouse button</param>
        /// <returns>Whether the button was just pressed</returns>
        public static bool GetMouseButtonDown(MouseButton button)
        {
            return mouseButtonStates.TryGetValue(button, out var state) && state == InputState.FirstPress;
        }

        /// <summary>
        /// Check whether a mouse button was just released
        /// </summary>
        /// <param name="button">The mouse button</param>
        /// <returns>Whether the button was just released</returns>
        public static bool GetMouseButtonUp(MouseButton button)
        {
            return mouseButtonStates.TryGetValue(button, out var state) && state == InputState.FirstRelease;
        }

        /// <summary>
        /// Locks the cursor to the window
        /// </summary>
        public static void LockCursor()
        {
            window.LockCursor();
        }

        /// <summary>
        /// Unlocks the cursor
        /// </summary>
        public static void UnlockCursor()
        {
            window.UnlockCursor();
        }

        /// <summary>
        /// Hides the cursor
        /// </summary>
        public static void HideCursor()
        {
            window.HideCursor();
        }

        /// <summary>
        /// Shows the cursor
        /// </summary>
        public static void ShowCursor()
        {
            window.ShowCursor();
        }
    }
}
