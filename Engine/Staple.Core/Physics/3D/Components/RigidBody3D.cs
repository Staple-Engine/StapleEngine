namespace Staple;

public partial class RigidBody3D : IComponent
{
    /// <summary>
    /// The actual body instance
    /// </summary>
    internal IBody3D body;

    /// <summary>
    /// The motion type of the body
    /// </summary>
    public partial BodyMotionType motionType { get; set; }

    /// <summary>
    /// Whether to freeze rotation in the X axis
    /// </summary>
    public partial bool freezeRotationX { get; set; }

    /// <summary>
    /// Whether to freeze rotation in the Y axis
    /// </summary>
    public partial bool freezeRotationY { get; set; }

    /// <summary>
    /// Whether to freeze rotation in the Z axis
    /// </summary>
    public partial bool freezeRotationZ { get; set; }

    /// <summary>
    /// Whether this body is acting as a 2D body
    /// </summary>
    public partial bool is2DPlane { get; set; }

    /// <summary>
    /// Gravity factor of this body
    /// </summary>
    public partial float gravityFactor { get; set; }

    /// <summary>
    /// Whether this body is a trigger (doesn't collide, detects events)
    /// </summary>
    public partial bool isTrigger { get; set; }

    /// <summary>
    /// The friction factor of this body
    /// </summary>
    public partial float friction { get; set; }

    /// <summary>
    /// The restitution factor of this body
    /// </summary>
    public partial float restitution { get; set; }

    /// <summary>
    /// The mass of this body
    /// </summary>
    public partial float mass { get; set; }

    public RigidBody3D()
    {
        motionType = BodyMotionType.Dynamic;
        gravityFactor = 1.0f;
        friction = 0.2f;
        mass = 80;
    }
}
