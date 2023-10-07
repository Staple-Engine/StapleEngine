using JoltPhysicsSharp;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Implements Jolt Physics
    /// </summary>
    internal class JoltPhysics3D : IPhysics3D
    {
        private const int AllocatorSize = 10 * 1024 * 1024;

        private const uint MaxBodies = 1024;

        private const uint NumBodyMutexes = 0;
        private const uint MaxBodyPairs = 1024;
        private const uint MaxContactConstraints = 1024;

        //Dependencies
        private TempAllocator allocator;
        private JobSystemThreadPool jobThreadPool;
        private BroadPhaseLayerInterface broadPhaseLayerInterface;
        private ObjectVsBroadPhaseLayerFilter objectVsBroadPhaseLayerFilter;
        private ObjectLayerPairFilter objectLayerPairFilter;
        private PhysicsSystem physicsSystem;

        //Tracking live bodies
        private List<JoltBodyPair> bodies = new List<JoltBodyPair>();

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

            try
            {
                Foundation.SetAssertFailureHandler((inExpression, inMessage, inFile, inLine) =>
                {
                    var message = inMessage ?? inExpression;

                    var outMessage = $"[JoltPhysics] Assertion failure at {inFile}:{inLine}: {message}";

                    System.Diagnostics.Debug.WriteLine(outMessage);

                    Log.Info(outMessage);

                    return true;
                });
            }
            catch(Exception)
            {
                Log.Error("[JoltPhysics] Failed to initialize assertion failure handler");
            }

            allocator = new(AllocatorSize);
            jobThreadPool = new(Foundation.MaxPhysicsJobs, Foundation.MaxPhysicsBarriers);
            broadPhaseLayerInterface = new JoltBroadPhaseLayerInterface();
            objectVsBroadPhaseLayerFilter = new JoltObjectVsBroadPhaseLayerFilter();
            objectLayerPairFilter = new JoltObjectLayerPairFilter();

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

            foreach (var pair in bodies)
            {
                if(Scene.current.world.IsEntityEnabled(pair.entity) == false)
                {
                    if(pair.body.IsActive)
                    {
                        physicsSystem.BodyInterface.DeactivateBody(pair.body.ID);
                    }
                }
                else if(pair.body.IsActive == false)
                {
                    physicsSystem.BodyInterface.ActivateBody(pair.body.ID);
                }
            }

            physicsSystem.Update(deltaTime, collisionSteps, allocator, jobThreadPool);

            foreach(var pair in bodies)
            {
                var transform = Scene.current.world.GetComponent<Transform>(pair.entity);

                if(transform != null)
                {
                    var body = pair.body;

                    transform.Position = body.Position;
                    transform.LocalRotation = body.Rotation;
                }
            }
        }

        private bool CreateBody(Entity entity, ShapeSettings settings, Vector3 position, Quaternion rotation, MotionType motionType, ushort layer,
            bool isTrigger, float gravityFactor, out IBody3D body)
        {
            var b = physicsSystem.BodyInterface.CreateBody(new BodyCreationSettings(settings, position, rotation, motionType, new ObjectLayer(layer)));

            if (b != null)
            {
                var pair = new JoltBodyPair()
                {
                    body = b,
                    entity = entity,
                };

                bodies.Add(pair);

                body = pair;

                pair.IsTrigger = isTrigger;
                pair.GravityFactor = gravityFactor;

                AddBody(body, true);

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

        public bool CreateBox(Entity entity, Vector3 extents, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
            bool isTrigger, float gravityFactor, out IBody3D body)
        {
            if(extents.X <= 0 || extents.Y <= 0 || extents.Z <= 0)
            {
                throw new ArgumentException("Extents must be bigger than 0");
            }

            return CreateBody(entity, new BoxShapeSettings(extents / 2), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor, out body);
        }

        public bool CreateSphere(Entity entity, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
            bool isTrigger, float gravityFactor, out IBody3D body)
        {
            if (radius <= 0)
            {
                throw new ArgumentException("Radius must be bigger than 0");
            }

            return CreateBody(entity, new SphereShapeSettings(radius), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor, out body);
        }

        public bool CreateCapsule(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType,
            ushort layer, bool isTrigger, float gravityFactor, out IBody3D body)
        {
            if (radius <= 0)
            {
                throw new ArgumentException("Radius must be bigger than 0");
            }

            if (height <= 0)
            {
                throw new ArgumentException("Height must be bigger than 0");
            }

            return CreateBody(entity, new CapsuleShapeSettings(height / 2, radius), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor, out body);
        }

        public bool CreateCylinder(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType,
            ushort layer, bool isTrigger, float gravityFactor, out IBody3D body)
        {
            if (radius <= 0)
            {
                throw new ArgumentException("Radius must be bigger than 0");
            }

            if (height <= 0)
            {
                throw new ArgumentException("Height must be bigger than 0");
            }

            return CreateBody(entity, new CylinderShapeSettings(height / 2, radius), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor, out body);
        }

        public bool CreateMesh(Entity entity, Mesh mesh, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
            bool isTrigger, float gravityFactor, out IBody3D body)
        {
            if(ReferenceEquals(mesh, null))
            {
                throw new NullReferenceException("Mesh is null");
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

            return CreateBody(entity, settings, position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor, out body);
        }

        public bool CreateMesh(Entity entity, ReadOnlySpan<Triangle> triangles, Vector3 position, Quaternion rotation, BodyMotionType motionType,
            ushort layer, bool isTrigger, float gravityFactor, out IBody3D body)
        {
            return CreateBody(entity, new MeshShapeSettings(triangles), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor, out body);
        }

        public void DestroyBody(IBody3D body)
        {
            if(body is JoltBodyPair pair)
            {
                physicsSystem.BodyInterface.DestroyBody(pair.body.ID);

                bodies.Remove(pair);
            }
        }

        public void AddBody(IBody3D body, bool activated)
        {
            if(body is JoltBodyPair pair)
            {
                physicsSystem.BodyInterface.AddBody(pair.body, activated ? Activation.Activate : Activation.DontActivate);
            }
        }

        public void RemoveBody(IBody3D body)
        {
            if (body is JoltBodyPair pair)
            {
                physicsSystem.BodyInterface.RemoveBody(pair.body.ID);
            }
        }

        public bool RayCast(Ray ray, out IBody3D body, out float fraction, PhysicsTriggerQuery triggerQuery, float maxDistance)
        {
            var hit = RayCastResult.Default;

            var broadPhaseFilter = new JoltPhysicsBroadPhaseLayerFilter();

            var objectLayerFilter = new JoltPhysicsObjectLayerFilter();

            var bodyFilter = new JoltPhysicsBodyFilter()
            {
                triggerQuery = triggerQuery,
            };

            if (physicsSystem.NarrowPhaseQuery.CastRay((Double3)ray.position, ray.direction * maxDistance, ref hit, broadPhaseFilter, objectLayerFilter, bodyFilter))
            {
                if(TryFindBody(hit.BodyID, out body))
                {
                    fraction = hit.Fraction;

                    return true;
                }
            }

            body = default;
            fraction = default;

            return false;
        }

        public float GravityFactor(IBody3D body)
        {
            if(body is JoltBodyPair pair)
            {
                return physicsSystem.BodyInterfaceNoLock.GetGravityFactor(pair.body.ID);
            }

            return 0;
        }

        public void SetGravityFactor(IBody3D body, float factor)
        {
            if(body is JoltBodyPair pair)
            {
                physicsSystem.BodyInterface.SetGravityFactor(pair.body.ID, factor);
            }
        }

        public void SetBodyPosition(IBody3D body, Vector3 newPosition)
        {
            if(body is JoltBodyPair pair)
            {
                physicsSystem.BodyInterface.SetPosition(pair.body.ID, (Double3)newPosition, pair.body.IsActive ? Activation.Activate : Activation.DontActivate);
            }
        }

        public void SetBodyRotation(IBody3D body, Quaternion newRotation)
        {
            if (body is JoltBodyPair pair)
            {
                physicsSystem.BodyInterface.SetRotation(pair.body.ID, newRotation, pair.body.IsActive ? Activation.Activate : Activation.DontActivate);
            }
        }

        public void SetBodyTrigger(IBody3D body, bool value)
        {
            if (body is JoltBodyPair pair)
            {
                pair.body.IsSensor = value;
            }
        }
    }
}
