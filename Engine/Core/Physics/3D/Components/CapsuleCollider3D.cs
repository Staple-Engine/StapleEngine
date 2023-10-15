using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represnts a 3D capsule collider
    /// </summary>
    public sealed class CapsuleCollider3D : Collider3D
    {
        /// <summary>
        /// The height of the capsule
        /// </summary>
        public float height = 1;

        /// <summary>
        /// The radius of the capsule
        /// </summary>
        public float radius = 1;
    }
}
