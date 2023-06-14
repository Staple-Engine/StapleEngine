namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void ResetScenePhysics()
        {
            foreach(var pair in pickEntityBodies)
            {
                Physics3D.Instance.DestroyBody(pair.Value.body);
            }

            pickEntityBodies.Clear();
        }

        public void ReplaceEntityBody(Entity entity, Transform transform, AABB bounds)
        {
            if(pickEntityBodies.TryGetValue(entity, out var pair))
            {
                Physics3D.Instance.DestroyBody(pair.body);

                pickEntityBodies.Remove(entity);
            }

            if(Physics3D.Instance.CreateBox(entity, bounds.Size, transform.Position, transform.Rotation, BodyMotionType.Dynamic, 0, out var body))
            {
                body.GravityFactor = 0;

                pickEntityBodies.Add(entity, new EntityBody()
                {
                    body = body,
                    bounds = bounds,
                });
            }
        }

        public void ReplaceEntityBodyIfNeeded(Entity entity, Transform transform, AABB bounds)
        {
            if (pickEntityBodies.TryGetValue(entity, out var pair) == false || (pair.bounds.center != bounds.center || pair.bounds.extents != bounds.extents))
            {
                ReplaceEntityBody(entity, transform, bounds);
            }
        }
    }
}