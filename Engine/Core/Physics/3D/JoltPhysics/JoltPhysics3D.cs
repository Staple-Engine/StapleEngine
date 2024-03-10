using JoltPhysicsSharp;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Staple;

/// <summary>
/// Implements Jolt Physics
/// </summary>
[AdditionalLibrary(AppPlatform.Android, "joltc")]
internal class JoltPhysics3D : IPhysics3D
{
    public const float MinExtents = 0.2f;

    private const int AllocatorSize = 10 * 1024 * 1024;

    private const uint MaxBodies = 1024;

    private const uint NumBodyMutexes = 0;
    private const uint MaxBodyPairs = 1024;
    private const uint MaxContactConstraints = 1024;

    //Dependencies
    private readonly BroadPhaseLayerInterface broadPhaseLayerInterface;
    private readonly ObjectVsBroadPhaseLayerFilter objectVsBroadPhaseLayerFilter;
    private readonly ObjectLayerPairFilter objectLayerPairFilter;
    private readonly PhysicsSystem physicsSystem;

    //Tracking live bodies
    private readonly List<JoltBodyPair> bodies = new();

    private bool destroyed = false;

    public bool Destroyed => destroyed;

    public Vector3 Gravity
    {
        get => physicsSystem.Gravity;

        set => physicsSystem.Gravity = value;
    }

    public JoltPhysics3D()
    {
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

                return true;
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

    public void Destroy()
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
        if(TryFindBody(subShapePair.Body1ID, out var b1) &&
            TryFindBody(subShapePair.Body2ID, out var b2))
        {
            Physics3D.ContactRemoved(b1, b2);
        }
    }

    private ValidateResult OnContactValidate(PhysicsSystem system, in Body body1, in Body body2, Double3 baseOffset, IntPtr collisionResult)
    {
        if(TryFindBody(body1, out var b1) && TryFindBody(body2, out var b2))
        {
            if(Physics3D.ContactValidate(b1, b2) == false)
            {
                return ValidateResult.RejectContact;
            }
        }

        return ValidateResult.AcceptContact;
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
            if(pair.entity.Enabled == false)
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

        physicsSystem.Step(deltaTime, collisionSteps);

        foreach(var pair in bodies)
        {
            var transform = pair.entity.GetComponent<Transform>();

            if(transform != null)
            {
                var body = pair.body;

                transform.Position = body.Position;
                transform.LocalRotation = body.Rotation;
            }
        }
    }

    private bool CreateBody(Entity entity, ShapeSettings settings, Vector3 position, Quaternion rotation, MotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane, out IBody3D body)
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

        if(freezeY == false)
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

        foreach(var d in dofs)
        {
            dof |= d;
        }

        creationSettings.AllowedDOFs = dof;

        var b = physicsSystem.BodyInterface.CreateBody(creationSettings);

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
            pair.Friction = friction;
            pair.Restitution = restitution;

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
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane, out IBody3D body)
    {
        if(extents.X < MinExtents || extents.Y < MinExtents || extents.Z < MinExtents)
        {
            throw new ArgumentException($"Extents must be bigger or equal to {MinExtents}");
        }

        return CreateBody(entity, new BoxShapeSettings(extents / 2), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, out body);
    }

    public bool CreateSphere(Entity entity, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane, out IBody3D body)
    {
        if(radius <= 0)
        {
            throw new ArgumentException("Radius must be bigger than 0");
        }

        return CreateBody(entity, new SphereShapeSettings(radius), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, out body);
    }

    public bool CreateCapsule(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ,
        bool is2DPlane, out IBody3D body)
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
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, out body);
    }

    public bool CreateCylinder(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ,
        bool is2DPlane, out IBody3D body)
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
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, out body);
    }

    public bool CreateMesh(Entity entity, Mesh mesh, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        out IBody3D body)
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

        if((mesh.vertices?.Length ?? 0) == 0)
        {
            throw new ArgumentException("Mesh doesn't have vertices", nameof(mesh));
        }

        if((mesh.indices?.Length ?? 0) == 0)
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

        return CreateBody(entity, settings, position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor, friction, restitution,
            freezeX, freezeY, freezeZ, is2DPlane, out body);
    }

    public bool CreateMesh(Entity entity, ReadOnlySpan<Triangle> triangles, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane, out IBody3D body)
    {
        return CreateBody(entity, new MeshShapeSettings(triangles), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, out body);
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

        if(world.TryGetComponent<BoxCollider3D>(entity, out var boxCollider))
        {
            any = true;

            var extents = boxCollider.size * transform.Scale;

            if(extents.X <= 0)
            {
                throw new ArgumentException($"BoxCollider3D {world.GetEntityName(entity)} Extents X must be bigger than 0");
            }

            if (extents.Y <= 0)
            {
                throw new ArgumentException($"BoxCollider3D {world.GetEntityName(entity)} Extents Y must be bigger than 0");
            }

            if (extents.Z <= 0)
            {
                throw new ArgumentException($"BoxCollider3D {world.GetEntityName(entity)} Extents Z must be bigger than 0");
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

            if ((mesh.vertices?.Length ?? 0) == 0)
            {
                throw new ArgumentException($"MeshCollider3D {world.GetEntityName(entity)} Mesh doesn't have vertices", nameof(mesh));
            }

            if ((mesh.indices?.Length ?? 0) == 0)
            {
                throw new ArgumentException($"MeshCollider3D {world.GetEntityName(entity)} Mesh doesn't have indices", nameof(mesh));
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
            rigidBody.is2DPlane, out var body))
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
            return physicsSystem.BodyInterface.GetGravityFactor(pair.body.ID);
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

    public IBody3D GetBody(Entity entity)
    {
        foreach(var body in bodies)
        {
            if(body is JoltBodyPair pair && pair.entity == entity)
            {
                return body;
            }
        }

        return null;
    }

    public IBody3D GetBody(BodyID bodyID)
    {
        foreach (var body in bodies)
        {
            if (body is JoltBodyPair pair && pair.body.ID == bodyID)
            {
                return body;
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
