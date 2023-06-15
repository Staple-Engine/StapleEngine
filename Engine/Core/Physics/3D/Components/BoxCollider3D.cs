using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Represents a 3D box collider
    /// </summary>
    public class BoxCollider3D : Collider3D
    {
        /// <summary>
        /// Size of the box
        /// </summary>
        public Vector3 size = Vector3.One;

        private void Awake(Entity entity, Transform transform)
        {
            Physics3D.Instance?.CreateBox(entity, size, transform.Position, transform.Rotation, motionType,
                (ushort)(Scene.current?.world.GetEntityLayer(entity) ?? 0), isTrigger, gravityFactor, out body);
        }

        private void OnDestroy()
        {
            Physics3D.Instance?.DestroyBody(body);
        }
    }
}
