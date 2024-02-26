using System.IO;
using System.Reflection;

namespace Staple.Editor;

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

        if(World.Current != null)
        {
            Scene.IterateEntities((entity) =>
            {
                var gotIcon = false;

                entity.IterateComponents((ref IComponent component) =>
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

                    var icon = ThumbnailCache.GetTexture(Path.Combine(StapleBasePath, "Staging", "Editor Resources", "Component Icons", attribute.path), true);

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

        var extents = bounds.extents * transform.Scale * 2;

        var needsBoundsFix = extents.X < JoltPhysics3D.MinExtents ||
            extents.Y < JoltPhysics3D.MinExtents ||
            extents.Z < JoltPhysics3D.MinExtents;

        if(needsBoundsFix)
        {
            if(extents.X < JoltPhysics3D.MinExtents)
            {
                extents.X = JoltPhysics3D.MinExtents;
            }

            if (extents.Y < JoltPhysics3D.MinExtents)
            {
                extents.Y = JoltPhysics3D.MinExtents;
            }

            if (extents.Z < JoltPhysics3D.MinExtents)
            {
                extents.Z = JoltPhysics3D.MinExtents;
            }
        }

        if (Physics3D.Instance.CreateBox(entity, extents, transform.Position, transform.Rotation, BodyMotionType.Dynamic, 0, false,
            0, 0, 0, true, true, true, false, out var body))
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