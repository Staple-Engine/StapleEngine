using System.Numerics;

namespace Staple
{
    [AbstractComponent]
    public class Collider3D : IComponent
    {
        /// <summary>
        /// The position of the collider in local space
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// The rotation of the collider in local space
        /// </summary>
        public Quaternion rotation;
    }
}
