using JoltPhysicsSharp;
using Staple.Internal;
using Staple.Utilities;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple.JoltPhysics;

/// <summary>
/// Implements Jolt Physics
/// </summary>
[AdditionalLibrary(AppPlatform.Android, "joltc")]
public class JoltPhysics3D : IPhysics3D
{
    public const float MinExtents = 0.2f;

    //Dependencies
    private BroadPhaseLayerInterface broadPhaseLayerInterface;
    private ObjectVsBroadPhaseLayerFilter objectVsBroadPhaseLayerFilter;
    private ObjectLayerPairFilter objectLayerPairFilter;
    private PhysicsSystem physicsSystem;

    //Tracking live bodies
    private readonly List<JoltBodyPair> bodies = new();

    private bool destroyed = false;

    private bool locked = false;
    private object threadLock = new();

    private CallbackGatherer callbackGatherer = new();

    public bool Destroyed => destroyed;

    public Vector3 Gravity
    {
        get => physicsSystem.Gravity;

        set => physicsSystem.Gravity = value;
    }

    public void Startup()
    {
        destroyed = false;

        if(JoltPhysicsSharp.Foundation.Init() == false)
        {
            throw new InvalidOperationException("[JoltPhysics] Failed to initialize Foundation");
        }

        try
        {
            JoltPhysicsSharp.Foundation.SetAssertFailureHandler((inExpression, inMessage, inFile, inLine) =>
            {
                var message = inMessage ?? inExpression;

                var outMessage = $"[JoltPhysics] Assertion failure at {inFile}:{inLine}: {message}";

                System.Diagnostics.Debug.WriteLine(outMessage);

                Log.Info(outMessage);

                throw new Exception(outMessage);
            });
        }
        catch(Exception)
        {
            Log.Error("[JoltPhysics] Failed to initialize assertion failure handler");
        }

        var table = new BroadPhaseLayerInterfaceTable((uint)LayerMask.AllLayers.Count, (uint)LayerMask.AllLayers.Count);

        broadPhaseLayerInterface = table;

        for(var i = 0; i < LayerMask.AllLayers.Count; i++)
        {
            table.MapObjectToBroadPhaseLayer(new ObjectLayer((ushort)i), new BroadPhaseLayer((byte)i));
        }

        var layerPair = new ObjectLayerPairFilterTable((uint)LayerMask.AllLayers.Count);

        for(var i = 0; i < LayerMask.AllLayers.Count; i++)
        {
            for(var j = 0; j < LayerMask.AllLayers.Count; j++)
            {
                if (ColliderMask.ShouldCollide(i, j))
                {
                    layerPair.EnableCollision(new ObjectLayer((ushort)i), new ObjectLayer((ushort)j));
                }
                else
                {
                    layerPair.DisableCollision(new ObjectLayer((ushort)i), new ObjectLayer((ushort)j));
                }
            }
        }

        objectLayerPairFilter = layerPair;

        objectVsBroadPhaseLayerFilter = new ObjectVsBroadPhaseLayerFilterTable(broadPhaseLayerInterface, (uint)LayerMask.AllLayers.Count,
            objectLayerPairFilter, (uint)LayerMask.AllLayers.Count);

        physicsSystem = new(new PhysicsSystemSettings()
        {
            BroadPhaseLayerInterface = broadPhaseLayerInterface,
            ObjectLayerPairFilter = objectLayerPairFilter,
            ObjectVsBroadPhaseLayerFilter = objectVsBroadPhaseLayerFilter,
        });

        physicsSystem.OnBodyActivated += OnBodyActivated;
        physicsSystem.OnBodyDeactivated += OnBodyDeactivated;
        physicsSystem.OnContactAdded += OnContactAdded;
        physicsSystem.OnContactRemoved += OnContactRemoved;
        physicsSystem.OnContactPersisted += OnContactPersisted;
        physicsSystem.OnContactValidate += OnContactValidate;
    }

    public void Shutdown()
    {
        if(destroyed)
        {
            return;
        }

        destroyed = true;

        JoltPhysicsSharp.Foundation.Shutdown();
    }

    #region Internal
    private bool TryFindBody(Body body, out IBody3D outBody)
    {
        lock (threadLock)
        {
            foreach (var b in bodies)
            {
                if (b.body == body)
                {
                    outBody = b;

                    return true;
                }
            }
        }

        outBody = default;

        return false;
    }

