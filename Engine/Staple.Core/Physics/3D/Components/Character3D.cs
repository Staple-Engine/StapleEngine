using System.Numerics;

namespace Staple;

public class Character3D : IComponent
{
    /// <summary>
    /// The actual body instance
    /// </summary>
    internal IBody3D body;

    /// <summary>
    /// Gravity factor of this character
    /// </summary>
    public float gravityFactor = 1;

    /// <summary>
    /// The friction factor of this character
    /// </summary>
    public float friction = 0.2f;

    /// <summary>
    /// The mass of this character
    /// </summary>
    public float mass = 80;

    /// <summary>
    /// The maximum slope angle this character may move
    /// </summary>
    public float maxSlopeAngle = 50;

    /// <summary>
    /// The upwards orientation of this character
    /// </summary>
    public Vector3 upDirection = Vector3.UnitY;
}
