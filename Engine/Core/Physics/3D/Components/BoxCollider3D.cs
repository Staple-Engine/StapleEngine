using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D box collider
    /// </summary>
    public class BoxCollider3D : IComponent
    {
        internal IBody3D body;

        /// <summary>
        /// Size of the box
        /// </summary>
        public Vector3 extents = Vector3.One;

        /// <summary>
        /// Gravity factor of the box
        /// </summary>
        public float gravityFactor = 1.0f;

        /// <summary>
        /// The motion type of the body
        /// </summary>
        public BodyMotionType motionType = BodyMotionType.Dynamic;

        private void Awake(Entity entity, Transform transform)
        {
            Physics3D.Instance?.CreateBox(entity, extents, transform.Position, transform.Rotation, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), out body);

            body.GravityFactor = gravityFactor;
        }

        private void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
