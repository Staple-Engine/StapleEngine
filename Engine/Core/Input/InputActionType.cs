namespace Staple;

/// <summary>
/// The type of actions for input actions
/// </summary>
public enum InputActionType
{
    /// <summary>
    /// Single press on a button, key, or touch.
    /// Will trigger on the frame it happens, not while pressed.
    /// </summary>
    Press,

    /// <summary>
    /// Continuous pressing of a button, key, or touch.
    /// Will trigger as long as it is currently pressed.
    /// </summary>
    ContinousPress,

    /// <summary>
    /// An axis, which will be a float.
    /// This is not guaranteed to be normalized (-1 to 1). For example, mice can be quite fast and we don't want to limit this.
    /// For keys: The first positive and negative keys are used to build the axis. This is normalized.
    /// For mouse: Input is not normalized. Should use scroll, horizontal, or vertical.
    /// For touch: Not supported
    /// For gamepads: The first positive and negative axis are used to build the axis. This is normalized.
    /// </summary>
    Axis,
    /// <summary>
    /// A dual axis, which will be a Vector2.
    /// This is not guaranteed to be normalized (-1 to 1). For example, mice can be quite fast and we don't want to limit this.
    /// For keys: The first positive and negative keys are used to build the X axis. The second positive and negative keys are used to build the Y axis. This is normalized.
    /// For mouse: Input is not normalized. Should use scroll, horizontal, or vertical, at least two of them, or one of the axis will be 0.
    /// For touch: Not supported
    /// For gamepads: The first positive and negative axis are used to build the X axis. The second positive and negative axis are used to build the Y axis. This is normalized.
    /// </summary>
    DualAxis,
}
