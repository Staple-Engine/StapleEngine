using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Axis Aligned Bounding Box
    /// </summary>
    public struct AABB
    {
        /// <summary>
        /// The center of the box
        /// </summary>
        public Vector3 center;

        /// <summary>
        /// The extents of the box (distance from the box as a radius)
        /// </summary>
        public Vector3 extents;

        /// <summary>
        /// The minimum position of the box
        /// </summary>
        public Vector3 min => new Vector3(center.X - extents.X / 2, center.Y - extents.Y / 2, center.Z - extents.Z / 2);

        /// <summary>
        /// The maximum position of the box
        /// </summary>
        public Vector3 max => new Vector3(center.X + extents.X / 2, center.Y + extents.Y / 2, center.Z + extents.Z / 2);

        /// <summary>
        /// The size of the box
        /// </summary>
        public Vector3 size => extents * 2;

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

        /// <summary>
        /// Checks whether this box contains a point
        /// </summary>
        /// <param name="point">The point to check</param>
        /// <returns>Whether it contains a point</returns>
        public bool Contains(Vector3 point)
        {
            //Slight optimization to prevent many function calls
            var min = this.min;
            var max = this.max;

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
    }
}
