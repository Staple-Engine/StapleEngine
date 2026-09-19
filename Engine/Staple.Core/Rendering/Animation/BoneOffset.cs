using System.Numerics;

namespace Staple;

public class BoneOffset : SkinModifier
{
    public Vector3 positionOffset;
    public Quaternion rotationOffset = Quaternion.Identity;
    public Vector3 scaleOffset;

    public override void Apply(Transform bone, bool wasReset)
    {
        if(!wasReset)
        {
            return;
        }

        bone.LocalPosition += positionOffset;
        bone.LocalScale += scaleOffset;
        bone.LocalRotation = rotationOffset * bone.LocalRotation;
    }
}
