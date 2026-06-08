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
                var entity = pair.Value.body.Entity;

                Physics3D.Instance.DestroyBody(pair.Value.body);

                entity.Destroy();
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
        Entity wrapper = default;

        if(pickEntityBodies.TryGetValue(entity, out var pair))
        {
            wrapper = pair.body.Entity;

            Physics3D.Instance.DestroyBody(pair.body);

            pickEntityBodies.Remove(entity);
        }

        if(!wrapper.IsValid)
        {
            wrapper = Entity.Create(typeof(Transform));

            wrapper.Name = $"{entity.Name} Physics Wrapper";

            wrapper.HierarchyVisibility = EntityHierarchyVisibility.HideAndDontSave;
        }

        if (Physics3D.Instance.CreateBox(wrapper, bounds.size, bounds.center, Quaternion.Identity, BodyMotionType.Dynamic,
            Physics3D.PhysicsPickLayer, false, 0, 0, 0, true, true, true, false, 1, out var body))
        {
            pickEntityBodies.Add(entity, new EntityBody()
            {
                body = body,
                bounds = bounds,
            });
        }
        else
        {
            wrapper.Destroy();
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

        if (!pickEntityBodies.TryGetValue(entity, out var pair) ||
            pair.bounds != bounds)
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
        if(!pickEntityBodies.TryGetValue(entity, out var pair))
        {
            return;
        }

        var e = pair.body.Entity;

        Physics3D.Instance.DestroyBody(pair.body);

        e.Destroy();

        pickEntityBodies.Remove(entity);
    }

    /// <summary>
    /// Recreates the regular rigid bodies and characters in the scene
    /// </summary>
    public void RecreateRigidBodies()
    {
        var rigidBodies = Scene.Query<RigidBody3D>();
        var characters = Scene.Query<Character3D>();

        foreach (var pair in rigidBodies)
        {
            Physics3D.Instance.RecreateBody(pair.Item1);
        }

        foreach (var pair in characters)
        {
            Physics3D.Instance.RecreateBody(pair.Item1);
        }
    }
}
