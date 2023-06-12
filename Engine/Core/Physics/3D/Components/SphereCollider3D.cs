using System.Numerics;

namespace Staple
{
    public class SphereCollider3D : IComponent
    {
        internal IBody3D body;

        public float radius = 1;

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
