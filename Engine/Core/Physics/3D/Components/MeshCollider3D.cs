using System.Numerics;

namespace Staple
{
    public class MeshCollider3D : IComponent
    {
        internal IBody3D body;

        public Mesh mesh;

        public BodyMotionType motionType = BodyMotionType.Dynamic;

        private void Awake(Entity entity)
        {
            if(mesh == null)
            {
                return;
            }

            Physics3D.Instance?.CreateMesh(entity, mesh, Vector3.Zero, Quaternion.Identity, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), out body);
        }

        private void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
