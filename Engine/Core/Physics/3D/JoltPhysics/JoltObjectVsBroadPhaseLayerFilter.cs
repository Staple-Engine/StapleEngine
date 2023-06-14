using JoltPhysicsSharp;

namespace Staple
{
    /// <summary>
    /// Object vs Broadphase Layer Filter
    /// Responsible for telling the physics system if an object collides with a broadphase
    /// </summary>
    internal class JoltObjectVsBroadPhaseLayerFilter : ObjectVsBroadPhaseLayerFilter
    {
        protected override bool ShouldCollide(ObjectLayer layer1, BroadPhaseLayer layer2)
        {
            return ColliderMask.ShouldCollide(layer1, layer2);
        }
    }
}
