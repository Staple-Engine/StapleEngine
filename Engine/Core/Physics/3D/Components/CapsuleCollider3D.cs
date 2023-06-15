using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represnts a 3D capsule collider
    /// </summary>
    public class CapsuleCollider3D : Collider3D
    {
        /// <summary>
        /// The height of the capsule
        /// </summary>
        public float height = 1;

        /// <summary>
        /// The radius of the capsule
        /// </summary>
        public float radius = 1;

        private void Awake(Entity entity, Transform transform)
        {
            Physics3D.Instance?.CreateCapsule(entity, height, radius, transform.Position, transform.Rotation, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), isTrigger, gravityFactor, out body);
        }

        private void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