    private bool TryFindBody(BodyID body, out IBody3D outBody)
    {
        lock (threadLock)
        {
            foreach (var b in bodies)
            {
                if (b.body.ID == body)
                {
                    outBody = b;

                    return true;
                }
            }
        }

        outBody = default;

        return false;
    }

    private void OnContactAdded(PhysicsSystem system, in Body body1, in Body body2, in ContactManifold manifold, in ContactSettings settings)
    {
        lock (threadLock)
        {
            locked = true;
        }

        if (TryFindBody(body1, out var b1) && TryFindBody(body2, out var b2))
        {
            callbackGatherer.AddCallback(() =>
            {
                Physics3D.ContactAdded(b1, b2);
            });
        }

        lock (threadLock)
        {
            locked = false;
        }
    }

    private void OnContactPersisted(PhysicsSystem system, in Body body1, in Body body2, in ContactManifold manifold, in ContactSettings settings)
    {
        lock (threadLock)
        {
            locked = true;
        }

        if (TryFindBody(body1, out var b1) && TryFindBody(body2, out var b2))
        {
            callbackGatherer.AddCallback(() =>
            {
                Physics3D.ContactPersisted(b1, b2);
            });
        }

        lock (threadLock)
        {
            locked = false;
        }
    }

    private void OnContactRemoved(PhysicsSystem system, ref SubShapeIDPair subShapePair)
    {
        lock (threadLock)
        {
            locked = true;
        }

        if (TryFindBody(subShapePair.Body1ID, out var b1) &&
            TryFindBody(subShapePair.Body2ID, out var b2))
        {
            callbackGatherer.AddCallback(() =>
            {
                Physics3D.ContactRemoved(b1, b2);
            });
        }

        lock (threadLock)
        {
            locked = false;
        }
    }

    private ValidateResult OnContactValidate(PhysicsSystem system, in Body body1, in Body body2, Double3 baseOffset, IntPtr collisionResult)
    {
        lock (threadLock)
        {
            locked = true;
        }

        if (TryFindBody(body1, out var b1) && TryFindBody(body2, out var b2))
        {
            if(Physics3D.ContactValidate(b1, b2) == false)
            {
                lock (threadLock)
                {
                    locked = false;
                }

                return ValidateResult.RejectContact;
            }
        }

        lock (threadLock)
        {
            locked = false;
        }

        return ValidateResult.AcceptContact;
    }

    private void OnBodyActivated(PhysicsSystem system, in BodyID bodyID, ulong bodyUserData)
    {
        lock (threadLock)
        {
            locked = true;
        }

        if (TryFindBody(bodyID, out var body))
        {
            callbackGatherer.AddCallback(() =>
            {
                Physics3D.BodyActivated(body);
            });
        }

        lock (threadLock)
        {
            locked = false;
        }
    }

