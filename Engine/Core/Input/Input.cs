using Staple.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple;

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

    private static readonly Dictionary<int, InputState> touchStates = new();

    private static readonly Dictionary<int, Vector2> touchPositions = new();

    private static readonly HashSet<int> touchKeysToRemove = new();

    /// <summary>
    /// Last input character
    /// </summary>
    public static uint Character { get; private set; }

    /// <summary>
    /// Current mouse position
    /// </summary>
    public static Vector2 MousePosition { get; private set; }

    /// <summary>
    /// Last movement of the mouse
    /// </summary>
    public static Vector2 MouseRelativePosition { get; private set; }

    /// <summary>
    /// Current mouse scroll wheel delta
    /// </summary>
    public static Vector2 MouseDelta { get; private set; }

    internal static Vector2 previousMousePosition;

    internal static IRenderWindow window;

    internal static void UpdateState()
    {
        static void Handle<T>(Dictionary<T, InputState> dict)
        {
            foreach (var key in dict.Keys)
            {
                if (dict[key] == InputState.FirstRelease)
                {
                    dict[key] = InputState.Release;
                }
                else if (dict[key] == InputState.FirstPress)
                {
                    dict[key] = InputState.Press;
                }
            }
        }

        touchKeysToRemove.Clear();

        foreach(var key in touchStates.Keys)
        {
            if (touchStates[key] == InputState.Release)
            {
                touchKeysToRemove.Add(key);
            }
        }

        foreach(var key in touchKeysToRemove)
        {
            touchStates.Remove(key);
            touchPositions.Remove(key);
        }

        Handle(keyStates);
        Handle(mouseButtonStates);
        Handle(touchStates);

        Character = 0;
        MouseDelta = Vector2.Zero;
        MouseRelativePosition = Vector2.Zero;

        previousMousePosition = MousePosition;
    }

    internal static void HandleMouseDeltaEvent(AppEvent appEvent)
    {
        MouseDelta = appEvent.mouseDelta.delta;
    }

    internal static void MouseScrollCallback(float xOffset, float yOffset)
    {
        AppEventQueue.instance.Add(AppEvent.MouseDelta(new Vector2(xOffset, yOffset)));
    }

    internal static void HandleTouchEvent(AppEvent appEvent)
    {
        if (touchPositions.ContainsKey(appEvent.touchEvent.touchID))
        {
            touchPositions[appEvent.touchEvent.touchID] = appEvent.touchEvent.position;
        }
        else
        {
            touchPositions.Add(appEvent.touchEvent.touchID, appEvent.touchEvent.position);
        }

        if (appEvent.touchEvent.state == Internal.InputState.Repeat)
        {
            return;
        }

        bool pressed = appEvent.touchEvent.state == Internal.InputState.Press;

        InputState touchState = pressed ? InputState.FirstPress : InputState.FirstRelease;

        if (touchStates.ContainsKey(appEvent.touchEvent.touchID))
        {
            touchState = touchStates[appEvent.touchEvent.touchID];

            if (pressed)
            {
                if (touchState == InputState.FirstPress)
                {
                    touchState = InputState.Press;
                }
                else
                {
                    touchState = InputState.FirstPress;
                }
            }
            else
            {
                if (touchState == InputState.FirstRelease)
                {
                    touchState = InputState.Release;
                }
                else
                {
                    touchState = InputState.FirstRelease;
                }
            }

            touchStates[appEvent.touchEvent.touchID] = touchState;
        }
        else
        {
            touchStates.Add(appEvent.touchEvent.touchID, touchState);
        }
    }

    internal static void HandleMouseButtonEvent(AppEvent appEvent)
    {
        MouseButton mouseButton = (MouseButton)appEvent.mouse.button;

        bool pressed = appEvent.type == AppEventType.MouseDown;

        InputState mouseButtonState = pressed ? InputState.FirstPress : InputState.FirstRelease;

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
        var code = appEvent.key.key;

        bool pressed = appEvent.type == AppEventType.KeyDown;

        var keyState = pressed ? InputState.FirstPress : InputState.FirstRelease;

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

        MousePosition = newPos;

        MouseRelativePosition = previousMousePosition - newPos;
    }

    internal static void HandleTextEvent(AppEvent appEvent)
    {
        Character = appEvent.character;
    }

    /// <summary>
    /// How many fingers are currently active
    /// </summary>
    public static int TouchCount => touchStates.Count;

    /// <summary>
    /// Gets the pointer ID at a touch index. Used to query the correct pointer ID from an index based off TouchCount
    /// </summary>
    /// <param name="index">the index of the touch</param>
    /// <returns>The Pointer ID associated with the index</returns>
    public static int GetPointerID(int index)
    {
        return touchStates.Keys.Skip(index).FirstOrDefault();
    }

    /// <summary>
    /// Gets the touch position at a specific pointer index
    /// </summary>
    /// <param name="pointerIndex">The pointer index (can get from GetPointerID)</param>
    /// <returns>The position</returns>
    public static Vector2 GetTouchPosition(int pointerIndex)
    {
        return touchPositions.TryGetValue(pointerIndex, out var position) ? position : Vector2.Zero;
    }

    /// <summary>
    /// Check whether a finger is currently pressing
    /// </summary>
    /// <param name="pointerIndex">The pointer index (can get from GetPointerID)</param>
    /// <returns>Whether pressed</returns>
    public static bool GetTouch(int pointerIndex)
    {
        return touchStates.TryGetValue(pointerIndex, out var state) && (state == InputState.Press || state == InputState.FirstPress);
    }

    /// <summary>
    /// Check whether a finger just pressed
    /// </summary>
    /// <param name="pointerIndex">The pointer index (can get from GetPointerID)</param>
    /// <returns>Whether pressed</returns>
    public static bool GetTouchDown(int pointerIndex)
    {
        return touchStates.TryGetValue(pointerIndex, out var state) && state == InputState.FirstPress;
    }

    /// <summary>
    /// Check whether a finger just released
    /// </summary>
    /// <param name="pointerIndex">The pointer index (can get from GetPointerID)</param>
    /// <returns>Whether just released</returns>
    public static bool GetTouchUp(int pointerIndex)
    {
        return touchStates.TryGetValue(pointerIndex, out var state) && state == InputState.FirstRelease;
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
