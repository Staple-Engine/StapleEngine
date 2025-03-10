using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Axis Aligned Bounding Box
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 0)]
public struct AABB
{
    /// <summary>
    /// The center of the box
    /// </summary>
    public readonly Vector3 center;

    /// <summary>
    /// The extents of the box (distance from the box as a radius)
    /// </summary>
    public readonly Vector3 extents;

    /// <summary>
    /// The minimum position of the box
    /// </summary>
    public readonly Vector3 min;

    /// <summary>
    /// The maximum position of the box
    /// </summary>
    public readonly Vector3 max;

    /// <summary>
    /// The size of the box
    /// </summary>
    public readonly Vector3 size;

    /// <summary>
    /// Creates an Axis Aligned Bounding Box from a center and size
    /// </summary>
    /// <param name="center">The center of the box</param>
    /// <param name="size">The size of the box</param>
    public AABB(Vector3 center, Vector3 size)
    {
        this.center = center;
        this.size = size;

        extents = size / 2;

        min = new(center.X - extents.X, center.Y - extents.Y, center.Z - extents.Z);
        max = new(center.X + extents.X, center.Y + extents.Y, center.Z + extents.Z);
    }

    public override readonly string ToString()
    {
        return $"({center}, {extents})";
    }

    /// <summary>
    /// Checks whether this box contains a point
    /// </summary>
    /// <param name="point">The point to check</param>
    /// <returns>Whether it contains a point</returns>
    public readonly bool Contains(Vector3 point)
    {
        return point.X >= min.X && point.Y >= min.Y && point.Z >= min.Z &&
            point.X <= max.X && point.Y <= max.Y && point.Z <= max.Z;
    }

    /// <summary>
    /// Creates an expanded form of this box with an increased size
    /// </summary>
    /// <param name="amount">The amount as a float</param>
    public readonly AABB Expanded(float amount)
    {
        return new AABB(center, size * amount);
    }

    /// <summary>
    /// Creates an expanded form of this box with an increased size
    /// </summary>
    /// <param name="amount">The amount as a Vector3</param>
    public readonly AABB Expanded(Vector3 amount)
    {
        return new AABB(center, size + amount);
    }

    /// <summary>
    /// Creates an AABB from a min/max vector
    /// </summary>
    /// <param name="min">the minimum value</param>
    /// <param name="max">the maximum value</param>
    /// <returns>The AABB</returns>
    public static AABB CreateFromMinMax(Vector3 min, Vector3 max)
    {
        return new AABB((min + max) / 2, max - min);
    }

    /// <summary>
    /// Calculates a AABB from a list of points
    /// </summary>
    /// <param name="points">The points to validate</param>
    /// <returns>The AABB</returns>
    public static AABB CreateFromPoints(Span<Vector3> points)
    {
        var min = Vector3.One * 999999;
        var max = Vector3.One * -999999;

        foreach (var v in points)
        {
            if (v.X < min.X)
            {
                min.X = v.X;
            }

            if (v.Y < min.Y)
            {
                min.Y = v.Y;
            }

            if (v.Z < min.Z)
            {
                min.Z = v.Z;
            }

            if (v.X > max.X)
            {
                max.X = v.X;
            }

            if (v.Y > max.Y)
            {
                max.Y = v.Y;
            }

            if (v.Z > max.Z)
            {
                max.Z = v.Z;
            }
        }

        return new AABB((min + max) / 2, max - min);
    }
}
