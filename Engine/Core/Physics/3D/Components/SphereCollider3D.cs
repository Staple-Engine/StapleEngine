using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D sphere collider
    /// </summary>
    public class SphereCollider3D : IComponent
    {
        internal IBody3D body;

        /// <summary>
        /// The radius of the sphere
        /// </summary>
        public float radius = 1;

        /// <summary>
        /// The motion type of the body
        /// </summary>
        public BodyMotionType motionType = BodyMotionType.Dynamic;

        private void Awake(Entity entity)
        {
            Physics3D.Instance?.CreateSphere(entity, radius, Vector3.Zero, Quaternion.Identity, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), out body);
        }

        private void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
