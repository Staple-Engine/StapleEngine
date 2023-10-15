using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D box collider
    /// </summary>
    public sealed class BoxCollider3D : Collider3D
    {
        /// <summary>
        /// Size of the box
        /// </summary>
        public Vector3 size = Vector3.One;
    }
}
