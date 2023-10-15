using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D mesh collider
    /// </summary>
    public sealed class MeshCollider3D : Collider3D
    {
        /// <summary>
        /// The mesh for the collider.
        /// </summary>
        /// <remarks>Must be readable and be a triangle mesh</remarks>
        public Mesh mesh;
    }
}
