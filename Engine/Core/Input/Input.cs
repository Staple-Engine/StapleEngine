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

    private class GamepadState
    {
        public GamepadConnectionState state = GamepadConnectionState.Unknown;
        public readonly Dictionary<GamepadButton, InputState> buttonStates = new();
        public readonly Dictionary<GamepadAxis, float> axis = new();
    }

    private static readonly Dictionary<KeyCode, InputState> keyStates = new();

    private static readonly Dictionary<MouseButton, InputState> mouseButtonStates = new();

    private static readonly Dictionary<int, InputState> touchStates = new();

    private static readonly Dictionary<int, Vector2> touchPositions = new();

    private static readonly HashSet<int> touchKeysToRemove = new();

    private static readonly Dictionary<int, GamepadState> gamepads = new();

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

    private static void HandleInputStateChange<Key>(Key key, Internal.AppEventInputState state, Dictionary<Key, InputState> states)
    {
        bool pressed = state == Internal.AppEventInputState.Press;

        var buttonState = pressed ? InputState.FirstPress : InputState.FirstRelease;

        if (states.ContainsKey(key))
        {
            buttonState = states[key];

            if (pressed)
            {
                if (buttonState == InputState.FirstPress)
                {
                    buttonState = InputState.Press;
                }
                else
                {
                    buttonState = InputState.FirstPress;
                }
            }
            else
            {
                if (buttonState == InputState.FirstRelease)
                {
                    buttonState = InputState.Release;
                }
                else
                {
                    buttonState = InputState.FirstRelease;
                }
            }

            states[key] = buttonState;
        }
        else
        {
            states.Add(key, buttonState);
        }
    }

    internal static void GamepadConnect(AppEvent appEvent)
    {
        if(gamepads.TryGetValue(appEvent.gamepadConnect.index, out var gamepad) == false)
        {
            gamepad = new();

            gamepads.Add(appEvent.gamepadConnect.index, gamepad);
        }

        gamepad.state = appEvent.gamepadConnect.state;

        gamepad.axis.Clear();
    }

    internal static void GamepadButton(AppEvent appEvent)
    {
        if(gamepads.TryGetValue(appEvent.gamepadButton.index, out var gamepad) == false)
        {
            return;
        }

        HandleInputStateChange(appEvent.gamepadButton.button, appEvent.gamepadButton.state, gamepad.buttonStates);
    }

    internal static void GamepadMovement(AppEvent appEvent)
    {
        if (gamepads.TryGetValue(appEvent.gamepadButton.index, out var gamepad) == false)
        {
            return;
        }

        gamepad.axis.AddOrSetKey(appEvent.gamepadMovement.axis, appEvent.gamepadMovement.movement);
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
        if (touchPositions.ContainsKey(appEvent.touch.touchID))
        {
            touchPositions[appEvent.touch.touchID] = appEvent.touch.position;
        }
        else
        {
            touchPositions.Add(appEvent.touch.touchID, appEvent.touch.position);
        }

        if (appEvent.touch.state == Internal.AppEventInputState.Repeat)
        {
            return;
        }

        HandleInputStateChange(appEvent.touch.touchID, appEvent.touch.state, touchStates);
    }

    internal static void HandleMouseButtonEvent(AppEvent appEvent)
    {
        var mouseButton = (MouseButton)appEvent.mouse.button;

        HandleInputStateChange(mouseButton, appEvent.type == AppEventType.MouseDown ? Internal.AppEventInputState.Press : Internal.AppEventInputState.Release,
            mouseButtonStates);
    }

    internal static void HandleKeyEvent(AppEvent appEvent)
    {
        var code = appEvent.key.key;

        HandleInputStateChange(code, appEvent.type == AppEventType.KeyDown ? Internal.AppEventInputState.Press : Internal.AppEventInputState.Release,
            keyStates);
    }

    internal static void CursorPosCallback(float xpos, float ypos)
    {
        var newPos = new Vector2(xpos, ypos);

        if(MousePosition == Vector2.Zero)
        {
            previousMousePosition = newPos;
        }

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
    /// Gets how many gamepads are currently usable
    /// </summary>
    /// <returns>How many gamepads are connected</returns>
    public static int GetGamepadCount()
    {
        return gamepads.Count;
    }

    /// <summary>
    /// Checks whether a gamepad is usable
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <returns>Whether the gamepad is available for use</returns>
    public static bool IsGamepadAvailable(int index)
    {
        return gamepads.TryGetValue(index, out var gamepad) && gamepad.state == GamepadConnectionState.Connected;
    }

    /// <summary>
    /// Checks whether a gamepad button is pressed
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <param name="button">The button to check</param>
    /// <returns>Whether it is currently being pressed</returns>
    public static bool GetGamepadButton(int index, GamepadButton button)
    {
        if(gamepads.TryGetValue(index, out var gamepad) == false ||
            gamepad.state == GamepadConnectionState.Disconnected ||
            gamepad.buttonStates.TryGetValue(button, out var state) == false)
        {
            return false;
        }

        return state == InputState.Press || state == InputState.FirstPress;
    }

    /// <summary>
    /// Checks whether a gamepad button was just pressed
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <param name="button">The button to check</param>
    /// <returns>Whether it was just pressed</returns>
    public static bool GetGamepadButtonDown(int index, GamepadButton button)
    {
        if (gamepads.TryGetValue(index, out var gamepad) == false ||
            gamepad.state == GamepadConnectionState.Disconnected ||
            gamepad.buttonStates.TryGetValue(button, out var state) == false)
        {
            return false;
        }

        return state == InputState.FirstPress;
    }

    /// <summary>
    /// Checks whether a gamepad button was just released
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <param name="button">The button to check</param>
    /// <returns>Whether it was just released</returns>
    public static bool GetGamepadButtonUp(int index, GamepadButton button)
    {
        if (gamepads.TryGetValue(index, out var gamepad) == false ||
            gamepad.state == GamepadConnectionState.Disconnected ||
            gamepad.buttonStates.TryGetValue(button, out var state) == false)
        {
            return false;
        }

        return state == InputState.FirstRelease;
    }

    /// <summary>
    /// Gets a gamepad's axis movement
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <param name="axis">The axis to check</param>
    /// <returns>The axis movement</returns>
    public static float GetGamepadAxis(int index, GamepadAxis axis)
    {
        if (gamepads.TryGetValue(index, out var gamepad) == false ||
            gamepad.state == GamepadConnectionState.Disconnected ||
            gamepad.axis.TryGetValue(axis, out var state) == false)
        {
            return 0;
        }

        return state;
    }

    /// <summary>
    /// Gets the left thumbstick movement for a gamepad
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <returns>The movement</returns>
    public static Vector2 GetGamepadLeftAxis(int index)
    {
        return new Vector2(GetGamepadAxis(index, GamepadAxis.LeftX),
            GetGamepadAxis(index, GamepadAxis.LeftY));
    }

    /// <summary>
    /// Gets the right thumbstick movement for a gamepad
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <returns>The movement</returns>
    public static Vector2 GetGamepadRightAxis(int index)
    {
        return new Vector2(GetGamepadAxis(index, GamepadAxis.RightX),
            GetGamepadAxis(index, GamepadAxis.RightY));
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
