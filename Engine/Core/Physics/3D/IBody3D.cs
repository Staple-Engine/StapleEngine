using System.Numerics;

namespace Staple
{
    public interface IBody3D
    {
        Entity Entity { get; }

        Vector3 Velocity { get; set; }

        Vector3 AngularVelocity { get; set; }

        BodyMotionType MotionType { get; set; }

        float GravityFactor { get; set; }
    }
}
