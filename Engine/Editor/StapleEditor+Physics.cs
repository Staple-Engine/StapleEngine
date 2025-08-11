using Staple.Internal;
using System.IO;
using System.Numerics;
using System.Reflection;

namespace Staple.Editor;

internal partial class StapleEditor
{
    /// <summary>
    /// Resets the scene physics
    /// </summary>
    public void ResetScenePhysics(bool cleanup)
    {
        if(cleanup)
        {
            foreach (var pair in pickEntityBodies)
            {
                Physics3D.Instance.DestroyBody(pair.Value.body);
            }
        }

        pickEntityBodies.Clear();

        componentIcons.Clear();

        if(World.Current != null)
        {
            World.EmitWorldChangedEvent();

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

                    var icon = ThumbnailCache.GetTexture(Path.Combine(EditorUtils.EditorPath.Value, "EditorResources", "ComponentIcons", attribute.path), true, true);

                    if(icon != null)
                    {
                        gotIcon = true;

                        componentIcons.AddOrSetKey(entity, icon);
                    }
                });
            });
        }
    }

    /// <summary>
    /// Replaces an entity's physics body in the scene
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <param name="bounds">The entity bounds</param>
    public void ReplaceEntityBody(Entity entity, AABB bounds)
    {
        if(pickEntityBodies.TryGetValue(entity, out var pair))
        {
            Physics3D.Instance.DestroyBody(pair.body);

            pickEntityBodies.Remove(entity);
        }

        if (Physics3D.Instance.CreateBox(entity, bounds.size, bounds.center, Quaternion.Identity, BodyMotionType.Dynamic, 0, false,
            0, 0, 0, true, true, true, false, 1, out var body))
        {
            pickEntityBodies.Add(entity, new EntityBody()
            {
                body = body,
                bounds = bounds,
            });
        }
    }

    /// <summary>
    /// Replaces an entity's body in the scene if needed
    /// </summary>
    /// <param name="entity">The entity to replace</param>
    /// <param name="transform">The entity's transform</param>
    /// <param name="bounds">The entity's bounds</param>
    public void ReplaceEntityBodyIfNeeded(Entity entity, AABB bounds)
    {
        if(bounds.extents.LengthSquared() == 0 || playMode != PlayMode.Stopped)
        {
            return;
        }

        if (pickEntityBodies.TryGetValue(entity, out var pair) == false ||
            (pair.bounds.center != bounds.center || pair.bounds.extents != bounds.extents))
        {
            ReplaceEntityBody(entity, bounds);
        }
    }

    /// <summary>
    /// Removes an entity's body in the scene
    /// </summary>
    /// <param name="entity">The entity</param>
    public void ClearEntityBody(Entity entity)
    {
        if(pickEntityBodies.TryGetValue(entity, out var pair) == false)
        {
            return;
        }

        Physics3D.Instance.DestroyBody(pair.body);

        pickEntityBodies.Remove(entity);
    }
}