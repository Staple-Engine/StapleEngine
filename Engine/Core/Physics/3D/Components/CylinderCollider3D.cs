using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D cylinder collider
    /// </summary>
    public class CylinderCollider3D : Collider3DBase
    {
        /// <summary>
        /// The height of the cylinder
        /// </summary>
        public float height = 1;

        /// <summary>
        /// The radius of the cylinder
        /// </summary>
        public float radius = 1;

        protected override void Awake(Entity entity, Transform transform)
        {
            Physics3D.Instance?.CreateCylinder(entity, height, radius, transform.Position, transform.Rotation, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), isTrigger, gravityFactor, out body);
        }

        protected override void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
