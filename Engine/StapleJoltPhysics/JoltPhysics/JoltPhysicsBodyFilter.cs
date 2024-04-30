using JoltPhysicsSharp;
using Staple.Internal;

namespace Staple.JoltPhysics;

internal class JoltPhysicsBodyFilter : BodyFilter
{
    public PhysicsTriggerQuery triggerQuery;

    protected override bool ShouldCollide(BodyID bodyID)
    {
        if(Physics3D.Instance?.Impl is JoltPhysics3D physics)
        {
            var localBody = physics.GetBody(bodyID);

            if(localBody == null)
            {
                return false;
            }

            if (triggerQuery == PhysicsTriggerQuery.Ignore && localBody.IsTrigger)
            {
                return false;
            }
        }

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