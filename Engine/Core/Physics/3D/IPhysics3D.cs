using System.Numerics;

namespace Staple
{
    internal interface IPhysics3D
    {
        bool Destroyed { get; }

        Vector3 Gravity { get; set; }

        void Destroy();

        void Update(float deltaTime);

        bool CreateBox(Entity entity, Vector3 extents, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body);

        bool CreateSphere(Entity entity, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body);

        bool CreateCapsule(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body);

        bool CreateCylinder(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body);

        bool CreateMesh(Entity entity, Mesh mesh, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body);

        void DestroyBody(IBody3D body);

        void AddBody(IBody3D body, bool activated);

        void RemoveBody(IBody3D body);

        void SetBodyMotion(IBody3D body, BodyMotionType motionType);
    }
}
