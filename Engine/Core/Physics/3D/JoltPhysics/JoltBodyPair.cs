using JoltPhysicsSharp;
using System.Numerics;
using System;

namespace Staple;

/// <summary>
/// Jolt Physics 3D Body implementation
/// </summary>
internal class JoltBodyPair : IBody3D
{
    public Entity entity;
    public Body body;

    public Entity Entity => entity;

    public bool IsTrigger
    {
        get => body.IsSensor;

        set => Physics3D.Instance.SetBodyTrigger(this, value);
    }

    public Vector3 Position
    {
        get => body.CenterOfMassPosition;

        set => Physics3D.Instance.SetBodyPosition(this, value);
    }

    public Quaternion Rotation
    {
        get => body.Rotation;

        set => Physics3D.Instance.SetBodyRotation(this, value);
    }

    public Vector3 Velocity
    {
        get => body.GetLinearVelocity();

        set => body.SetLinearVelocity(value);
    }

    public Vector3 AngularVelocity
    {
        get => body.GetAngularVelocity();

        set => body.SetAngularVelocity(value);
    }

    public float Friction
    {
        get => body.Friction;

        set => body.Friction = value;
    }

    public float Restitution
    {
        get => body.Restitution;

        set => body.Restitution = value;
    }

    public BodyMotionType MotionType
    {
        get
        {
            return body.MotionType switch
            {
                JoltPhysicsSharp.MotionType.Static => BodyMotionType.Static,
                JoltPhysicsSharp.MotionType.Dynamic => BodyMotionType.Dynamic,
                JoltPhysicsSharp.MotionType.Kinematic => BodyMotionType.Kinematic,
                _ => throw new InvalidOperationException("Invalid Body Motion Type"),
            };
        }

        set
        {
            body.MotionType = value switch
            {
                BodyMotionType.Static => JoltPhysicsSharp.MotionType.Static,
                BodyMotionType.Dynamic => JoltPhysicsSharp.MotionType.Dynamic,
                BodyMotionType.Kinematic => JoltPhysicsSharp.MotionType.Kinematic,
                _ => throw new InvalidOperationException("Invalid Body Motion Type"),
            };
        }
    }

    public float GravityFactor
    {
        get
        {
            return Physics3D.Instance.GravityFactor(this);
        }

        set
        {
            Physics3D.Instance.SetGravityFactor(this, value);
        }
    }
}