    private void OnBodyDeactivated(PhysicsSystem system, in BodyID bodyID, ulong bodyUserData)
    {
        lock (threadLock)
        {
            locked = true;
        }

        if (TryFindBody(bodyID, out var body))
        {
            callbackGatherer.AddCallback(() =>
            {
                Physics3D.BodyDeactivated(body);
            });
        }

        lock (threadLock)
        {
            locked = false;
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

        lock (threadLock)
        {
            foreach (var pair in bodies)
            {
                if (pair.entity.EnabledInHierarchy == false)
                {
                    if (pair.body.IsActive)
                    {
                        physicsSystem.BodyInterface.DeactivateBody(pair.body.ID);
                    }
                }
                else if (pair.body.IsActive == false)
                {
                    physicsSystem.BodyInterface.ActivateBody(pair.body.ID);
                }
            }
        }

        physicsSystem.Step(deltaTime, collisionSteps);

        callbackGatherer.PerformAll();

        lock(threadLock)
        {
            foreach (var pair in bodies)
            {
                var transform = pair.entity.GetComponent<Transform>();

                if (transform != null)
                {
                    var body = pair.body;

                    if(transform.Position != body.Position)
                    {
                        transform.Position = body.Position;
                    }

                    if(transform.Rotation != body.Rotation)
                    {
                        transform.Rotation = body.Rotation;
                    }
                }
            }
        }
    }

    private bool CreateBody(Entity entity, ShapeSettings settings, Vector3 position, Quaternion rotation, MotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        float mass, out IBody3D body)
    {
        lock (threadLock)
        {
            var creationSettings = new BodyCreationSettings(settings, position, rotation, motionType, new ObjectLayer(layer));

            var dofs = new List<AllowedDOFs>
            {
                AllowedDOFs.TranslationX,
                AllowedDOFs.TranslationY
            };

            if (freezeX == false)
            {
                dofs.Add(AllowedDOFs.RotationX);
            }

            if (freezeY == false)
            {
                dofs.Add(AllowedDOFs.RotationY);
            }

            if (freezeZ == false)
            {
                dofs.Add(AllowedDOFs.RotationZ);
            }

            if (is2DPlane == false)
            {
                dofs.Add(AllowedDOFs.TranslationZ);
            }

            AllowedDOFs dof = 0;

            foreach (var d in dofs)
            {
                dof |= d;
            }

            creationSettings.AllowedDOFs = dof;

            Body b;

            if (locked)
            {
                b = physicsSystem.BodyInterfaceNoLock.CreateBody(creationSettings);
            }
            else
            {
                b = physicsSystem.BodyInterface.CreateBody(creationSettings);
            }

            if (b.Handle != nint.Zero)
            {
                if(motionType != MotionType.Static)
                {
                    b.MotionProperties.SetMassProperties(dof, new MassProperties()
                    {
                        Mass = mass,
                    });
                }

                var pair = new JoltBodyPair()
                {
                    body = b,
                    entity = entity,
                };

                bodies.Add(pair);

                body = pair;

                pair.IsTrigger = isTrigger;
                pair.GravityFactor = gravityFactor;
                pair.Friction = friction;
                pair.Restitution = restitution;

                AddBody(body, true);

                return true;
            }

            body = default;

            return false;
        }
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

    public bool CreateHeightMap(Entity entity, float[] heights, Vector3 offset, Vector3 scale, Vector3 position, Quaternion rotation,
        ushort layer, float friction, float restitution, float mass, out IBody3D body)
    {
        if(heights != null &&
            heights.Length % 2 == 0 &&
            Math.Sqrt(heights.Length) > 0 &&
            heights.Length > 0)
        {
            unsafe
            {
                fixed(float *ptr = heights)
                {
                    return CreateBody(entity, new HeightFieldShapeSettings(ptr, offset, scale, (int)Math.Sqrt(heights.Length)),
                        position, rotation, MotionType.Static, layer, false, 0, friction, restitution, true, true, true, false,
                        mass, out body);
                }
            }
        }

        body = default;

        return false;
    }

    public bool CreateBox(Entity entity, Vector3 extents, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        float mass, out IBody3D body)
    {
        if(extents.X < MinExtents)
        {
            extents.X = MinExtents;
        }

        if (extents.Y < MinExtents)
        {
            extents.Y = MinExtents;
        }

        if (extents.Z < MinExtents)
        {
            extents.Z = MinExtents;
        }

        return CreateBody(entity, new BoxShapeSettings(extents / 2), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    public bool CreateSphere(Entity entity, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        float mass, out IBody3D body)
    {
        if(radius <= 0)
        {
            throw new ArgumentException("Radius must be bigger than 0");
        }

        return CreateBody(entity, new SphereShapeSettings(radius), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    public bool CreateCapsule(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ,
        bool is2DPlane, float mass, out IBody3D body)
    {
        if(radius <= 0)
        {
            throw new ArgumentException("Radius must be bigger than 0");
        }

        if(height <= 0)
        {
            throw new ArgumentException("Height must be bigger than 0");
        }

        return CreateBody(entity, new CapsuleShapeSettings(height / 2, radius), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    public bool CreateCylinder(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ,
        bool is2DPlane, float mass, out IBody3D body)
    {
        if(radius <= 0)
        {
            throw new ArgumentException("Radius must be bigger than 0");
        }

        if(height <= 0)
        {
            throw new ArgumentException("Height must be bigger than 0");
        }

        return CreateBody(entity, new CylinderShapeSettings(height / 2, radius), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    public bool CreateMesh(Entity entity, Mesh mesh, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        float mass, out IBody3D body)
    {
        if(mesh is null)
        {
            throw new NullReferenceException("Mesh is null");
        }

        if(mesh.isReadable == false)
        {
            throw new ArgumentException("Mesh is not readable", nameof(mesh));
        }

        if(mesh.IndexCount % 3 != 0)
        {
            throw new ArgumentException("Mesh doesn't have valid index count (should be multiple of 3)", nameof(mesh));
        }

        if((mesh.Vertices?.Length ?? 0) == 0)
        {
            throw new ArgumentException("Mesh doesn't have vertices", nameof(mesh));
        }

        if((mesh.Indices?.Length ?? 0) == 0)
        {
            throw new ArgumentException("Mesh doesn't have indices", nameof(mesh));
        }

        MeshShapeSettings settings;

        unsafe
        {
            var triangles = new List<IndexedTriangle>();
            var indices = mesh.Indices;

            for (var i = 0; i < mesh.IndexCount; i += 3)
            {
                triangles.Add(new IndexedTriangle(indices[i],
                    indices[i + 1],
                    indices[i + 2]));
            }

            settings = new MeshShapeSettings(mesh.Vertices, triangles.ToArray());
        }

        return CreateBody(entity, settings, position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor, friction, restitution,
            freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    public bool CreateMesh(Entity entity, ReadOnlySpan<Triangle> triangles, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        float mass, out IBody3D body)
    {
        return CreateBody(entity, new MeshShapeSettings(triangles), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    public IBody3D CreateBody(Entity entity, World world)
    {
        if(world.TryGetComponent<RigidBody3D>(entity, out var rigidBody) == false)
        {
            Log.Debug($"[Physics3D] Failed to create body for entity {world.GetEntityName(entity)}: No RigidBody3D component found");

            return null;
        }

        if (world.TryGetComponent<Transform>(entity, out var transform) == false)
        {
            Log.Debug($"[Physics3D] Failed to create body for entity {world.GetEntityName(entity)}: No Transform component found");

            return null;
        }

        var compound = new MutableCompoundShapeSettings();

        var any = false;

        if(world.TryGetComponent<HeightMapCollider3D>(entity, out var heightMap) &&
            heightMap.heights != null &&
            heightMap.heights.Length % 2 == 0 &&
            Math.Sqrt(heightMap.heights.Length) > 0 &&
            heightMap.heights.Length > 0)
        {
            any = true;

            unsafe
            {
                fixed(float *ptr = heightMap.heights)
                {
                    compound.AddShape(heightMap.position, heightMap.rotation,
                        new HeightFieldShapeSettings(ptr, heightMap.offset, heightMap.scale, (int)Math.Sqrt(heightMap.heights.Length)));
                }
            }
        }

        if(world.TryGetComponent<BoxCollider3D>(entity, out var boxCollider))
        {
            any = true;

            var extents = boxCollider.size * transform.Scale;

            if(extents.X <= MinExtents)
            {
                extents.X = MinExtents;
            }

            if (extents.Y <= MinExtents)
            {
                extents.Y = MinExtents;
            }

            if (extents.Z <= MinExtents)
            {
                extents.Z = MinExtents;
            }

            compound.AddShape(boxCollider.position, boxCollider.rotation, new BoxShapeSettings(extents / 2));
        }

        if(world.TryGetComponent<SphereCollider3D>(entity, out var sphereCollider))
        {
            any = true;

            var radius = sphereCollider.radius * transform.Scale.X;

            if(radius <= 0)
            {
                throw new ArgumentException($"SphereCollider3D {world.GetEntityName(entity)} Radius must be bigger than 0");
            }

            compound.AddShape(sphereCollider.position, sphereCollider.rotation, new SphereShapeSettings(radius));
        }

        if(world.TryGetComponent<CapsuleCollider3D>(entity, out var capsuleCollider))
        {
            any = true;

            var radius = capsuleCollider.radius * transform.Scale.X;
            var height = capsuleCollider.height * transform.Scale.Y;

            if(radius <= 0)
            {
                throw new ArgumentException($"CapsuleCollider3D {world.GetEntityName(entity)}  Radius must be bigger than 0");
            }

            if (height <= 0)
            {
                throw new ArgumentException($"CapsuleCollider3D {world.GetEntityName(entity)} Height must be bigger than 0");
            }

            compound.AddShape(capsuleCollider.position, capsuleCollider.rotation, new CapsuleShapeSettings(height / 2, radius));
        }

        if(world.TryGetComponent<CylinderCollider3D>(entity, out var cylinderCollider))
        {
            any = true;

            var radius = cylinderCollider.radius * transform.Scale.X;
            var height = cylinderCollider.height * transform.Scale.Y;

            if (radius <= 0)
            {
                throw new ArgumentException($"CylinderCollider3D {world.GetEntityName(entity)} Radius must be bigger than 0");
            }

            if (height <= 0)
            {
                throw new ArgumentException($"CylinderCollider3D {world.GetEntityName(entity)} Height must be bigger than 0");
            }

            compound.AddShape(cylinderCollider.position, cylinderCollider.rotation, new CylinderShapeSettings(height / 2, radius));
        }

        if(world.TryGetComponent<MeshCollider3D>(entity, out var meshCollider))
        {
            any = true;

            var mesh = meshCollider.mesh;

            if (mesh is null)
            {
                throw new NullReferenceException($"MeshCollider3D {world.GetEntityName(entity)} Mesh is null");
            }

            if (mesh.isReadable == false)
            {
                throw new ArgumentException($"MeshCollider3D {world.GetEntityName(entity)} Mesh is not readable", nameof(mesh));
            }

            if (mesh.IndexCount % 3 != 0)
            {
                throw new ArgumentException($"MeshCollider3D {world.GetEntityName(entity)} Mesh doesn't have valid index count (should be multiple of 3)", nameof(mesh));
            }

            if ((mesh.Vertices?.Length ?? 0) == 0)
            {
                throw new ArgumentException($"MeshCollider3D {world.GetEntityName(entity)} Mesh doesn't have vertices", nameof(mesh));
            }

            if ((mesh.Indices?.Length ?? 0) == 0)
            {
                throw new ArgumentException($"MeshCollider3D {world.GetEntityName(entity)} Mesh doesn't have indices", nameof(mesh));
            }

            MeshShapeSettings settings;

            var triangles = new List<IndexedTriangle>();
            var indices = mesh.Indices;

            for (var i = 0; i < mesh.IndexCount; i += 3)
            {
                triangles.Add(new IndexedTriangle(indices[i],
                    indices[i + 1],
                    indices[i + 2]));
            }

            settings = new MeshShapeSettings(mesh.Vertices, triangles.ToArray());

            compound.AddShape(meshCollider.position, meshCollider.rotation, settings);
        }

        if(any == false)
        {
            Log.Error($"[Physics3D] Rigid Body for entity {world.GetEntityName(entity)} has no attached colliders, ignoring...");

            return null;
        }

        if(CreateBody(entity, compound, transform.Position, transform.Rotation, GetMotionType(rigidBody.motionType),
            (ushort)world.GetEntityLayer(entity), rigidBody.isTrigger, rigidBody.gravityFactor, rigidBody.friction,
            rigidBody.restitution, rigidBody.freezeRotationX, rigidBody.freezeRotationY, rigidBody.freezeRotationZ,
            rigidBody.is2DPlane, rigidBody.mass, out var body))
        {
            return body;
        }
        else
        {
            Log.Error($"[Physics3D] Failed to create body for entity {world.GetEntityName(entity)}");
        }

        return null;
    }

    public void DestroyBody(IBody3D body)
    {
        if(body is JoltBodyPair pair)
        {
            lock (threadLock)
            {
                if (locked)
                {
                    physicsSystem.BodyInterfaceNoLock.RemoveBody(pair.body.ID);
                    physicsSystem.BodyInterfaceNoLock.DestroyBody(pair.body.ID);
                }
                else
                {
                    physicsSystem.BodyInterface.RemoveBody(pair.body.ID);
                    physicsSystem.BodyInterface.DestroyBody(pair.body.ID);
                }

                bodies.Remove(pair);
            }
        }
    }

    public void AddBody(IBody3D body, bool activated)
    {
        if(body is JoltBodyPair pair)
        {
            lock (threadLock)
            {
                if (locked)
                {
                    physicsSystem.BodyInterfaceNoLock.AddBody(pair.body, activated ? Activation.Activate : Activation.DontActivate);
                }
                else
                {
                    physicsSystem.BodyInterface.AddBody(pair.body, activated ? Activation.Activate : Activation.DontActivate);
                }
            }
        }
    }

    public void RemoveBody(IBody3D body)
    {
        if (body is JoltBodyPair pair)
        {
            lock (threadLock)
            {
                if (locked)
                {
                    physicsSystem.BodyInterfaceNoLock.RemoveBody(pair.body.ID);
                }
                else
                {
                    physicsSystem.BodyInterface.RemoveBody(pair.body.ID);
                }
            }
        }
    }

    public bool RayCast(Ray ray, out IBody3D body, out float fraction, LayerMask layerMask, PhysicsTriggerQuery triggerQuery, float maxDistance)
    {
        var hit = RayCastResult.Default;

        var broadPhaseFilter = new JoltPhysicsBroadPhaseLayerFilter();

        var objectLayerFilter = new JoltPhysicsObjectLayerFilter()
        {
            layerMask = new()
            {
                value = layerMask.value,
            }
        };

        var bodyFilter = new JoltPhysicsBodyFilter()
        {
            triggerQuery = triggerQuery,
        };

        var result = false;

        lock (threadLock)
        {
            if(locked)
            {
                result = physicsSystem.NarrowPhaseQueryNoLock.CastRay(ray.position, ray.direction * maxDistance, out hit,
                    broadPhaseFilter, objectLayerFilter, bodyFilter);
            }
            else
            {
                result = physicsSystem.NarrowPhaseQuery.CastRay(ray.position, ray.direction * maxDistance, out hit,
                    broadPhaseFilter, objectLayerFilter, bodyFilter);
            }
        }

        if (result)
        {
            if (TryFindBody(hit.BodyID, out body))
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
            lock (threadLock)
            {
                if (locked)
                {
                    return physicsSystem.BodyInterfaceNoLock.GetGravityFactor(pair.body.ID);
                }
                else
                {
                    return physicsSystem.BodyInterface.GetGravityFactor(pair.body.ID);
                }
            }
        }

        return 0;
    }

    public void SetGravityFactor(IBody3D body, float factor)
    {
        if(body is JoltBodyPair pair)
        {
            lock (threadLock)
            {
                if (locked)
                {
                    physicsSystem.BodyInterface.SetGravityFactor(pair.body.ID, factor);
                }
                else
                {
                    physicsSystem.BodyInterfaceNoLock.SetGravityFactor(pair.body.ID, factor);
                }
            }
        }
    }

    public void SetBodyPosition(IBody3D body, Vector3 newPosition)
    {
        if(body is JoltBodyPair pair)
        {
            lock (threadLock)
            {
                if (locked)
                {
                    physicsSystem.BodyInterfaceNoLock.SetPosition(pair.body.ID, newPosition, pair.body.IsActive ? Activation.Activate : Activation.DontActivate);
                }
                else
                {
                    physicsSystem.BodyInterface.SetPosition(pair.body.ID, newPosition, pair.body.IsActive ? Activation.Activate : Activation.DontActivate);
                }
            }
        }
    }

    public void SetBodyRotation(IBody3D body, Quaternion newRotation)
    {
        if (body is JoltBodyPair pair)
        {
            lock(threadLock)
            {
                if(locked)
                {
                    physicsSystem.BodyInterfaceNoLock.SetRotation(pair.body.ID, newRotation, pair.body.IsActive ? Activation.Activate : Activation.DontActivate);
                }
                else
                {
                    physicsSystem.BodyInterface.SetRotation(pair.body.ID, newRotation, pair.body.IsActive ? Activation.Activate : Activation.DontActivate);
                }
            }
        }
    }

    public void SetBodyTrigger(IBody3D body, bool value)
    {
        if (body is JoltBodyPair pair)
        {
            pair.body.IsSensor = value;
        }
    }

    public IBody3D GetBody(Entity entity)
    {
        lock (threadLock)
        {
            foreach (var body in bodies)
            {
                if (body is JoltBodyPair pair && pair.entity == entity)
                {
                    return body;
                }
            }
        }

        return null;
    }

    public IBody3D GetBody(BodyID bodyID)
    {
        lock (threadLock)
        {
            foreach (var body in bodies)
            {
                if (body is JoltBodyPair pair && pair.body.ID == bodyID)
                {
                    return body;
                }
            }
        }

        return null;
    }

    public void AddForce(IBody3D body, Vector3 force)
    {
        if(body is not JoltBodyPair pair)
        {
            return;
        }

        pair.body.AddForce(force);
    }

    public void AddImpulse(IBody3D body, Vector3 impulse)
    {
        if (body is not JoltBodyPair pair)
        {
            return;
        }

        pair.body.AddImpulse(impulse);
    }

    public void AddAngularImpulse(IBody3D body, Vector3 impulse)
    {
        if (body is not JoltBodyPair pair)
        {
            return;
        }

        pair.body.AddAngularImpulse(impulse);
    }
}
