using JoltPhysicsSharp;

namespace Staple
{
    internal class JoltBroadPhaseLayerInterface : BroadPhaseLayerInterface
    {
        protected override BroadPhaseLayer GetBroadPhaseLayer(ObjectLayer layer)
        {
            if (layer < LayerMask.AllLayers.Count)
            {
                return new BroadPhaseLayer();
            }

            return new BroadPhaseLayer((byte)layer.Value);
        }

        protected override string GetBroadPhaseLayerName(BroadPhaseLayer layer)
        {
            return LayerMask.LayerToName(layer.Value);
        }

        protected override int GetNumBroadPhaseLayers()
        {
            return LayerMask.AllLayers.Count;
        }
    }
}
