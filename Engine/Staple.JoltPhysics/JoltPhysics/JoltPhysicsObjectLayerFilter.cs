using JoltPhysicsSharp;

namespace Staple.JoltPhysics;

internal class JoltPhysicsObjectLayerFilter : ObjectLayerFilter
{
    public LayerMask layerMask;

    protected override bool ShouldCollide(ObjectLayer layer)
    {
        if(layerMask.HasLayer(layer.Value))
        {
            return true;
        }

        return false;
    }
}