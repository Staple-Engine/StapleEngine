using JoltPhysicsSharp;

namespace Staple
{
    internal class JoltObjectLayerPairFilter : ObjectLayerPairFilter
    {
        protected override bool ShouldCollide(ObjectLayer object1, ObjectLayer object2)
        {
            return ColliderMask.ShouldCollide(object1, object2);
        }
    }
}
