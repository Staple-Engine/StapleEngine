using System.Numerics;

namespace Staple
{
    public class BoxCollider3D : IComponent
    {
        internal IBody3D body;

        public Vector3 extents = Vector3.One;

        public BodyMotionType motionType = BodyMotionType.Dynamic;

        private void Awake(Entity entity)
        {
            Physics3D.Instance?.CreateBox(entity, extents, Vector3.Zero, Quaternion.Identity, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), out body);
        }

        private void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
