using JoltPhysicsSharp;

namespace Staple
{
    /// <summary>
    /// Object Layer Pair Filter
    /// Responsible for telling the physics whether two objects collide
    /// </summary>
    internal class JoltObjectLayerPairFilter : ObjectLayerPairFilter
    {
        protected override bool ShouldCollide(ObjectLayer object1, ObjectLayer object2)
        {
            return ColliderMask.ShouldCollide(object1, object2);
        }
    }
}
