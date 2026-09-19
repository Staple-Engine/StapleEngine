using System.Numerics;

namespace Staple;

public class TargetBone : SkinModifier
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

    public override void Apply(Transform bone, bool wasReset)
    {
        var target = targetTransform?.Position ?? targetPoint;

        var scale = bone.Scale.Abs();

        var rotation = Quaternion.LookAt(bone.Position, target, Vector3.Up);

        var parentRotation = bone?.Parent?.Rotation ?? Quaternion.Identity;

        var invertedParentRotation = Quaternion.Inverse(parentRotation);

        var localRotation = invertedParentRotation * rotation;

        var angles = localRotation.ToEulerAngles().Clamp(angleLimitMin, angleLimitMax);

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
