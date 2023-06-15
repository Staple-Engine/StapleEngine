using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D mesh collider
    /// </summary>
    public class MeshCollider3D : Collider3D
    {
        /// <summary>
        /// The mesh for the collider.
        /// </summary>
        /// <remarks>Must be readable and be a triangle mesh</remarks>
        public Mesh mesh;

        private void Awake(Entity entity, Transform transform)
        {
            if(mesh == null)
            {
                return;
            }

            Physics3D.Instance?.CreateMesh(entity, mesh, transform.Position, transform.Rotation, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), isTrigger, gravityFactor, out body);
        }

        private void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
