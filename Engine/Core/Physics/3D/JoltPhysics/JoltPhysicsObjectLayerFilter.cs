using JoltPhysicsSharp;

namespace Staple;

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