using System.Numerics;

namespace Staple
{
    internal interface IBody3D
    {
        Entity Entity { get; }

        Vector3 Position { get; }

        Vector3 Velocity { get; }

        Vector3 AngularVelocity { get; }

        BodyMotionType MotionType { get; }
    }
}
