﻿using JoltPhysicsSharp;
using System.Numerics;
using System;

namespace Staple
{
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

        public BodyMotionType MotionType
        {
            get
            {
                switch (body.MotionType)
                {
                    case JoltPhysicsSharp.MotionType.Static:

                        return BodyMotionType.Static;

                    case JoltPhysicsSharp.MotionType.Dynamic:

                        return BodyMotionType.Dynamic;

                    case JoltPhysicsSharp.MotionType.Kinematic:

                        return BodyMotionType.Kinematic;
                }

                throw new InvalidOperationException("Invalid Body Motion Type");
            }

            set
            {
                switch (value)
                {
                    case BodyMotionType.Static:

                        body.MotionType = JoltPhysicsSharp.MotionType.Static;

                        break;

                    case BodyMotionType.Dynamic:

                        body.MotionType = JoltPhysicsSharp.MotionType.Dynamic;

                        break;

                    case BodyMotionType.Kinematic:

                        body.MotionType = JoltPhysicsSharp.MotionType.Kinematic;

                        break;

                    default:

                        throw new InvalidOperationException("Invalid Body Motion Type");
                }
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
}