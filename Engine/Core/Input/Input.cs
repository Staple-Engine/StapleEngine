using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;

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
        public readonly Dictionary<GamepadButton, InputState> buttonStates = [];
        public readonly Dictionary<GamepadAxis, float> axis = [];
    }

    private class InputCallback
    {
        public InputAction action;
        public Action<InputActionContext> onPress;
        public Action<InputActionContext, float> onAxis;
        public Action<InputActionContext, Vector2> onDualAxis;
        public Assembly assembly;
    }

    private static readonly Dictionary<KeyCode, InputState> keyStates = [];

    private static readonly Dictionary<MouseButton, InputState> mouseButtonStates = [];

    private static readonly Dictionary<int, InputState> touchStates = [];

    private static readonly Dictionary<int, Vector2> touchPositions = [];

    private static readonly HashSet<int> touchKeysToRemove = [];

    private static readonly Dictionary<int, GamepadState> gamepads = [];

    private static readonly Dictionary<int, InputCallback> inputCallbacks = [];

    private static int inputCallbackCounter = 0;

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
        static void ExecuteSafely(Action<InputActionContext> action, InputActionContext context)
        {
            try
            {
                action?.Invoke(context);
            }
            catch(Exception e)
            {
                Log.Debug(e.ToString());
            }
        }

        static void ExecuteSafelyAxis(Action<InputActionContext, float> action, InputActionContext context, float value)
        {
            try
            {
                action?.Invoke(context, value);
            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
            }
        }

        static void ExecuteSafelyDualAxis(Action<InputActionContext, Vector2> action, InputActionContext context, Vector2 value)
        {
            try
            {
                action?.Invoke(context, value);
            }
            catch (Exception e)
            {
                Log.Debug(e.ToString());
            }
        }

        foreach (var pair in inputCallbacks)
        {
            switch(pair.Value.action.type)
            {
                case InputActionType.Press:

                    foreach(var device in pair.Value.action.devices)
                    {
                        switch(device.device)
                        {
                            case InputDevice.Gamepad:

                                if (GetGamepadButtonDown(device.deviceIndex, device.gamepad.button))
                                {
                                    ExecuteSafely(pair.Value.onPress, new()
                                    {
                                        device = device.device,
                                        deviceIndex = device.deviceIndex,
                                    });
                                }

                                break;

                            case InputDevice.Keyboard:

                                if (GetKeyDown(device.keys.firstPositive))
                                {
                                    ExecuteSafely(pair.Value.onPress, new()
                                    {
                                        device = device.device,
                                        deviceIndex = device.deviceIndex,
                                    });
                                }

                                break;

                            case InputDevice.Mouse:

                                if(GetMouseButtonDown(device.mouse.button))
                                {
                                    ExecuteSafely(pair.Value.onPress, new()
                                    {
                                        device = device.device,
                                        deviceIndex = device.deviceIndex,
                                    });
                                }

                                break;

                            case InputDevice.Touch:

                                if(GetTouchDown(device.deviceIndex))
                                {
                                    ExecuteSafely(pair.Value.onPress, new()
                                    {
                                        device = device.device,
                                        deviceIndex = device.deviceIndex,
                                    });
                                }

                                break;
                        }
                    }

                    break;

                case InputActionType.ContinousPress:

                    foreach (var device in pair.Value.action.devices)
                    {
                        switch (device.device)
                        {
                            case InputDevice.Gamepad:

                                if (GetGamepadButton(device.deviceIndex, device.gamepad.button))
                                {
                                    ExecuteSafely(pair.Value.onPress, new()
                                    {
                                        device = device.device,
                                        deviceIndex = device.deviceIndex,
                                    });
                                }

                                break;

                            case InputDevice.Keyboard:

                                if (GetKey(device.keys.firstPositive))
                                {
                                    ExecuteSafely(pair.Value.onPress, new()
                                    {
                                        device = device.device,
                                        deviceIndex = device.deviceIndex,
                                    });
                                }

                                break;

                            case InputDevice.Mouse:

                                if (GetMouseButton(device.mouse.button))
                                {
                                    ExecuteSafely(pair.Value.onPress, new()
                                    {
                                        device = device.device,
                                        deviceIndex = device.deviceIndex,
                                    });
                                }

                                break;

                            case InputDevice.Touch:

                                if (GetTouch(device.deviceIndex))
                                {
                                    ExecuteSafely(pair.Value.onPress, new()
                                    {
                                        device = device.device,
                                        deviceIndex = device.deviceIndex,
                                    });
                                }

                                break;
                        }
                    }

                    break;

                case InputActionType.Axis:

                    foreach (var device in pair.Value.action.devices)
                    {
                        var axis = 0.0f;

                        switch (device.device)
                        {
                            case InputDevice.Gamepad:

                                axis = GetGamepadAxis(device.deviceIndex, device.gamepad.firstAxis);

                                break;

                            case InputDevice.Keyboard:

                                axis += GetKey(device.keys.firstPositive) ? 1 : 0;
                                axis -= GetKey(device.keys.firstNegative) ? 1 : 0;

                                break;

                            case InputDevice.Mouse:

                                if(device.mouse.scroll)
                                {
                                    axis = MouseDelta.Y;
                                }
                                else if(device.mouse.horizontal)
                                {
                                    axis = MouseRelativePosition.X;
                                }
                                else if(device.mouse.vertical)
                                {
                                    axis = -MouseRelativePosition.Y;
                                }

                                break;

                            case InputDevice.Touch:

                                //Not valid

                                break;
                        }

                        if(axis != 0)
                        {
                            ExecuteSafelyAxis(pair.Value.onAxis, new()
                            {
                                device = device.device,
                                deviceIndex = device.deviceIndex,
                            }, axis);
                        }
                    }

                    break;

                case InputActionType.DualAxis:

                    foreach (var device in pair.Value.action.devices)
                    {
                        var axis = Vector2.Zero;

                        switch (device.device)
                        {
                            case InputDevice.Gamepad:

                                axis.X = GetGamepadAxis(device.deviceIndex, device.gamepad.firstAxis);
                                axis.Y = GetGamepadAxis(device.deviceIndex, device.gamepad.secondAxis);

                                break;

                            case InputDevice.Keyboard:

                                axis.X += GetKey(device.keys.firstPositive) ? 1 : 0;
                                axis.X -= GetKey(device.keys.firstNegative) ? 1 : 0;

                                if(device.keys.secondPositive.HasValue && device.keys.secondNegative.HasValue)
                                {
                                    axis.Y += GetKey(device.keys.secondPositive.Value) ? 1 : 0;
                                    axis.Y -= GetKey(device.keys.secondNegative.Value) ? 1 : 0;
                                }

                                break;

                            case InputDevice.Mouse:

                                if (device.mouse.scroll)
                                {
                                    axis.X = MouseDelta.Y;
                                    axis.Y = MouseDelta.X;
                                }
                                else
                                {
                                    if (device.mouse.horizontal)
                                    {
                                        axis.X = MouseRelativePosition.X;
                                    }

                                    if (device.mouse.vertical)
                                    {
                                        axis.Y = -MouseRelativePosition.Y;
                                    }
                                }

                                break;

                            case InputDevice.Touch:

                                //Not valid

                                break;
                        }

                        if (axis != Vector2.Zero)
                        {
                            ExecuteSafelyDualAxis(pair.Value.onDualAxis, new()
                            {
                                device = device.device,
                                deviceIndex = device.deviceIndex,
                            }, axis);
                        }
                    }

                    break;
            }
        }

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

    private static void HandleInputStateChange<Key>(Key key, AppEventInputState state, Dictionary<Key, InputState> states)
    {
        bool pressed = state == AppEventInputState.Press;

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

        if (appEvent.touch.state == AppEventInputState.Repeat)
        {
            return;
        }

        HandleInputStateChange(appEvent.touch.touchID, appEvent.touch.state, touchStates);
    }

    internal static void HandleMouseButtonEvent(AppEvent appEvent)
    {
        var mouseButton = (MouseButton)appEvent.mouse.button;

        HandleInputStateChange(mouseButton, appEvent.type == AppEventType.MouseDown ? AppEventInputState.Press : AppEventInputState.Release,
            mouseButtonStates);
    }

    internal static void HandleKeyEvent(AppEvent appEvent)
    {
        var code = appEvent.key.key;

        HandleInputStateChange(code, appEvent.type == AppEventType.KeyDown ? AppEventInputState.Press : AppEventInputState.Release,
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

        MouseRelativePosition = newPos - previousMousePosition;
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

    /// <summary>
    /// Registers an input action for being pressed
    /// </summary>
    /// <param name="action">The action</param>
    /// <param name="callback">The callback</param>
    /// <returns>An ID for the action to be used for removing the action later</returns>
    public static int AddAction(InputAction action, Action<InputActionContext> callback)
    {
        inputCallbacks.Add(inputCallbackCounter, new InputCallback()
        {
            assembly = callback.Target.GetType().Assembly,
            action = action,
            onPress = callback,
        });

        return inputCallbackCounter++;
    }

    /// <summary>
    /// Registers an input action for a single axis
    /// </summary>
    /// <param name="action">The action</param>
    /// <param name="callback">The callback</param>
    /// <returns>An ID for the action to be used for removing the action later</returns>
    public static int AddAction(InputAction action, Action<InputActionContext, float> callback)
    {
        inputCallbacks.Add(inputCallbackCounter, new InputCallback()
        {
            assembly = callback.Target.GetType().Assembly,
            action = action,
            onAxis = callback,
        });

        return inputCallbackCounter++;
    }

    /// <summary>
    /// Registers an input action for two axis
    /// </summary>
    /// <param name="action">The action</param>
    /// <param name="callback">The callback</param>
    /// <returns>An ID for the action to be used for removing the action later</returns>
    public static int AddAction(InputAction action, Action<InputActionContext, Vector2> callback)
    {
        inputCallbacks.Add(inputCallbackCounter, new InputCallback()
        {
            assembly = callback.Target.GetType().Assembly,
            action = action,
            onDualAxis = callback,
        });

        return inputCallbackCounter++;
    }

    /// <summary>
    /// Clears an input action
    /// </summary>
    /// <param name="ID">The action ID to clear</param>
    public static void ClearAction(int ID)
    {
        inputCallbacks.Remove(ID);
    }

    /// <summary>
    /// Clears all actions belonging to a specific assembly
    /// </summary>
    /// <param name="assembly">The assembly to clear</param>
    internal static void ClearAssemblyActions(Assembly assembly)
    {
        var cleared = new HashSet<int>();

        foreach(var pair in inputCallbacks)
        {
            if(pair.Value.assembly == assembly)
            {
                cleared.Add(pair.Key);
            }
        }

        foreach(var key in cleared)
        {
            inputCallbacks.Remove(key);
        }
    }
}
