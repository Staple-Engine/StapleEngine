using System.Numerics;

namespace Staple;

public class TargetBone : IComponent, ISkinModifier
{
    public enum Interpolation
    {
        None,
        Slerp,
    }

    public Transform targetTransform;

    public Vector3 targetPoint;

    public Vector3 angleLimitMin = new(-65, -30, -10);

    public Vector3 angleLimitMax = new(65, 30, 10);

    public Interpolation interpolation = Interpolation.None;

    public float adjustmentSpeed = 5;

    public void Apply(Transform bone, bool wasReset)
    {
        var target = targetTransform?.Position ?? targetPoint;

        var forward = (target - bone.Position).Normalized;

        var scale = bone.Scale;

        if(scale.X < 0)
        {
            forward.X *= -1;
        }

        if (scale.Y < 0)
        {
            forward.Y *= -1;
        }

        if (scale.Z < 0)
        {
            forward.Z *= -1;
        }

        var rotation = Quaternion.LookAt(forward, Vector3.Up);

        var parentRotation = bone?.Parent?.Rotation ?? Quaternion.Identity;

        var invertedParentRotation = Quaternion.Inverse(parentRotation);

        var localRotation = invertedParentRotation * rotation;

        var angles = localRotation.ToEulerAngles();

        angles.X = Math.Clamp(angles.X, angleLimitMin.X, angleLimitMax.X);
        angles.Y = Math.Clamp(angles.Y, angleLimitMin.Y, angleLimitMax.Y);
        angles.Z = Math.Clamp(angles.Z, angleLimitMin.Z, angleLimitMax.Z);

        var finalRotation = Quaternion.Euler(angles);

        switch(interpolation)
        {
            case Interpolation.None:

                bone.LocalRotation = finalRotation;

                break;

            case Interpolation.Slerp:

                {
                    var current = bone.LocalRotation;

                    var targetRotation = Quaternion.Slerp(current, finalRotation, adjustmentSpeed * Time.deltaTime);

                    bone.LocalRotation = targetRotation;
                }

                break;
        }

    }
}
