using JoltPhysicsSharp;

namespace Staple
{
    internal class JoltPhysicsObjectLayerFilter : ObjectLayerFilter
    {
        protected override bool ShouldCollide(ObjectLayer layer)
        {
            return true;
        }
    }
}