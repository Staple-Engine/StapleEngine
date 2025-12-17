using System;
using System.Numerics;

namespace Staple;

/// <summary>
/// Represents a bounding sphere
/// </summary>
/// <param name="center">The center of the sphere</param>
/// <param name="radius">The radius of the sphere</param>
public readonly struct BoundingSphere(Vector3 center, float radius)
{
    /// <summary>
    /// The center of this sphere
    /// </summary>
    public readonly Vector3 center = center;

    /// <summary>
    /// The radius of this sphere
    /// </summary>
    public readonly float radius = radius;

    /// <summary>
    /// Creates a bounding sphere from a set of points
    /// </summary>
    /// <param name="points">The points</param>
    /// <returns>The bounding sphere</returns>
    public static BoundingSphere CreateFromPoints(Span<Vector3> points)
    {
        var aabb = AABB.CreateFromPoints(points);

        return CreateFromAABB(aabb);
    }

    /// <summary>
    /// Creates a bounding sphere from a <see cref="AABB"/>
    /// </summary>
    /// <param name="aabb">The <see cref="AABB"/></param>
    /// <returns>The bounding sphere</returns>
    public static BoundingSphere CreateFromAABB(AABB aabb)
    {
        var center = aabb.center;

        var temp = aabb.min - center;
        var distanceSquare = Vector3.Dot(temp, temp);
        var maxDistanceSquare = distanceSquare;

        temp = aabb.max - center;
        distanceSquare = Vector3.Dot(temp, temp);

        maxDistanceSquare = Math.Max(maxDistanceSquare, distanceSquare);

        return new(center, Math.Sqrt(maxDistanceSquare));
    }
}
