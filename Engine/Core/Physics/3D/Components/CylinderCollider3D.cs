using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D cylinder collider
    /// </summary>
    public sealed class CylinderCollider3D : Collider3D
    {
        /// <summary>
        /// The height of the cylinder
        /// </summary>
        public float height = 1;

        /// <summary>
        /// The radius of the cylinder
        /// </summary>
        public float radius = 1;
    }
}
