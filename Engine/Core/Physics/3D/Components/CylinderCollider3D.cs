using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D cylinder collider
    /// </summary>
    public class CylinderCollider3D : IComponent
    {
        internal IBody3D body;

        /// <summary>
        /// The height of the cylinder
        /// </summary>
        public float height = 1;

        /// <summary>
        /// The radius of the cylinder
        /// </summary>
        public float radius = 1;

        /// <summary>
        /// The motion type of the body
        /// </summary>
        public BodyMotionType motionType = BodyMotionType.Dynamic;

        private void Awake(Entity entity)
        {
            Physics3D.Instance?.CreateCylinder(entity, height, radius, Vector3.Zero, Quaternion.Identity, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), out body);
        }

        private void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
