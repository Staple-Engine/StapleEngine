using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Threading;
using static System.Collections.Specialized.BitVector32;

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

    private class InputObserver
    {
        public IInputObserver observer;
        public Assembly assembly;
    }

    private static readonly Dictionary<KeyCode, InputState> keyStates = [];

    private static readonly Dictionary<MouseButton, InputState> mouseButtonStates = [];

    private static readonly Dictionary<int, InputState> touchStates = [];

    private static readonly Dictionary<int, Vector2> touchPositions = [];

    private static readonly HashSet<int> touchKeysToRemove = [];

    private static readonly Dictionary<int, GamepadState> gamepads = [];

    private static readonly Dictionary<int, InputCallback> inputCallbacks = [];

    private static readonly Dictionary<int, InputObserver> inputObservers = [];

    private static readonly Lock lockObject = new();

    private static int inputCallbackCounter = 0;

    private static int inputObserverCounter = 0;

    /// <summary>
    /// Last input character
    /// </summary>
    public static char Character { get; private set; }

    /// <summary>
    /// Current mouse position
    /// </summary>
    public static Vector2 MousePosition { get; internal set; }

    /// <summary>
    /// Last movement of the mouse
    /// </summary>
    public static Vector2 MouseRelativePosition { get; private set; }

    /// <summary>
    /// Current mouse scroll wheel delta
    /// </summary>
    public static Vector2 MouseDelta { get; private set; }

    /// <summary>
    /// Gets the position of the current pointer input. Use this to not have to manually support <see cref="MousePosition"/> or <see cref="GetTouchPosition(int)"/>
    /// </summary>
    public static Vector2 PointerPosition
    {
        get
        {
            if(Platform.IsMobilePlatform)
            {
                if(TouchCount > 0)
                {
                    return GetTouchPosition(GetPointerID(0));
                }
            }

            return MousePosition;
        }
    }

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

        static bool HandleTouchAxisBehaviour(InputAction.Device device, out Vector2 axis)
        {
            if(GetTouch(device.deviceIndex) == false)
            {
                device.touch.pressing = false;
            }

            if(device.touch.pressing == false)
            {
                for (var i = 0; i < TouchCount; i++)
                {
                    var pointer = GetPointerID(i);

                    if (GetTouchDown(pointer))
                    {
                        var position = GetTouchPosition(pointer);

                        var normalizedPosition = new Vector2(position.X / Screen.Width, position.Y / Screen.Height);

                        device.touch.pressing = device.touch.affectedArea.Contains(normalizedPosition);

                        if (device.touch.pressing)
                        {
                            device.deviceIndex = pointer;

                            device.touch.lastPosition = position;
                        }

                        break;
                    }
                }
            }

            if (device.touch.pressing && GetTouch(device.deviceIndex))
            {
                var current = GetTouchPosition(device.deviceIndex);

                var difference = current - device.touch.lastPosition;

                if(difference == Vector2.Zero)
                {
                    axis = difference;

                    return true;
                }

                var direction = Vector2.Normalize(difference);

                direction.Y *= -1;

                if(Math.Abs(direction.X) < 0.5f)
                {
                    direction.X = 0;
                }

                if (Math.Abs(direction.Y) < 0.5f)
                {
                    direction.Y = 0;
                }

                axis = direction;

                return true;
            }

            axis = default;

            return false;
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
                                        name = pair.Value.action.name,
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
                                        name = pair.Value.action.name,
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
                                        name = pair.Value.action.name,
                                        device = device.device,
                                        deviceIndex = device.deviceIndex,
                                    });
                                }

                                break;

                            case InputDevice.Touch:

                                for(var i = 0; i < TouchCount; i++)
                                {
                                    if (GetTouchDown(GetPointerID(i)))
                                    {
                                        ExecuteSafely(pair.Value.onPress, new()
                                        {
                                            name = pair.Value.action.name,
                                            device = device.device,
                                            deviceIndex = i,
                                        });

                                        break;
                                    }
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
                                        name = pair.Value.action.name,
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
                                        name = pair.Value.action.name,
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
                                        name = pair.Value.action.name,
                                        device = device.device,
                                        deviceIndex = device.deviceIndex,
                                    });
                                }

                                break;

                            case InputDevice.Touch:

                                for (var i = 0; i < TouchCount; i++)
                                {
                                    if (GetTouch(GetPointerID(i)))
                                    {
                                        ExecuteSafely(pair.Value.onPress, new()
                                        {
                                            name = pair.Value.action.name,
                                            device = device.device,
                                            deviceIndex = i,
                                        });

                                        break;
                                    }
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

                                if(HandleTouchAxisBehaviour(device, out var touchAxis))
                                {
                                    if (device.touch.horizontal)
                                    {
                                        axis = touchAxis.X;
                                    }
                                    else if (device.touch.vertical)
                                    {
                                        axis = touchAxis.Y;
                                    }
                                }

                                break;
                        }

                        if(axis != 0)
                        {
                            ExecuteSafelyAxis(pair.Value.onAxis, new()
                            {
                                name = pair.Value.action.name,
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

                                axis.Y += GetKey(device.keys.secondPositive) ? 1 : 0;
                                axis.Y -= GetKey(device.keys.secondNegative) ? 1 : 0;

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

                                if (HandleTouchAxisBehaviour(device, out var touchAxis))
                                {
                                    axis = touchAxis;
                                }

                                break;
                        }

                        if (axis != Vector2.Zero)
                        {
                            ExecuteSafelyDualAxis(pair.Value.onDualAxis, new()
                            {
                                name = pair.Value.action.name,
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

        lock(lockObject)
        {
            foreach(var pair in inputObservers)
            {
                //TODO: Gamepad and Touch
                foreach (var statePair in keyStates)
                {
                    switch (statePair.Value)
                    {
                        case InputState.Press:

                            pair.Value.observer.OnKeyPressed(statePair.Key);

                            break;

                        case InputState.FirstPress:

                            pair.Value.observer.OnKeyJustPressed(statePair.Key);

                            break;

                        case InputState.FirstRelease:

                            pair.Value.observer.OnKeyReleased(statePair.Key);

                            break;
                    }
                }

                foreach (var statePair in mouseButtonStates)
                {
                    switch (statePair.Value)
                    {
                        case InputState.Press:

                            pair.Value.observer.OnMouseButtonPressed(statePair.Key);

                            break;

                        case InputState.FirstPress:

                            pair.Value.observer.OnMouseButtonJustPressed(statePair.Key);

                            break;

                        case InputState.FirstRelease:

                            pair.Value.observer.OnMouseButtonReleased(statePair.Key);

                            break;
                    }
                }
            }

            touchKeysToRemove.Clear();

            foreach (var key in touchStates.Keys)
            {
                if (touchStates[key] == InputState.Release)
                {
                    touchKeysToRemove.Add(key);
                }
            }

            foreach (var key in touchKeysToRemove)
            {
                touchStates.Remove(key);
                touchPositions.Remove(key);
            }

            Handle(keyStates);
            Handle(mouseButtonStates);
            Handle(touchStates);

            Character = (char)0;
            MouseDelta = Vector2.Zero;
            MouseRelativePosition = Vector2.Zero;

            previousMousePosition = MousePosition;
        }
    }

    private static void HandleInputStateChange<Key>(Key key, AppEventInputState state, Dictionary<Key, InputState> states)
    {
        lock (lockObject)
        {
            bool pressed = state == AppEventInputState.Press;

            var buttonState = pressed ? InputState.FirstPress : InputState.FirstRelease;

            if (states.TryGetValue(key, out InputState value))
            {
                buttonState = value;

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
    }

    internal static void GamepadConnect(AppEvent appEvent)
    {
        lock (lockObject)
        {
            if (gamepads.TryGetValue(appEvent.gamepadConnect.index, out var gamepad) == false)
            {
                gamepad = new();

                gamepads.Add(appEvent.gamepadConnect.index, gamepad);
            }

            gamepad.state = appEvent.gamepadConnect.state;

            gamepad.axis.Clear();
        }
    }

    internal static void GamepadButton(AppEvent appEvent)
    {
        lock (lockObject)
        {
            if (gamepads.TryGetValue(appEvent.gamepadButton.index, out var gamepad) == false)
            {
                return;
            }

            HandleInputStateChange(appEvent.gamepadButton.button, appEvent.gamepadButton.state, gamepad.buttonStates);
        }
    }

    internal static void GamepadMovement(AppEvent appEvent)
    {
        lock (lockObject)
        {
            if (gamepads.TryGetValue(appEvent.gamepadButton.index, out var gamepad) == false)
            {
                return;
            }

            gamepad.axis.AddOrSetKey(appEvent.gamepadMovement.axis, appEvent.gamepadMovement.movement);
        }
    }

    internal static void HandleMouseDeltaEvent(AppEvent appEvent)
    {
        lock (lockObject)
        {
            MouseDelta = appEvent.mouseDelta.delta;

            foreach(var pair in inputObservers)
            {
                pair.Value.observer.OnMouseWheelScrolled(MouseDelta);
            }
        }
    }

    internal static void MouseScrollCallback(float xOffset, float yOffset)
    {
        lock (lockObject)
        {
            AppEventQueue.instance.Add(AppEvent.MouseDelta(new Vector2(xOffset, yOffset)));
        }
    }

    internal static void HandleTouchEvent(AppEvent appEvent)
    {
        lock (lockObject)
        {
            touchPositions[appEvent.touch.touchID] = appEvent.touch.position;

            if (appEvent.touch.state == AppEventInputState.Repeat)
            {
                return;
            }

            HandleInputStateChange(appEvent.touch.touchID, appEvent.touch.state, touchStates);
        }
    }

    internal static void HandleMouseButtonEvent(AppEvent appEvent)
    {
        lock (lockObject)
        {
            var mouseButton = (MouseButton)appEvent.mouse.button;

            HandleInputStateChange(mouseButton, appEvent.type == AppEventType.MouseDown ? AppEventInputState.Press : AppEventInputState.Release,
                mouseButtonStates);
        }
    }

    internal static void HandleKeyEvent(AppEvent appEvent)
    {
        lock (lockObject)
        {
            var code = appEvent.key.key;

            HandleInputStateChange(code, appEvent.type == AppEventType.KeyDown ? AppEventInputState.Press : AppEventInputState.Release,
                keyStates);
        }
    }

    internal static void CursorPosCallback(float xpos, float ypos)
    {
        lock (lockObject)
        {
            var newPos = new Vector2(xpos, ypos);

            if (Cursor.LockState == CursorLockMode.Locked)
            {
                MouseRelativePosition = newPos;
            }
            else
            {
                if (MousePosition == Vector2.Zero)
                {
                    previousMousePosition = newPos;
                }

                MousePosition = newPos;

                MouseRelativePosition = newPos - previousMousePosition;

                foreach(var pair in inputObservers)
                {
                    pair.Value.observer.OnMouseMove(newPos);
                }
            }
        }
    }

    internal static void HandleTextEvent(AppEvent appEvent)
    {
        lock (lockObject)
        {
            Character = (char)appEvent.character;

            foreach(var pair in inputObservers)
            {
                pair.Value.observer.OnCharacterEntered(Character);
            }
        }
    }

    /// <summary>
    /// How many fingers are currently active
    /// </summary>
    public static int TouchCount
    {
        get
        {
            lock (lockObject)
            {
                return touchStates.Count;
            }
        }
    }

    /// <summary>
    /// Gets the pointer ID at a touch index. Used to query the correct pointer ID from an index based off TouchCount
    /// </summary>
    /// <param name="index">the index of the touch</param>
    /// <returns>The Pointer ID associated with the index</returns>
    public static int GetPointerID(int index)
    {
        lock (lockObject)
        {
            return touchStates.Keys.Skip(index).FirstOrDefault();
        }
    }

    /// <summary>
    /// Gets the touch position at a specific pointer index
    /// </summary>
    /// <param name="pointerIndex">The pointer index (can get from GetPointerID)</param>
    /// <returns>The position</returns>
    public static Vector2 GetTouchPosition(int pointerIndex)
    {
        lock (lockObject)
        {
            return touchPositions.TryGetValue(pointerIndex, out var position) ? position : Vector2.Zero;
        }
    }

    /// <summary>
    /// Check whether a finger is currently pressing
    /// </summary>
    /// <param name="pointerIndex">The pointer index (can get from GetPointerID)</param>
    /// <returns>Whether pressed</returns>
    public static bool GetTouch(int pointerIndex)
    {
        lock (lockObject)
        {
            return touchStates.TryGetValue(pointerIndex, out var state) && (state == InputState.Press || state == InputState.FirstPress);
        }
    }

    /// <summary>
    /// Check whether a finger just pressed
    /// </summary>
    /// <param name="pointerIndex">The pointer index (can get from GetPointerID)</param>
    /// <returns>Whether pressed</returns>
    public static bool GetTouchDown(int pointerIndex)
    {
        lock (lockObject)
        {
            return touchStates.TryGetValue(pointerIndex, out var state) && state == InputState.FirstPress;
        }
    }

    /// <summary>
    /// Check whether a finger just released
    /// </summary>
    /// <param name="pointerIndex">The pointer index (can get from GetPointerID)</param>
    /// <returns>Whether just released</returns>
    public static bool GetTouchUp(int pointerIndex)
    {
        lock (lockObject)
        {
            return touchStates.TryGetValue(pointerIndex, out var state) && state == InputState.FirstRelease;
        }
    }

    /// <summary>
    /// Check whether a key is currently pressed
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>Whether the key was pressed</returns>
    public static bool GetKey(KeyCode key)
    {
        lock (lockObject)
        {
            return keyStates.TryGetValue(key, out var state) && (state == InputState.Press || state == InputState.FirstPress);
        }
    }

    /// <summary>
    /// Check whether a key was just pressed
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>Whether the key was just pressed</returns>
    public static bool GetKeyDown(KeyCode key)
    {
        lock (lockObject)
        {
            return keyStates.TryGetValue(key, out var state) && state == InputState.FirstPress;
        }
    }

    /// <summary>
    /// Check whether a key was just released
    /// </summary>
    /// <param name="key">The key</param>
    /// <returns>Whether the key was just released</returns>
    public static bool GetKeyUp(KeyCode key)
    {
        lock (lockObject)
        {
            return keyStates.TryGetValue(key, out var state) && state == InputState.FirstRelease;
        }
    }

    /// <summary>
    /// Check whether a mouse button is currently pressed
    /// </summary>
    /// <param name="button">The mouse button</param>
    /// <returns>Whether the key was pressed</returns>
    public static bool GetMouseButton(MouseButton button)
    {
        lock (lockObject)
        {
            return mouseButtonStates.TryGetValue(button, out var state) && (state == InputState.Press || state == InputState.FirstPress);
        }
    }

    /// <summary>
    /// Check whether a mouse button was just pressed
    /// </summary>
    /// <param name="button">The mouse button</param>
    /// <returns>Whether the button was just pressed</returns>
    public static bool GetMouseButtonDown(MouseButton button)
    {
        lock (lockObject)
        {
            return mouseButtonStates.TryGetValue(button, out var state) && state == InputState.FirstPress;
        }
    }

    /// <summary>
    /// Check whether a mouse button was just released
    /// </summary>
    /// <param name="button">The mouse button</param>
    /// <returns>Whether the button was just released</returns>
    public static bool GetMouseButtonUp(MouseButton button)
    {
        lock (lockObject)
        {
            return mouseButtonStates.TryGetValue(button, out var state) && state == InputState.FirstRelease;
        }
    }

    /// <summary>
    /// Gets how many gamepads are currently usable
    /// </summary>
    /// <returns>How many gamepads are connected</returns>
    public static int GetGamepadCount()
    {
        lock (lockObject)
        {
            return gamepads.Count;
        }
    }

    /// <summary>
    /// Checks whether a gamepad is usable
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <returns>Whether the gamepad is available for use</returns>
    public static bool IsGamepadAvailable(int index)
    {
        lock (lockObject)
        {
            return gamepads.TryGetValue(index, out var gamepad) && gamepad.state == GamepadConnectionState.Connected;
        }
    }

    /// <summary>
    /// Checks whether a gamepad button is pressed
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <param name="button">The button to check</param>
    /// <returns>Whether it is currently being pressed</returns>
    public static bool GetGamepadButton(int index, GamepadButton button)
    {
        lock (lockObject)
        {
            if (gamepads.TryGetValue(index, out var gamepad) == false ||
                gamepad.state == GamepadConnectionState.Disconnected ||
                gamepad.buttonStates.TryGetValue(button, out var state) == false)
            {
                return false;
            }

            return state == InputState.Press || state == InputState.FirstPress;
        }
    }

    /// <summary>
    /// Checks whether a gamepad button was just pressed
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <param name="button">The button to check</param>
    /// <returns>Whether it was just pressed</returns>
    public static bool GetGamepadButtonDown(int index, GamepadButton button)
    {
        lock (lockObject)
        {
            if (gamepads.TryGetValue(index, out var gamepad) == false ||
                gamepad.state == GamepadConnectionState.Disconnected ||
                gamepad.buttonStates.TryGetValue(button, out var state) == false)
            {
                return false;
            }

            return state == InputState.FirstPress;
        }
    }

    /// <summary>
    /// Checks whether a gamepad button was just released
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <param name="button">The button to check</param>
    /// <returns>Whether it was just released</returns>
    public static bool GetGamepadButtonUp(int index, GamepadButton button)
    {
        lock (lockObject)
        {
            if (gamepads.TryGetValue(index, out var gamepad) == false ||
                gamepad.state == GamepadConnectionState.Disconnected ||
                gamepad.buttonStates.TryGetValue(button, out var state) == false)
            {
                return false;
            }

            return state == InputState.FirstRelease;
        }
    }

    /// <summary>
    /// Gets a gamepad's axis movement
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <param name="axis">The axis to check</param>
    /// <returns>The axis movement</returns>
    public static float GetGamepadAxis(int index, GamepadAxis axis)
    {
        lock (lockObject)
        {
            if (gamepads.TryGetValue(index, out var gamepad) == false ||
                gamepad.state == GamepadConnectionState.Disconnected ||
                gamepad.axis.TryGetValue(axis, out var state) == false)
            {
                return 0;
            }

            return state;
        }
    }

    /// <summary>
    /// Gets the left thumbstick movement for a gamepad
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <returns>The movement</returns>
    public static Vector2 GetGamepadLeftAxis(int index)
    {
        lock (lockObject)
        {
            return new Vector2(GetGamepadAxis(index, GamepadAxis.LeftX),
                GetGamepadAxis(index, GamepadAxis.LeftY));
        }
    }

    /// <summary>
    /// Gets the right thumbstick movement for a gamepad
    /// </summary>
    /// <param name="index">The gamepad index</param>
    /// <returns>The movement</returns>
    public static Vector2 GetGamepadRightAxis(int index)
    {
        lock (lockObject)
        {
            return new Vector2(GetGamepadAxis(index, GamepadAxis.RightX),
                GetGamepadAxis(index, GamepadAxis.RightY));
        }
    }

    /// <summary>
    /// Registers an input action for being pressed
    /// </summary>
    /// <param name="action">The action</param>
    /// <param name="callback">The callback</param>
    /// <returns>An ID for the action to be used for removing the action later</returns>
    public static int AddPressedAction(InputAction action, Action<InputActionContext> callback)
    {
        lock (lockObject)
        {
            inputCallbacks.Add(inputCallbackCounter, new InputCallback()
            {
                assembly = callback.Target.GetType().Assembly,
                action = action,
                onPress = callback,
            });

            return inputCallbackCounter++;
        }
    }

    /// <summary>
    /// Registers an input action for a single axis
    /// </summary>
    /// <param name="action">The action</param>
    /// <param name="callback">The callback</param>
    /// <returns>An ID for the action to be used for removing the action later</returns>
    public static int AddSingleAxisAction(InputAction action, Action<InputActionContext, float> callback)
    {
        lock (lockObject)
        {
            inputCallbacks.Add(inputCallbackCounter, new InputCallback()
            {
                assembly = callback.Target.GetType().Assembly,
                action = action,
                onAxis = callback,
            });

            return inputCallbackCounter++;
        }
    }

    /// <summary>
    /// Registers an input action for two axis
    /// </summary>
    /// <param name="action">The action</param>
    /// <param name="callback">The callback</param>
    /// <returns>An ID for the action to be used for removing the action later</returns>
    public static int AddDualAxisAction(InputAction action, Action<InputActionContext, Vector2> callback)
    {
        lock (lockObject)
        {
            inputCallbacks.Add(inputCallbackCounter, new InputCallback()
            {
                assembly = callback.Target.GetType().Assembly,
                action = action,
                onDualAxis = callback,
            });

            return inputCallbackCounter++;
        }
    }

    /// <summary>
    /// Clears an input action
    /// </summary>
    /// <param name="ID">The action ID to clear</param>
    public static void ClearAction(int ID)
    {
        lock (lockObject)
        {
            inputCallbacks.Remove(ID);
        }
    }

    /// <summary>
    /// Clears all actions belonging to a specific assembly
    /// </summary>
    /// <param name="assembly">The assembly to clear</param>
    internal static void ClearAssemblyActions(Assembly assembly)
    {
        lock (lockObject)
        {
            var cleared = new HashSet<int>();

            foreach (var pair in inputCallbacks)
            {
                if (pair.Value.assembly == assembly)
                {
                    cleared.Add(pair.Key);
                }
            }

            foreach (var key in cleared)
            {
                inputCallbacks.Remove(key);
            }
        }
    }

    /// <summary>
    /// Registers an input observer to get input events automatically
    /// </summary>
    /// <param name="observer">The input observer</param>
    /// <returns>An ID to unregister the observer later</returns>
    internal static int RegisterInputObserver(IInputObserver observer)
    {
        if(observer == null)
        {
            return -1;
        }

        lock (lockObject)
        {
            inputObservers.Add(inputObserverCounter, new()
            {
                assembly = observer.GetType().Assembly,
                observer = observer,
            });

            return inputObserverCounter++;
        }
    }

    /// <summary>
    /// Unregisters an input observer to no longer receive input events
    /// </summary>
    /// <param name="ID">The observer ID</param>
    internal static void UnregisterInputObserver(int ID)
    {
        lock(lockObject)
        {
            inputObservers.Remove(ID);
        }
    }

    /// <summary>
    /// Clears all observers belonging to a specific assembly
    /// </summary>
    /// <param name="assembly">The assembly to clear</param>
    internal static void ClearAssemblyObservers(Assembly assembly)
    {
        lock (lockObject)
        {
            var cleared = new HashSet<int>();

            foreach (var pair in inputObservers)
            {
                if (pair.Value.assembly == assembly)
                {
                    cleared.Add(pair.Key);
                }
            }

            foreach (var key in cleared)
            {
                inputObservers.Remove(key);
            }
        }
    }
}
