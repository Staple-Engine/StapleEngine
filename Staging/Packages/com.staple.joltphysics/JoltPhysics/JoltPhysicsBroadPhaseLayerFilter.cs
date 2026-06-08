using JoltPhysicsSharp;

namespace Staple.JoltPhysics;

internal class JoltPhysicsBroadPhaseLayerFilter : BroadPhaseLayerFilter
{
    protected override bool ShouldCollide(BroadPhaseLayer layer)
    {
        return true;
    }
}
