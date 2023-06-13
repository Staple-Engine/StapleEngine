using System;
using System.Numerics;

namespace Staple
{
    internal class Physics3D : ISubsystem
    {
        private static Physics3D instance;

        public static Physics3D Instance
        {
            get
            {
                if (instance == null)
                {
                    throw new InvalidOperationException("Physics3D was not initialized");
                }

                return instance;
            }

            set
            {
                instance = value;
            }
        }

        public static readonly byte Priority = 2;

        public delegate void OnBodyActivated(IBody3D body);
        public delegate void OnBodyDeactivated(IBody3D body);
        public delegate void OnContactAdded(IBody3D self, IBody3D other);
        public delegate void OnContactPersisted(IBody3D self, IBody3D other);

        private IPhysics3D impl;

        public static event OnBodyActivated onBodyActivated;
        public static event OnBodyDeactivated onBodyDeactivated;
        public static event OnContactAdded onContactAdded;
        public static event OnContactPersisted onContactPersisted;

        public bool Destroyed => impl?.Destroyed ?? true;

        public SubsystemType type => SubsystemType.FixedUpdate;

        public Vector3 Gravity
        {
            get => impl.Gravity;

            set => impl.Gravity = value;
        }

        public Physics3D(IPhysics3D impl)
        {
            this.impl = impl;
        }

        #region API
        public bool CreateBox(Entity entity, Vector3 extents, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            return impl.CreateBox(entity, extents, position, rotation, motionType, layer, out body);
        }

        public bool CreateSphere(Entity entity, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            return impl.CreateSphere(entity, radius, position, rotation, motionType, layer, out body);
        }

        public bool CreateCapsule(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            return impl.CreateCapsule(entity, height, radius, position, rotation, motionType, layer, out body);
        }

        public bool CreateCylinder(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            return impl.CreateCylinder(entity, height, radius, position, rotation, motionType, layer, out body);
        }

        public bool CreateMesh(Entity entity, Mesh mesh, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            return impl.CreateMesh(entity, mesh, position, rotation, motionType, layer, out body);
        }

        public void DestroyBody(IBody3D body)
        {
            impl.DestroyBody(body);
        }

        public void AddBody(IBody3D body, bool activated)
        {
            impl.AddBody(body, activated);
        }

        public void RemoveBody(IBody3D body)
        {
            impl.RemoveBody(body);
        }

        public bool RayCast(Ray ray, out IBody3D body, out float fraction)
        {
            return impl.RayCast(ray, out body, out fraction);
        }
        #endregion

        #region Internal
        public void Startup()
        {
        }

        public void Shutdown()
        {
            impl.Destroy();

            impl = null;
        }

        public void Update()
        {
            impl.Update(Time.fixedDeltaTime);
        }

        internal static void BodyActivated(IBody3D body)
        {
            onBodyActivated?.Invoke(body);
        }

        internal static void BodyDeactivated(IBody3D body)
        {
            onBodyDeactivated?.Invoke(body);
        }

        internal static void ContactAdded(IBody3D A, IBody3D B)
        {
            onContactAdded?.Invoke(A, B);
        }

        internal static void ContactPersisted(IBody3D A, IBody3D B)
        {
            onContactPersisted?.Invoke(A, B);
        }
        #endregion
    }
}
