using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

/// <summary>
/// General Physics interface
/// </summary>
public static class Physics
{
    private class RaycastSorter(RenderableSortMode sortMode) : IComparer<RaycastHit3D>
    {
        public int Compare(RaycastHit3D x, RaycastHit3D y)
        {
            return sortMode switch
            {
                RenderableSortMode.FrontToBack => x.fraction.CompareTo(y.fraction),
                RenderableSortMode.BackToFront => y.fraction.CompareTo(x.fraction),
                _ => 0,
            };
        }
    }

    private class BodySorter(Vector3 origin, RenderableSortMode sortMode) : IComparer<IBody3D>
    {
        public int Compare(IBody3D x, IBody3D y)
        {
            var distanceX = Vector3.DistanceSquared(x.Position, origin);
            var distanceY = Vector3.DistanceSquared(y.Position, origin);

            return sortMode switch
            {
                RenderableSortMode.FrontToBack => distanceX.CompareTo(distanceY),
                RenderableSortMode.BackToFront => distanceY.CompareTo(distanceX),
                _ => 0,
            };
        }
    }

    /// <summary>
    /// The gravity of the 3D physics system
    /// </summary>
    public static Vector3 Gravity3D
    {
        get => Physics3D.Instance?.Gravity ?? Vector3.Zero;

        set => Physics3D.Instance?.Gravity = value;
    }

    /// <summary>
    /// Whether to interpolate physics
    /// </summary>
    public static bool InterpolatePhysics
    {
        get => AppSettings.Active.usePhysicsInterpolation;

        set => AppSettings.Active.usePhysicsInterpolation = value;
    }

    /// <summary>
    /// The current physics frame rate
    /// </summary>
    public static int PhysicsFrameRate
    {
        get => AppSettings.Active.physicsFrameRate;

        set
        {
            AppSettings.Active.physicsFrameRate = value;

            if(AppSettings.Active.physicsFrameRate <= 0)
            {
                AppSettings.Active.physicsFrameRate = 1;
            }

            Physics3D.Instance?.UpdateConfiguration();
        }
    }

    /// <summary>
    /// Casts a ray and checks for a hit
    /// </summary>
    /// <param name="ray">The ray to cast</param>
    /// <param name="hit">The result of the raycast</param>
    /// <param name="triggerQuery">Whether to hit triggers</param>
    /// <param name="maxDistance">The maximum distance to hit</param>
    /// <returns>Whether the body has been hit</returns>
    public static bool RayCast3D(Ray ray, out RaycastHit3D hit, LayerMask layerMask, PhysicsTriggerQuery triggerQuery = PhysicsTriggerQuery.Ignore, float maxDistance = 1.0f)
    {
        hit = default;

        return Physics3D.Instance?.RayCast(ray, out hit, layerMask, triggerQuery, maxDistance) ?? false;
    }

    /// <summary>
    /// Casts a ray and gets a collision result
    /// </summary>
    /// <param name="ray">The ray to cast</param>
    /// <param name="layerMask">The layer mask to use, or LayerMask.Everything.value</param>
    /// <param name="triggerQuery">Whether to hit triggers</param>
    /// <param name="maxDistance">The maximum distance to hit</param>
    /// <param name="sortMode">How to sort the results</param>
    /// <returns>An array of all hits</returns>
    public static RaycastHit3D[] RayCastAll(Ray ray, LayerMask layerMask, PhysicsTriggerQuery triggerQuery, float maxDistance,
        RenderableSortMode sortMode = RenderableSortMode.None)
    {
        return Physics3D.Instance?.RayCastAll(ray, layerMask, triggerQuery, maxDistance, sortMode) ?? [];
    }

    /// <summary>
    /// Casts a ray and gets a collision result
    /// </summary>
    /// <param name="ray">The ray to cast</param>
    /// <param name="hits">A container for all results</param>
    /// <param name="layerMask">The layer mask to use, or LayerMask.Everything.value</param>
    /// <param name="triggerQuery">Whether to hit triggers</param>
    /// <param name="maxDistance">The maximum distance to hit</param>
    /// <param name="sortMode">How to sort the results</param>
    /// <returns>How many elements were used for <see cref="hits"/></returns>
    public static int RayCastNoAlloc(Ray ray, Span<RaycastHit3D> hits, LayerMask layerMask, PhysicsTriggerQuery triggerQuery, float maxDistance,
        RenderableSortMode sortMode = RenderableSortMode.None)
    {
        return Physics3D.Instance?.RayCastNoAlloc(ray, hits, layerMask, triggerQuery, maxDistance, sortMode) ?? 0;
    }

    /// <summary>
    /// Gets the 3D body that belongs to an entity
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <returns>The body, or null</returns>
    public static IBody3D GetBody3D(Entity entity)
    {
        return Physics3D.Instance?.GetBody(entity);
    }

    /// <summary>
    /// Attempts to get a 3D body that belong to an entity
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <param name="body">The body</param>
    /// <returns>Whether the body was found</returns>
    public static bool TryGetBody3D(Entity entity, out IBody3D body)
    {
        body = Physics3D.Instance?.GetBody(entity);

        return body != null;
    }

    /// <summary>
    /// Sorts <see cref="RaycastHit3D"/>
    /// </summary>
    /// <param name="hits">The <see cref="RaycastHit3D"/> to sort</param>
    /// <param name="sortMode">The sorting mode</param>
    public static void SortRaycastHits3D(Span<RaycastHit3D> hits, RenderableSortMode sortMode)
    {
        MemoryExtensions.Sort(hits, new RaycastSorter(sortMode));
    }

    /// <summary>
    /// Sorts <see cref="IBody3D"/> based on an origin point
    /// </summary>
    /// <param name="origin">The origin point</param>
    /// <param name="bodies">The <see cref="IBody3D"/> to sort</param>
    /// <param name="sortMode">The sorting mode</param>
    public static void SortBodies3D(Vector3 origin, Span<IBody3D> bodies, RenderableSortMode sortMode)
    {
        MemoryExtensions.Sort(bodies, new BodySorter(origin, sortMode));
    }
}
