using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D mesh collider
    /// </summary>
    public class MeshCollider3D : IComponent
    {
        internal IBody3D body;

        /// <summary>
        /// The mesh for the collider.
        /// </summary>
        /// <remarks>Must be readable and be a triangle mesh</remarks>
        public Mesh mesh;

        /// <summary>
        /// The motion type of the body
        /// </summary>
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
