﻿namespace Staple;

/// <summary>
/// Contains information on an input action so you can tell what device it's related to.
/// </summary>
public struct InputActionContext
{
    /// <summary>
    /// The name of the action
    /// </summary>
    public string name;

    /// <summary>
    /// The device type
    /// </summary>
    public InputDevice device;

    /// <summary>
    /// The device index (check InputAction for details of how this works)
    /// </summary>
    public int deviceIndex;
}
