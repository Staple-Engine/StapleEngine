namespace Staple;

/// <summary>
/// Defines types of motion for a physics body
/// </summary>
public enum BodyMotionType
{
    /// <summary>
    /// Whether the body doesn't move
    /// </summary>
    Static,
    /// <summary>
    /// Whether the body ignores other colliders and is manually moved
    /// </summary>
    Kinematic,
    /// <summary>
    /// Whether the body is dynamic and moves normally
    /// </summary>
    Dynamic
}
