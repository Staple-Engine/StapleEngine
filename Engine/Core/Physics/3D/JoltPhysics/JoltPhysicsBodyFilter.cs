using JoltPhysicsSharp;

namespace Staple
{
    internal class JoltPhysicsBodyFilter : BodyFilter
    {
        public PhysicsTriggerQuery triggerQuery;

        protected override bool ShouldCollide(BodyID bodyID)
        {
            return true;
        }

        protected override bool ShouldCollideLocked(Body body)
        {
            if(triggerQuery == PhysicsTriggerQuery.Ignore && body.IsSensor)
            {
                return false;
            }

            return true;
        }
    }
}