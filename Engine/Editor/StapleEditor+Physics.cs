using System.IO;
using System.Reflection;

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

            componentIcons.Clear();

            if(Scene.current?.world != null)
            {
                Scene.current.world.Iterate((entity) =>
                {
                    var gotIcon = false;

                    Scene.current.world.IterateComponents(entity, (ref IComponent component) =>
                    {
                        if(gotIcon)
                        {
                            return;
                        }

                        var attribute = component.GetType().GetCustomAttribute<ComponentIconAttribute>();

                        if(attribute == null)
                        {
                            return;
                        }

                        var icon = ThumbnailCache.GetTexture(Path.Combine(StapleBasePath, "Staging", "Editor Resources", "Component Icons", attribute.path));

                        if(icon != null)
                        {
                            gotIcon = true;

                            componentIcons.AddOrSetKey(entity, icon);
                        }
                    });
                });
            }
        }

        public void ReplaceEntityBody(Entity entity, Transform transform, AABB bounds)
        {
            if(pickEntityBodies.TryGetValue(entity, out var pair))
            {
                Physics3D.Instance.DestroyBody(pair.body);

                pickEntityBodies.Remove(entity);
            }

            var extents = bounds.extents;

            var needsBoundsFix = extents.X <= 0 || extents.Y <= 0 || extents.Z <= 0;

            if(needsBoundsFix)
            {
                if(extents.X <= 0)
                {
                    extents.X = 1.0f;
                }

                if (extents.Y <= 0)
                {
                    extents.Y = 1.0f;
                }

                if (extents.Z <= 0)
                {
                    extents.Z = 1.0f;
                }
            }

            if (Physics3D.Instance.CreateBox(entity, extents, transform.Position, transform.Rotation, BodyMotionType.Dynamic, 0, false, 0, out var body))
            {
                pickEntityBodies.Add(entity, new EntityBody()
                {
                    body = body,
                    bounds = bounds,
                });
            }
        }

        public void ReplaceEntityBodyIfNeeded(Entity entity, Transform transform, AABB bounds)
        {
            if(bounds.extents.LengthSquared() == 0)
            {
                return;
            }

            if (pickEntityBodies.TryGetValue(entity, out var pair) == false || (pair.bounds.center != bounds.center || pair.bounds.extents != bounds.extents))
            {
                ReplaceEntityBody(entity, transform, bounds);
            }
        }
    }
}