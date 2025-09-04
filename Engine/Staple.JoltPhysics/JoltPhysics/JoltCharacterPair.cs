using JoltPhysicsSharp;
using System.Numerics;

namespace Staple.JoltPhysics;

/// <summary>
/// Jolt Physics 3D Body implementation
/// </summary>
internal class JoltCharacterPair : IBody3D
{
    public Entity entity;
    public Character character;
    public float friction;
    public float gravityFactor;
    public bool enabled;

    public Entity Entity => entity;

    public bool IsTrigger
    {
        get => false;

        set { }
    }

    public Vector3 Position
    {
        get => character.GetPosition();

        set => character.SetPosition(value);
    }

    public Quaternion Rotation
    {
        get => character.GetRotation();

        set => character.SetRotation(value);
    }

    public Vector3 Velocity
    {
        get => character.GetLinearVelocity();

        set => character.SetLinearVelocity(value);
    }

    public Vector3 AngularVelocity
    {
        get => Vector3.Zero;

        set { }
    }

    public float Friction
    {
        get => friction;

        set { }
    }

    public float Restitution
    {
        get => 0;

        set { }
    }

    public BodyMotionType MotionType
    {
        get => BodyMotionType.Dynamic;

        set { }
    }

    public float GravityFactor
    {
        get => gravityFactor;

        set { }
    }

    public void AddAngularImpulse(Vector3 impulse)
    {
    }

    public void AddForce(Vector3 force)
    {
    }

    public void AddImpulse(Vector3 impulse)
    {
        character.AddImpulse(impulse);
    }
}
