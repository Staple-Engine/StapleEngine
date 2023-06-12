using JoltPhysicsSharp;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple
{
    internal class JoltPhysics3D : IPhysics3D
    {
        private const int AllocatorSize = 10 * 1024 * 1024;

        private const uint MaxBodies = 1024;

        private const uint NumBodyMutexes = 0;
        private const uint MaxBodyPairs = 1024;
        private const uint MaxContactConstraints = 1024;

        private class StapleBoardPhaseLayerInterface : BroadPhaseLayerInterface
        {
            protected override BroadPhaseLayer GetBroadPhaseLayer(ObjectLayer layer)
            {
                if(layer < LayerMask.AllLayers.Count)
                {
                    return new BroadPhaseLayer();
                }

                return new BroadPhaseLayer((byte)layer.Value);
            }

            protected override string GetBroadPhaseLayerName(BroadPhaseLayer layer)
            {
                return LayerMask.LayerToName(layer.Value);
            }

            protected override int GetNumBroadPhaseLayers()
            {
                return LayerMask.AllLayers.Count;
            }
        }

        private class StapleObjectVsBroadPhaseLayerFilter : ObjectVsBroadPhaseLayerFilter
        {
            protected override bool ShouldCollide(ObjectLayer layer1, BroadPhaseLayer layer2)
            {
                return ColliderMask.ShouldCollide(layer1, layer2);
            }
        }

        private class StapleObjectLayerPairFilter : ObjectLayerPairFilter
        {
            protected override bool ShouldCollide(ObjectLayer object1, ObjectLayer object2)
            {
                return ColliderMask.ShouldCollide(object1, object2);
            }
        }

        private class BodyEntityPair : IBody3D
        {
            public Entity entity;
            public Body body;

            public Entity Entity => entity;

            public Vector3 Position => body.CenterOfMassPosition;

            public Vector3 Velocity => body.GetLinearVelocity();

            public Vector3 AngularVelocity => body.GetAngularVelocity();

            public BodyMotionType MotionType
            {
                get
                {
                    switch(body.MotionType)
                    {
                        case JoltPhysicsSharp.MotionType.Static:

                            return BodyMotionType.Static;

                        case JoltPhysicsSharp.MotionType.Dynamic:

                            return BodyMotionType.Dynamic;

                        case JoltPhysicsSharp.MotionType.Kinematic:

                            return BodyMotionType.Kinematic;
                    }

                    throw new InvalidOperationException("Invalid Body Motion Type");
                }
            }
        }

        private TempAllocator allocator;
        private JobSystemThreadPool jobThreadPool;
        private BroadPhaseLayerInterface broadPhaseLayerInterface;
        private ObjectVsBroadPhaseLayerFilter objectVsBroadPhaseLayerFilter;
        private ObjectLayerPairFilter objectLayerPairFilter;
        private PhysicsSystem physicsSystem;

        private List<BodyEntityPair> bodies = new List<BodyEntityPair>();

        public BodyInterface BodyInterface => physicsSystem.BodyInterface;

        private bool destroyed = false;

        public bool Destroyed => destroyed;

        public Vector3 Gravity
        {
            get => physicsSystem.Gravity;

            set => physicsSystem.Gravity = value;
        }

        public JoltPhysics3D()
        {
            if(Foundation.Init() == false)
            {
                throw new InvalidOperationException("[JoltPhysics] Failed to initialize Foundation");
            }

            allocator = new(AllocatorSize);
            jobThreadPool = new(Foundation.MaxPhysicsJobs, Foundation.MaxPhysicsBarriers);
            broadPhaseLayerInterface = new StapleBoardPhaseLayerInterface();
            objectVsBroadPhaseLayerFilter = new StapleObjectVsBroadPhaseLayerFilter();
            objectLayerPairFilter = new StapleObjectLayerPairFilter();

            physicsSystem = new();

            physicsSystem.Init(MaxBodies,
                NumBodyMutexes,
                MaxBodyPairs,
                MaxContactConstraints,
                broadPhaseLayerInterface,
                objectVsBroadPhaseLayerFilter,
                objectLayerPairFilter);

            physicsSystem.OnBodyActivated += OnBodyActivated;
            physicsSystem.OnBodyDeactivated += OnBodyDeactivated;
            physicsSystem.OnContactAdded += OnContactAdded;
            physicsSystem.OnContactRemoved += OnContactRemoved;
            physicsSystem.OnContactPersisted += OnContactPersisted;
        }

        public void Destroy()
        {
            if(destroyed)
            {
                return;
            }

            destroyed = true;

            Foundation.Shutdown();
        }

        #region Internal
        private bool TryFindBody(Body body, out IBody3D outBody)
        {
            foreach (var b in bodies)
            {
                if (b.body == body)
                {
                    outBody = b;

                    return true;
                }
            }

            outBody = default;

            return false;
        }

        private bool TryFindBody(BodyID body, out IBody3D outBody)
        {
            foreach (var b in bodies)
            {
                if (b.body.ID == body)
                {
                    outBody = b;

                    return true;
                }
            }

            outBody = default;

            return false;
        }

        private void OnContactAdded(PhysicsSystem system, in Body body1, in Body body2)
        {
            if (TryFindBody(body1, out var b1) && TryFindBody(body2, out var b2))
            {
                Physics3D.ContactAdded(b1, b2);
            }
        }

        private void OnContactPersisted(PhysicsSystem system, in Body body1, in Body body2)
        {
            if (TryFindBody(body1, out var b1) && TryFindBody(body2, out var b2))
            {
                Physics3D.ContactPersisted(b1, b2);
            }
        }

        private void OnContactRemoved(PhysicsSystem system, ref SubShapeIDPair subShapePair)
        {
        }

        private void OnBodyActivated(PhysicsSystem system, in BodyID bodyID, ulong bodyUserData)
        {
            if (TryFindBody(bodyID, out var body))
            {
                Physics3D.BodyActivated(body);
            }
        }

        private void OnBodyDeactivated(PhysicsSystem system, in BodyID bodyID, ulong bodyUserData)
        {
            if (TryFindBody(bodyID, out var body))
            {
                Physics3D.BodyActivated(body);
            }
        }
        #endregion

        public void Update(float deltaTime)
        {
            if(destroyed)
            {
                return;
            }

            var collisionSteps = Math.CeilToInt(deltaTime / (1 / 60.0f));

            physicsSystem.Update(deltaTime, collisionSteps, 1, allocator, jobThreadPool);
        }

        private bool CreateBody(Entity entity, ShapeSettings settings, Vector3 position, Quaternion rotation, MotionType motionType, ushort layer, out IBody3D body)
        {
            var b = BodyInterface.CreateBody(new BodyCreationSettings(settings, position, rotation, motionType, new ObjectLayer(layer)));

            if (b != null)
            {
                var pair = new BodyEntityPair()
                {
                    body = b,
                    entity = entity,
                };

                bodies.Add(pair);

                body = pair;

                return true;
            }

            body = default;

            return false;
        }

        private static MotionType GetMotionType(BodyMotionType motionType)
        {
            return motionType switch
            {
                BodyMotionType.Static => MotionType.Static,
                BodyMotionType.Kinematic => MotionType.Kinematic,
                BodyMotionType.Dynamic => MotionType.Dynamic,
                _ => throw new ArgumentException("Invalid motion type", nameof(motionType)),
            };
        }

        public bool CreateBox(Entity entity, Vector3 extents, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            return CreateBody(entity, new BoxShapeSettings(extents / 2), position, rotation, GetMotionType(motionType), layer, out body);
        }

        public bool CreateSphere(Entity entity, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            return CreateBody(entity, new SphereShapeSettings(radius), position, rotation, GetMotionType(motionType), layer, out body);
        }

        public bool CreateCapsule(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            return CreateBody(entity, new CapsuleShapeSettings(height / 2, radius), position, rotation, GetMotionType(motionType), layer, out body);
        }

        public bool CreateCylinder(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            return CreateBody(entity, new CylinderShapeSettings(height / 2, radius), position, rotation, GetMotionType(motionType), layer, out body);
        }

        public bool CreateMesh(Entity entity, Mesh mesh, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            if(ReferenceEquals(mesh, null))
            {
                throw new NullReferenceException($"Mesh is null");
            }

            if(mesh.isReadable == false)
            {
                throw new ArgumentException("Mesh is not readable", nameof(mesh));
            }

            if (mesh.IndexCount % 3 != 0)
            {
                throw new ArgumentException("Mesh doesn't have valid index count (should be multiple of 3)", nameof(mesh));
            }

            if((mesh.vertices?.Length ?? 0) == 0)
            {
                throw new ArgumentException("Mesh doesn't have vertices", nameof(mesh));
            }

            if ((mesh.indices?.Length ?? 0) == 0)
            {
                throw new ArgumentException("Mesh doesn't have indices", nameof(mesh));
            }

            MeshShapeSettings settings;

            unsafe
            {
                var triangles = new List<IndexedTriangle>();
                var indices = mesh.indices;

                for (var i = 0; i < mesh.IndexCount; i += 3)
                {
                    triangles.Add(new IndexedTriangle(indices[i],
                        indices[i + 1],
                        indices[i + 2]));
                }

                settings = new MeshShapeSettings(mesh.vertices, triangles.ToArray());
            }

            return CreateBody(entity, settings, position, rotation, GetMotionType(motionType), layer, out body);
        }

        public bool CreateMesh(Entity entity, ReadOnlySpan<Triangle> triangles, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer, out IBody3D body)
        {
            return CreateBody(entity, new MeshShapeSettings(triangles), position, rotation, GetMotionType(motionType), layer, out body);
        }

        public void DestroyBody(IBody3D body)
        {
            if(body is BodyEntityPair pair)
            {
                BodyInterface.DestroyBody(pair.body.ID);

                bodies.Remove(pair);
            }
        }

        public void AddBody(IBody3D body, bool activated)
        {
            if(body is BodyEntityPair pair)
            {
                BodyInterface.AddBody(pair.body, activated ? ActivationMode.Activate : ActivationMode.DontActivate);
            }
        }

        public void RemoveBody(IBody3D body)
        {
            if (body is BodyEntityPair pair)
            {
                BodyInterface.RemoveBody(pair.body.ID);
            }
        }

        public void SetBodyMotion(IBody3D body, BodyMotionType motionType)
        {
            if(body is BodyEntityPair pair)
            {
                BodyInterface.SetMotionType(pair.body.ID, GetMotionType(motionType), pair.body.IsActive ? ActivationMode.Activate : ActivationMode.DontActivate);
            }
        }
    }
}
