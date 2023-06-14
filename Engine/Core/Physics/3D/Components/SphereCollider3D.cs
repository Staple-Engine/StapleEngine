using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D sphere collider
    /// </summary>
    public class SphereCollider3D : Collider3DBase
    {
        /// <summary>
        /// The radius of the sphere
        /// </summary>
        public float radius = 1;

        protected override void Awake(Entity entity, Transform transform)
        {
            Physics3D.Instance?.CreateSphere(entity, radius, transform.Position, transform.Rotation, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), isTrigger, gravityFactor, out body);
        }

        protected override void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
