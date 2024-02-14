using JoltPhysicsSharp;

namespace Staple;

internal class JoltPhysicsBroadPhaseLayerFilter : BroadPhaseLayerFilter
{
    protected override bool ShouldCollide(BroadPhaseLayer layer)
    {
        return true;
    }
}
