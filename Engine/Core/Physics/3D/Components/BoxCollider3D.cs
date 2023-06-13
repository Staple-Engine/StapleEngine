using System.Numerics;

namespace Staple
{
    public class BoxCollider3D : IComponent
    {
        internal IBody3D body;

        public Vector3 extents = Vector3.One;

        public float gravityFactor = 1.0f;

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
