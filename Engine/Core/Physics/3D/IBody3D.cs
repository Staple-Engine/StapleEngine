using System.Numerics;

namespace Staple;

/// <summary>
/// Represents a 3D body
/// </summary>
public interface IBody3D
{
    /// <summary>
    /// The entity this belongs to
    /// </summary>
    Entity Entity { get; }

    /// <summary>
    /// Whether this body is a trigger
    /// </summary>
    bool IsTrigger { get; set; }

    /// <summary>
    /// The current position of this body
    /// </summary>
    Vector3 Position { get; set; }

    /// <summary>
    /// The current rotation of this body
    /// </summary>
    Quaternion Rotation { get; set; }

    /// <summary>
    /// The current velocity of this body
    /// </summary>
    Vector3 Velocity { get; set; }

    /// <summary>
    /// The current angular velocity of this body
    /// </summary>
    Vector3 AngularVelocity { get; set; }

    /// <summary>
    /// The motion type for this body
    /// </summary>
    BodyMotionType MotionType { get; set; }

    /// <summary>
    /// The gravity factor (multiplier) of this body
    /// </summary>
    float GravityFactor { get; set; }

    /// <summary>
    /// The body's friction
    /// </summary>
    float Friction { get; set; }

    /// <summary>
    /// The body's restitution
    /// </summary>
    float Restitution { get; set; }
}
