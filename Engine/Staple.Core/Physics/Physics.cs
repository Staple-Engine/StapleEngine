using Staple.Internal;
using System.Configuration;
using System.Numerics;

namespace Staple;

/// <summary>
/// General Physics interface
/// </summary>
public static class Physics
{
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
        get => AppSettings.Current?.usePhysicsInterpolation ?? false;

        set => AppSettings.Current?.usePhysicsInterpolation = value;
    }

    /// <summary>
    /// The current physics frame rate
    /// </summary>
    public static int PhysicsFrameRate
    {
        get => AppSettings.Current?.physicsFrameRate ?? 0;

        set
        {
            AppSettings.Current?.physicsFrameRate = value;

            if(AppSettings.Current?.physicsFrameRate <= 0)
            {
                AppSettings.Current?.physicsFrameRate = 1;
            }

            Physics3D.Instance?.UpdateConfiguration();
        }
    }

    /// <summary>
    /// Casts a ray and checks for a hit
    /// </summary>
    /// <param name="ray">The ray to cast</param>
    /// <param name="body">The hit body</param>
    /// <param name="fraction">The fraction of distance to hit the body</param>
    /// <param name="triggerQuery">Whether to hit triggers</param>
    /// <param name="maxDistance">The maximum distance to hit</param>
    /// <returns>Whether the body has been hit</returns>
    public static bool RayCast3D(Ray ray, out IBody3D body, out float fraction, LayerMask layerMask, PhysicsTriggerQuery triggerQuery = PhysicsTriggerQuery.Ignore, float maxDistance = 1.0f)
    {
        body = default;
        fraction = default;

        return Physics3D.Instance?.RayCast(ray, out body, out fraction, layerMask, triggerQuery, maxDistance) ?? false;
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
}
