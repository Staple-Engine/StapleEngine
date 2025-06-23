namespace Staple;

/// <summary>
/// Native API for the window type for a render window
/// </summary>
public enum NativeWindowType
{
    /// <summary>
    /// Other type
    /// </summary>
    Other,

    /// <summary>
    /// Linux X11 window
    /// </summary>
    X11,

    /// <summary>
    /// Linux Wayland window
    /// </summary>
    Wayland,
}
