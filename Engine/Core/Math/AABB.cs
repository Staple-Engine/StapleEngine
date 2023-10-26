using MessagePack;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple
{
    /// <summary>
    /// Axis Aligned Bounding Box
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    [MessagePackObject]
    public struct AABB
    {
        /// <summary>
        /// The center of the box
        /// </summary>
        [Key(0)]
        public Vector3 center;

        /// <summary>
        /// The extents of the box (distance from the box as a radius)
        /// </summary>
        [Key(1)]
        public Vector3 extents;

        /// <summary>
        /// The minimum position of the box
        /// </summary>
        [IgnoreMember]
        public readonly Vector3 Min => new(center.X - extents.X, center.Y - extents.Y, center.Z - extents.Z);

        /// <summary>
        /// The maximum position of the box
        /// </summary>
        [IgnoreMember]
        public readonly Vector3 Max => new(center.X + extents.X, center.Y + extents.Y, center.Z + extents.Z);

        /// <summary>
        /// The size of the box
        /// </summary>
        [IgnoreMember]
        public readonly Vector3 Size => extents * 2;

        /// <summary>
        /// Creates an Axis Aligned Bounding Box from a center and size
        /// </summary>
        /// <param name="center">The center of the box</param>
        /// <param name="size">The size of the box</param>
        public AABB(Vector3 center, Vector3 size)
        {
            this.center = center;

            extents = size / 2;
        }

        public override string ToString()
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
            //Slight optimization to prevent many function calls
            var min = Min;
            var max = Max;

            return point.X >= min.X && point.Y >= min.Y && point.Z >= min.Z &&
                point.X <= max.X && point.Y <= max.Y && point.Z <= max.Z;
        }

        /// <summary>
        /// Expands the box's size by an amount
        /// </summary>
        /// <param name="amount">The amount as a float</param>
        public void Expand(float amount)
        {
            extents += Vector3.One * (amount / 2);
        }

        /// <summary>
        /// Expands the box's size by an amount
        /// </summary>
        /// <param name="amount">The amount as a Vector3</param>
        public void Expand(Vector3 amount)
        {
            extents += amount / 2;
        }

        /// <summary>
        /// Calculates a AABB from a list of points
        /// </summary>
        /// <param name="points">The points to validate</param>
        /// <returns>The AABB</returns>
        public static AABB FromPoints(Vector3[] points)
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
}
