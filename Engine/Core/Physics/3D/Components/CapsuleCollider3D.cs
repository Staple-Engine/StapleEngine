using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represnts a 3D capsule collider
    /// </summary>
    public class CapsuleCollider3D : IComponent
    {
        internal IBody3D body;

        /// <summary>
        /// The height of the capsule
        /// </summary>
        public float height = 1;

        /// <summary>
        /// The radius of the capsule
        /// </summary>
        public float radius = 1;

        /// <summary>
        /// The motion type of the body
        /// </summary>
        public BodyMotionType motionType = BodyMotionType.Dynamic;

        private void Awake(Entity entity)
        {
            Physics3D.Instance?.CreateCapsule(entity, height, radius, Vector3.Zero, Quaternion.Identity, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), out body);
        }

        private void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
