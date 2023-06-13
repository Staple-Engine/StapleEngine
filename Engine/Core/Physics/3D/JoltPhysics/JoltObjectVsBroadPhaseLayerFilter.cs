using JoltPhysicsSharp;

namespace Staple
{
    internal class JoltObjectVsBroadPhaseLayerFilter : ObjectVsBroadPhaseLayerFilter
    {
        protected override bool ShouldCollide(ObjectLayer layer1, BroadPhaseLayer layer2)
        {
            return ColliderMask.ShouldCollide(layer1, layer2);
        }
    }
}
