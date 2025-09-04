using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

/// <summary>
/// Contains information of how an input action can be handled, for each device it supports.
/// </summary>
[Serializable]
public sealed class InputAction
{
    /// <summary>
    /// Keyboard parameters
    /// </summary>
    [Serializable]
    public sealed class Keys
    {
        /// <summary>
        /// Key that acts as a positive value if this is used for an axis, otherwise it's just the key field for a key press.
        /// </summary>
        public KeyCode firstPositive;

        /// <summary>
        /// Key that acts as a negative value if this is used for an axis.
        /// </summary>
        public KeyCode firstNegative;

        /// <summary>
        /// Key that acts as a positive value if this is used for two axis. Defaults to null.
        /// </summary>
        public KeyCode secondPositive;

        /// <summary>
        /// Key that acts as a negative value if this is used for two axis. Defaults to null.
        /// </summary>
        public KeyCode secondNegative;

        /// <summary>
        /// The key associated with this if used for a press event
        /// </summary>
        public KeyCode Key
        {
            get => firstPositive;

            set => firstPositive = value;
        }
    }

    /// <summary>
    /// Gamepad parameters
    /// </summary>
    [Serializable]
    public sealed class Gamepad
    {
        /// <summary>
        /// Button to check if this is used for a press event
        /// </summary>
        public GamepadButton button;

        /// <summary>
        /// Axis used for an axis event
        /// </summary>
        public GamepadAxis firstAxis;

        /// <summary>
        /// Second axis used for a dual axis event
        /// </summary>
        public GamepadAxis secondAxis;
    }

    /// <summary>
    /// Mouse parameters
    /// </summary>
    [Serializable]
    public sealed class Mouse
    {
        /// <summary>
        /// Button to check if this is a press event
        /// </summary>
        public MouseButton button;

        /// <summary>
        /// Whether we want to check the scroll wheel
        /// </summary>
        public bool scroll;

        /// <summary>
        /// Whether we want to check for horizontal movement
        /// </summary>
        public bool horizontal;

        /// <summary>
        /// Whether we want to check for vertical movement
        /// </summary>
        public bool vertical;
    }

    /// <summary>
    /// Touch parameters
    /// </summary>
    [Serializable]
    public sealed class Touch
    {
        /// <summary>
        /// Affected area in viewport space (0-1) where a touch is valid for an axis
        /// </summary>
        public RectFloat affectedArea;

        /// <summary>
        /// Whether we want to check for horizontal movement for an axis
        /// </summary>
        public bool horizontal;

        /// <summary>
        /// Whether we want to check for vertical movement for an axis
        /// </summary>
        public bool vertical;

        /// <summary>
        /// Whether we started pressing in the affected area
        /// </summary>
        internal bool pressing;

        /// <summary>
        /// The last position in the affected area
        /// </summary>
        internal Vector2 lastPosition;
    }

    /// <summary>
    /// Details for a specific device's settings
    /// </summary>
    [Serializable]
    public sealed class Device
    {
        /// <summary>
        /// The kind of device
        /// </summary>
        public InputDevice device;

        /// <summary>
        /// The device index.
        /// In the case of keyboard and mouse, this is ignored.
        /// In the case of Touch, this is the finger index.
        /// In the case of gamepad, this is the gamepad index.
        /// </summary>
        public int deviceIndex;

        public Keys keys = null;
        public Gamepad gamepad = null;
        public Mouse mouse = null;
        public Touch touch = null;

        public bool IsValid => device switch
        {
            InputDevice.Keyboard => keys != null,
            InputDevice.Gamepad => gamepad != null,
            InputDevice.Mouse => mouse != null,
            InputDevice.Touch => touch != null,
            _ => false,
        };
    }

    /// <summary>
    /// The name of the action
    /// </summary>
    public string name;

    /// <summary>
    /// Type of action
    /// </summary>
    public InputActionType type;

    /// <summary>
    /// Devices that can handle this action
    /// </summary>
    public List<Device> devices = [];
}
