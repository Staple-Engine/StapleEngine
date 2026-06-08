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
    public Transform transform;
    public float friction;
    public float gravityFactor;
    public bool enabled;

    public Vector3 previousPosition;
    public Vector3 currentPosition;
    public Vector3 interpolatedPosition;

    public Quaternion previousRotation = Quaternion.Identity;
    public Quaternion currentRotation = Quaternion.Identity;
    public Quaternion interpolatedRotation = Quaternion.Identity;

    public Entity Entity => entity;

    public bool IsTrigger
    {
        get => false;

        set { }
    }

    public Vector3 Position
    {
        get
        {
            if (Physics.InterpolatePhysics)
            {
                return interpolatedPosition;
            }

            return currentPosition;
        }

        set
        {
            currentPosition = value;

            character.SetPosition(value);
        }
    }

    public Quaternion Rotation
    {
        get
        {
            if (Physics.InterpolatePhysics)
            {
                return interpolatedRotation;
            }

            return currentRotation;
        }

        set
        {
            currentRotation = value;

            character.SetRotation(value);
        }
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
