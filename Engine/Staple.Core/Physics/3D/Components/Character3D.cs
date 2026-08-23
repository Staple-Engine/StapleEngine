using System.Numerics;

namespace Staple;

public partial class Character3D : IComponent
{
    /// <summary>
    /// The actual body instance
    /// </summary>
    internal IBody3D body;

    /// <summary>
    /// Gravity factor of this character
    /// </summary>
    public partial float gravityFactor { get; set; }

    /// <summary>
    /// The friction factor of this character
    /// </summary>
    public partial float friction { get; set; }

    /// <summary>
    /// The mass of this character
    /// </summary>
    public partial float mass { get; set; }

    /// <summary>
    /// The maximum slope angle this character may move
    /// </summary>
    public partial float maxSlopeAngle { get; set; }

    /// <summary>
    /// The upwards orientation of this character
    /// </summary>
    public partial Vector3 upDirection { get; set; }

    public Character3D()
    {
        gravityFactor = 1;
        friction = 0.2f;
        mass = 80;
        maxSlopeAngle = 50;
        upDirection = Vector3.Up;
    }
}
