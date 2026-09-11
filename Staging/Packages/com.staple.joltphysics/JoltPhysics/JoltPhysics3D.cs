using JoltPhysicsSharp;
using Staple.Internal;
using Staple.Utilities;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Staple.JoltPhysics;

/// <summary>
/// Implements Jolt Physics
/// </summary>
[AdditionalLibrary(AppPlatform.Android, "joltc")]
public class JoltPhysics3D : IPhysics3D
{
    public static readonly string LogTag = "JoltPhysics";

    public const float MinExtents = 0.2f;
    public const int PhysicsLayerCount = 32;
    public const int MaxBodyCount = 102400;
    public const int MaxBodyPairs = 655360;
    public const int NumBodyMutexes = 0;
    public const int OptimizeBroadPhaseBodyThreshold = 100;

    //Dependencies
    private BroadPhaseLayerInterface broadPhaseLayerInterface;
    private ObjectVsBroadPhaseLayerFilter objectVsBroadPhaseLayerFilter;
    private ObjectLayerPairFilter objectLayerPairFilter;
    private PhysicsSystem physicsSystem;
    private JobSystem jobSystem;

    private readonly ExpandableContainer<JoltBodyPair> bodies = new();

    private readonly ExpandableContainer<JoltCharacterPair> characters = new();

    private readonly Lock threadLock = new();
    private readonly CallbackGatherer callbackGatherer = new();

    private bool destroyed = false;
    private int lastAddedBodyCount = 0;

    private float interpolationAccumulator;

    private Thread simulationThread;

    public bool Destroyed
    {
        get
        {
            lock(threadLock)
            {
                return destroyed;
            }
        }

        set
        {
            lock(threadLock)
            {
                destroyed = value;
            }
        }
    }

    public Vector3 Gravity
    {
        get => physicsSystem.Gravity;

        set => physicsSystem.Gravity = value;
    }

    public bool InterpolatePhysics { get; set; }

    public void Startup()
    {
        Destroyed = false;

        if(!JoltPhysicsSharp.Foundation.Init())
        {
            throw new InvalidOperationException("[JoltPhysics] Failed to initialize Foundation");
        }

        try
        {
            JoltPhysicsSharp.Foundation.SetAssertFailureHandler((inExpression, inMessage, inFile, inLine) =>
            {
                var message = inMessage ?? inExpression;

                var outMessage = $"[JoltPhysics] Assertion failure {inExpression} at {inFile}:{inLine}: {message}";

                System.Diagnostics.Debug.WriteLine(outMessage);

                Log.Error(outMessage);

                throw new Exception(outMessage);
            });

            JoltPhysicsSharp.Foundation.SetTraceHandler((message) =>
            {
                System.Diagnostics.Debug.WriteLine(message);

                Log.Info(message, LogTag);
            });
        }
        catch (Exception)
        {
            Log.Error("Failed to initialize assertion failure handler", LogTag);
        }

        var table = new BroadPhaseLayerInterfaceTable(PhysicsLayerCount, PhysicsLayerCount);

        broadPhaseLayerInterface = table;

        for(var i = 0; i < PhysicsLayerCount; i++)
        {
            table.MapObjectToBroadPhaseLayer(new ObjectLayer((ushort)i), new BroadPhaseLayer((byte)i));
        }

        var layerPair = new ObjectLayerPairFilterTable(PhysicsLayerCount);

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

            layerPair.DisableCollision(new ObjectLayer((ushort)i), new ObjectLayer(Physics3D.PhysicsPickLayer));
        }

        layerPair.EnableCollision(new ObjectLayer(Physics3D.PhysicsPickLayer), new ObjectLayer(Physics3D.PhysicsPickLayer));

        objectLayerPairFilter = layerPair;

        objectVsBroadPhaseLayerFilter = new ObjectVsBroadPhaseLayerFilterTable(broadPhaseLayerInterface, PhysicsLayerCount,
            objectLayerPairFilter, PhysicsLayerCount);

        jobSystem = new JobSystemThreadPool();

        var physicsSettings = new PhysicsSystemSettings()
        {
            MaxBodies = MaxBodyCount,
            MaxBodyPairs = MaxBodyPairs,
            NumBodyMutexes = NumBodyMutexes,
            BroadPhaseLayerInterface = broadPhaseLayerInterface,
            ObjectLayerPairFilter = objectLayerPairFilter,
            ObjectVsBroadPhaseLayerFilter = objectVsBroadPhaseLayerFilter,
        };

        physicsSystem = new(physicsSettings);

        physicsSystem.OnBodyActivated += OnBodyActivated;
        physicsSystem.OnBodyDeactivated += OnBodyDeactivated;
        physicsSystem.OnContactAdded += OnContactAdded;
        physicsSystem.OnContactRemoved += OnContactRemoved;
        physicsSystem.OnContactPersisted += OnContactPersisted;
        physicsSystem.OnContactValidate += OnContactValidate;

        simulationThread = new(() =>
        {
            var tracker = new TimeTracker();
            var deltaTimer = 0.0f;
            var physicsDeltaTime = Physics3D.PhysicsDeltaTime;
            var maximumTime = Time.maximumFixedTimestepTime;

            for (; ; )
            {
                try
                {
                    if (Destroyed)
                    {
                        break;
                    }

                    deltaTimer += tracker.ElapsedTime;

                    var accumulated = 0.0f;

                    while (deltaTimer >= physicsDeltaTime && accumulated < maximumTime)
                    {
                        deltaTimer -= physicsDeltaTime;

                        accumulated += physicsDeltaTime;

                        Simulate();
                    }

                    SpinWait.SpinUntil(() => tracker.ElapsedTimeNoReset >= physicsDeltaTime);
                }
                catch(Exception e)
                {
                    Log.Error($"Simulation Thread exception: {e}", LogTag);
                }
            }
        })
        {
            IsBackground = true,
        };

        simulationThread.Start();
    }

    public void Shutdown()
    {
        lock(threadLock)
        {
            if (Destroyed)
            {
                return;
            }

            Destroyed = true;
        }

        while(simulationThread.IsAlive)
        {
            Thread.Sleep(5);
        }

        simulationThread = null;

        JoltPhysicsSharp.Foundation.Shutdown();
    }

    #region Internal
    private bool TryFindBody(BodyID body, out IBody3D outBody)
    {
        var contents = bodies.Contents;

        outBody = default;

        for(var i = 0; i < contents.Length; i++)
        {
            if (contents[i] is JoltBodyPair p && p.body.ID == body)
            {
                outBody = p;

                break;
            }
        }

        return outBody != null;
    }

    private bool TryFindCharacter(BodyID character, out IBody3D outBody)
    {
        var contents = characters.Contents;

        outBody = default;

        for (var i = 0; i < contents.Length; i++)
        {
            if (contents[i] is JoltCharacterPair p && p.character.BodyID == character)
            {
                outBody = p;

                break;
            }
        }

        return outBody != null;
    }

    private bool TryFindBodyOrCharacter(BodyID body, out IBody3D outBody)
    {
        return TryFindBody(body, out outBody) || TryFindCharacter(body, out outBody);
    }

    private void OnContactAdded(PhysicsSystem system, in Body body1, in Body body2, in ContactManifold manifold, ref ContactSettings settings)
    {
        if (TryFindBody(body1.ID, out var b1) && TryFindBody(body2.ID, out var b2))
        {
            callbackGatherer.AddCallback(() =>
            {
                Physics3D.Instance.ContactAdded(b1, b2);
            });
        }
    }

    private void OnContactPersisted(PhysicsSystem system, in Body body1, in Body body2, in ContactManifold manifold, ref ContactSettings settings)
    {
        if (TryFindBody(body1.ID, out var b1) && TryFindBody(body2.ID, out var b2))
        {
            callbackGatherer.AddCallback(() =>
            {
                Physics3D.Instance.ContactPersisted(b1, b2);
            });
        }
    }

    private void OnContactRemoved(PhysicsSystem system, ref SubShapeIDPair subShapePair)
    {
        if (TryFindBody(subShapePair.Body1ID, out var b1) &&
            TryFindBody(subShapePair.Body2ID, out var b2))
        {
            callbackGatherer.AddCallback(() =>
            {
                Physics3D.Instance.ContactRemoved(b1, b2);
            });
        }
    }

    private ValidateResult OnContactValidate(PhysicsSystem system, in Body body1, in Body body2, RVector3 baseOffset,
        in CollideShapeResult collisionResult)
    {
        if (TryFindBody(body1.ID, out var b1) && TryFindBody(body2.ID, out var b2))
        {
            if(!Physics3D.Instance.ContactValidate(b1, b2))
            {
                return ValidateResult.RejectContact;
            }
        }

        return ValidateResult.AcceptContact;
    }

    private void OnBodyActivated(PhysicsSystem system, in BodyID bodyID, ulong bodyUserData)
    {
        if (TryFindBodyOrCharacter(bodyID, out var body))
        {
            callbackGatherer.AddCallback(() =>
            {
                Physics3D.Instance.BodyActivated(body);
            });
        }
    }

    private void OnBodyDeactivated(PhysicsSystem system, in BodyID bodyID, ulong bodyUserData)
    {
        if (TryFindBodyOrCharacter(bodyID, out var body))
        {
            callbackGatherer.AddCallback(() =>
            {
                Physics3D.Instance.BodyDeactivated(body);
            });
        }
    }
    #endregion

    public void Update(float deltaTime)
    {
        if (Destroyed)
        {
            return;
        }

        lock (threadLock)
        {
            interpolationAccumulator += deltaTime;

            if (Physics.InterpolatePhysics)
            {
                var alpha = interpolationAccumulator / Physics3D.PhysicsDeltaTime;

                foreach (var p in bodies.Contents)
                {
                    if (p == null)
                    {
                        continue;
                    }

                    if (p.Entity.EnabledInHierarchy)
                    {
                        if(!p.body.IsActive)
                        {
                            p.needsAdd = true;
                        }
                    }
                    else if(p.body.IsActive)
                    {
                        p.needsRemove = true;
                    }

                    if (!p.body.IsActive)
                    {
                        continue;
                    }

                    p.interpolatedPosition = Vector3.Lerp(p.previousPosition, p.currentPosition, Math.Clamp01(alpha));
                    p.interpolatedRotation = Quaternion.Slerp(p.previousRotation, p.currentRotation, Math.Clamp01(alpha));

                    if (p.transform != null)
                    {
                        p.transform.Position = p.interpolatedPosition;
                        p.transform.Rotation = p.interpolatedRotation;
                    }
                }

                foreach (var p in characters.Contents)
                {
                    if (p == null)
                    {
                        continue;
                    }

                    if(p.Entity.EnabledInHierarchy)
                    {
                        if(!p.enabled)
                        {
                            p.needsAdd = true;
                        }
                    }
                    else if(p.enabled)
                    {
                        p.needsRemove = true;
                    }

                    if (!p.enabled)
                    {
                        continue;
                    }

                    p.interpolatedPosition = Vector3.Lerp(p.previousPosition, p.currentPosition, Math.Clamp01(alpha));
                    p.interpolatedRotation = Quaternion.Slerp(p.previousRotation, p.currentRotation, Math.Clamp01(alpha));

                    if (p.transform != null)
                    {
                        p.transform.Position = p.interpolatedPosition;
                        p.transform.Rotation = p.interpolatedRotation;
                    }
                }
            }
        }
    }

    private void Simulate()
    {
        var collisionSteps = Math.CeilToInt(Physics3D.PhysicsDeltaTime / (1 / 60.0f));

        lock (threadLock)
        {
            foreach (var p in bodies.Contents)
            {
                if (p == null)
                {
                    continue;
                }

                if (Physics.InterpolatePhysics)
                {
                    p.previousPosition = p.currentPosition;
                    p.previousRotation = p.currentRotation;
                }

                if (p.needsRemove)
                {
                    if (p.body.IsActive)
                    {
                        physicsSystem.BodyInterface.DeactivateBody(p.body.ID);
                    }
                }
                else if (p.needsAdd)
                {
                    physicsSystem.BodyInterface.ActivateBody(p.body.ID);
                }
            }

            foreach (var p in characters.Contents)
            {
                if(p == null)
                {
                    continue;
                }

                if (Physics.InterpolatePhysics)
                {
                    p.previousPosition = p.currentPosition;
                    p.previousRotation = p.currentRotation;
                }

                if (p.needsRemove)
                {
                    if (p.enabled)
                    {
                        p.enabled = false;

                        p.character.RemoveFromPhysicsSystem();
                    }
                }
                else if (p.needsAdd)
                {
                    p.enabled = true;

                    p.character.AddToPhysicsSystem();
                }
            }
        }

        physicsSystem.Update(Physics3D.PhysicsDeltaTime, collisionSteps, jobSystem);

        callbackGatherer.PerformAll();

        lock (threadLock)
        {
            foreach (var p in bodies.Contents)
            {
                if (p == null || !p.body.IsActive)
                {
                    continue;
                }

                p.currentPosition = p.body.Position;
                p.currentRotation = p.body.Rotation;

                if (!Physics.InterpolatePhysics && p.transform != null)
                {
                    p.transform.Position = p.currentPosition;
                    p.transform.Rotation = p.currentRotation;
                }
            }

            foreach (var p in characters.Contents)
            {
                if (p == null || !p.enabled)
                {
                    continue;
                }

                (p.currentPosition, p.currentRotation) = p.character.GetPositionAndRotation();

                if (!Physics.InterpolatePhysics && p.transform != null)
                {
                    p.transform.Position = p.currentPosition;
                    p.transform.Rotation = p.currentRotation;
                }

                p.character.PostSimulation(0.05f);
            }
        }
    }

    private bool CreateCharacter(Entity entity, Vector3 position, Quaternion rotation, ushort layer, float gravityFactor, float friction, float mass,
        float maxSlopeAngle, Shape shape, Vector3 upDirection, out IBody3D body)
    {
        lock(threadLock)
        {
            if(characters.Length <= entity.Index)
            {
                characters.Resize(entity.Index + 1, true);
            }

            ref var entityCharacter = ref characters.Contents[entity.Index];

            if (entityCharacter != null)
            {
                Log.Warning($"Attempting to create a character for {entity} failed: There's already a body for it! Returning existing body...",
                    LogTag);

                body = entityCharacter;

                return true;
            }

            var settings = new CharacterSettings()
            {
                MaxSlopeAngle = Math.Deg2Rad * maxSlopeAngle,
                Shape = shape,
                Friction = friction,
                Mass = mass,
                GravityFactor = gravityFactor,
                Layer = layer,
                Up = upDirection,
            };

            var character = new Character(settings, position, rotation.SafeNormalize(), 0, physicsSystem);

            if(character.Handle != IntPtr.Zero)
            {
                var pair = new JoltCharacterPair()
                {
                    character = character,
                    entity = entity,
                    transform = entity.GetComponent<Transform>(),
                    gravityFactor = gravityFactor,
                    friction = friction,
                    Position = position,
                    Rotation = rotation.SafeNormalize(),
                    enabled = true,
                    currentPosition = position,
                    currentRotation = rotation.SafeNormalize(),
                    previousPosition = position,
                    previousRotation = rotation.SafeNormalize(),
                };

                entityCharacter = pair;

                body = pair;

                AddBody(body, true);

                return true;
            }

            body = default;

            return false;
        }
    }

    private bool CreateBody(Entity entity, ShapeSettings settings, Vector3 position, Quaternion rotation, MotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        float mass, out IBody3D body)
    {
        lock (threadLock)
        {
            if (bodies.Length <= entity.Index)
            {
                bodies.Resize(entity.Index + 1, true);
            }

            ref var entityBody = ref bodies.Contents[entity.Index];

            if (entityBody != null)
            {
                Log.Warning($"Attempting to create a body for {entity} failed: There's already a body for it! Returning existing body...",
                    LogTag);

                body = entityBody;

                return true;
            }

            var creationSettings = new BodyCreationSettings(settings, position, rotation.SafeNormalize(), motionType, new ObjectLayer(layer));

            var dofs = new List<AllowedDOFs>
            {
                AllowedDOFs.TranslationX,
                AllowedDOFs.TranslationY
            };

            if (!freezeX)
            {
                dofs.Add(AllowedDOFs.RotationX);
            }

            if (!freezeY)
            {
                dofs.Add(AllowedDOFs.RotationY);
            }

            if (!freezeZ)
            {
                dofs.Add(AllowedDOFs.RotationZ);
            }

            if (!is2DPlane)
            {
                dofs.Add(AllowedDOFs.TranslationZ);
            }

            AllowedDOFs dof = 0;

            foreach (var d in dofs)
            {
                dof |= d;
            }

            creationSettings.AllowedDOFs = dof;

            Body b = physicsSystem.BodyInterface.CreateBody(creationSettings);

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
                    transform = entity.GetComponent<Transform>(),
                    currentPosition = position,
                    currentRotation = rotation.SafeNormalize(),
                    previousPosition = position,
                    previousRotation = rotation.SafeNormalize(),
                };

                entityBody = pair;

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
                    return CreateBody(entity, new HeightFieldShapeSettings(ptr, offset, scale, (uint)Math.Sqrt(heights.Length)),
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

        if(!mesh.isReadable)
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

    public bool CreateMesh(Entity entity, Span<Triangle> triangles, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        float mass, out IBody3D body)
    {
        return CreateBody(entity, new MeshShapeSettings(triangles), position, rotation, GetMotionType(motionType), layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    public IBody3D CreateBody(Entity entity, World world)
    {
        RigidBody3D rigidBody = null;
        Character3D character = null;

        if (!world.TryGetComponent(entity, out rigidBody) && !world.TryGetComponent(entity, out character))
        {
            Log.Debug($"Failed to create body for entity {world.GetEntityName(entity)}: No RigidBody3D or Character3D component found",
                LogTag);

            return null;
        }

        if (!world.TryGetComponent<Transform>(entity, out var transform))
        {
            Log.Debug($"Failed to create body for entity {world.GetEntityName(entity)}: No Transform component found", LogTag);

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
                    compound.AddShape(heightMap.position, heightMap.rotation.SafeNormalize(),
                        new HeightFieldShapeSettings(ptr, heightMap.offset, heightMap.scale, (uint)Math.Sqrt(heightMap.heights.Length)));
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

            compound.AddShape(boxCollider.position, boxCollider.rotation.SafeNormalize(), new BoxShapeSettings(extents / 2));
        }

        if(world.TryGetComponent<SphereCollider3D>(entity, out var sphereCollider))
        {
            any = true;

            var radius = sphereCollider.radius * transform.Scale.X;

            if(radius <= 0)
            {
                throw new ArgumentException($"SphereCollider3D {world.GetEntityName(entity)} Radius must be bigger than 0");
            }

            compound.AddShape(sphereCollider.position, sphereCollider.rotation.SafeNormalize(), new SphereShapeSettings(radius));
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

            compound.AddShape(capsuleCollider.position, capsuleCollider.rotation.SafeNormalize(), new CapsuleShapeSettings(height / 2, radius));
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

            compound.AddShape(cylinderCollider.position, cylinderCollider.rotation.SafeNormalize(), new CylinderShapeSettings(height / 2, radius));
        }

        if(world.TryGetComponent<MeshCollider3D>(entity, out var meshCollider))
        {
            any = true;

            var mesh = meshCollider.mesh;

            if (mesh is null)
            {
                throw new NullReferenceException($"MeshCollider3D {world.GetEntityName(entity)} Mesh is null");
            }

            if (!mesh.isReadable)
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

            compound.AddShape(meshCollider.position, meshCollider.rotation.SafeNormalize(), settings);
        }

        if(!any)
        {
            Log.Error($"Rigid Body for entity {world.GetEntityName(entity)} has no attached colliders, ignoring...", LogTag);

            return null;
        }

        if(rigidBody != null)
        {
            if (CreateBody(entity, compound, transform.Position, transform.Rotation, GetMotionType(rigidBody.motionType),
                (ushort)world.GetEntityLayer(entity), rigidBody.isTrigger, rigidBody.gravityFactor, rigidBody.friction,
                rigidBody.restitution, rigidBody.freezeRotationX, rigidBody.freezeRotationY, rigidBody.freezeRotationZ,
                rigidBody.is2DPlane, rigidBody.mass, out var body))
            {
                return body;
            }
        }
        else if(character != null)
        {
            if(CreateCharacter(entity, transform.Position, transform.Rotation, (ushort)world.GetEntityLayer(entity),
                character.gravityFactor, character.friction, character.mass, character.maxSlopeAngle, new MutableCompoundShape(compound),
                character.upDirection, out var body))
            {
                return body;
            }
        }

        Log.Error($"Failed to create body for entity {world.GetEntityName(entity)}", LogTag);

        return null;
    }

    public void DestroyBody(IBody3D body)
    {
        if(body is JoltBodyPair pair)
        {
            lock (threadLock)
            {
                var id = pair.body.ID;

                physicsSystem.BodyInterface.RemoveAndDestroyBody(id);

                bodies.Contents[pair.Entity.Index] = null;
            }
        }
        else if(body is JoltCharacterPair characterPair)
        {
            lock(threadLock)
            {
                characterPair.enabled = false;

                var id = characterPair.character.BodyID;

                physicsSystem.BodyInterface.RemoveAndDestroyBody(id);

                characters.Contents[characterPair.Entity.Index] = null;
            }
        }
    }

    public void AddBody(IBody3D body, bool activated)
    {
        if(body is JoltBodyPair bodyPair)
        {
            lock (threadLock)
            {
                physicsSystem.BodyInterface.AddBody(bodyPair.body, activated ? Activation.Activate : Activation.DontActivate);
            }
        }
        else if(body is JoltCharacterPair characterPair)
        {
            lock(threadLock)
            {
                characterPair.enabled = true;

                characterPair.character.AddToPhysicsSystem();
            }
        }

        lock(threadLock)
        {
            var currentBodyCount = bodies.Length + characters.Length;

            if (currentBodyCount >= lastAddedBodyCount + OptimizeBroadPhaseBodyThreshold)
            {
                physicsSystem.OptimizeBroadPhase();

                lastAddedBodyCount = currentBodyCount;
            }
        }
    }

    public void RemoveBody(IBody3D body)
    {
        if (body is JoltBodyPair bodyPair)
        {
            lock (threadLock)
            {
                physicsSystem.BodyInterface.RemoveBody(bodyPair.body.ID);
            }
        }
        else if (body is JoltCharacterPair characterPair)
        {
            lock (threadLock)
            {
                characterPair.enabled = false;

                characterPair.character.RemoveFromPhysicsSystem();
            }
        }

        lock (threadLock)
        {
            var currentBodyCount = bodies.Length + characters.Length;

            if (currentBodyCount <= lastAddedBodyCount - OptimizeBroadPhaseBodyThreshold)
            {
                physicsSystem.OptimizeBroadPhase();

                lastAddedBodyCount = currentBodyCount;
            }
        }
    }

    public bool RayCast(Ray ray, out RaycastHit3D raycastHit, LayerMask layerMask, PhysicsTriggerQuery triggerQuery, float maxDistance)
    {
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

        var r = new JoltPhysicsSharp.Ray(ray.position, ray.direction * maxDistance);
        var results = new List<RayCastResult>();

        lock (threadLock)
        {
            result = physicsSystem.NarrowPhaseQuery.CastRay(r, new RayCastSettings(), CollisionCollectorType.ClosestHit, results,
                broadPhaseFilter, objectLayerFilter, bodyFilter);
        }

        if (result)
        {
            var closestDistance = 99999.0f;
            var lastBody = default(IBody3D);

            IBody3D body;
            var fraction = 0.0f;

            foreach(var hit in results)
            {
                if (TryFindBody(hit.BodyID, out body))
                {
                    var distance = Vector3.DistanceSquared(body.Position, ray.position);

                    if (lastBody != null)
                    {
                        if(distance < closestDistance)
                        {
                            fraction = hit.Fraction;
                            closestDistance = distance;
                            lastBody = body;
                        }
                    }
                    else
                    {
                        fraction = hit.Fraction;
                        closestDistance = distance;
                        lastBody = body;
                    }
                }
            }

            body = lastBody;

            raycastHit = new(body, fraction, ray.position + ray.direction * fraction * maxDistance);

            return true;
        }

        raycastHit = default;

        return false;
    }

    public RaycastHit3D[] RayCastAll(Ray ray, LayerMask layerMask, PhysicsTriggerQuery triggerQuery, float maxDistance, RenderableSortMode sortMode)
    {
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

        var r = new JoltPhysicsSharp.Ray(ray.position, ray.direction * maxDistance);
        var results = new List<RayCastResult>();

        lock (threadLock)
        {
            result = physicsSystem.NarrowPhaseQuery.CastRay(r, new RayCastSettings(), CollisionCollectorType.AllHit, results,
                broadPhaseFilter, objectLayerFilter, bodyFilter);
        }

        if (result)
        {
            var outValue = new List<RaycastHit3D>();

            foreach(var hit in results)
            {
                if (TryFindBody(hit.BodyID, out var body))
                {
                    outValue.Add(new(body, hit.Fraction * maxDistance, ray.position + ray.direction * hit.Fraction * maxDistance));
                }
            }

            var arrayResult = outValue.ToArray();

            Physics.SortRaycastHits3D(arrayResult, sortMode);

            return arrayResult;
        }

        return [];
    }

    public int RayCastNoAlloc(Ray ray, Span<RaycastHit3D> hits, LayerMask layerMask, PhysicsTriggerQuery triggerQuery, float maxDistance,
        RenderableSortMode sortMode)
    {
        if(hits.Length == 0)
        {
            return 0;
        }

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

        var r = new JoltPhysicsSharp.Ray(ray.position, ray.direction * maxDistance);
        var results = new List<RayCastResult>();

        lock (threadLock)
        {
            result = physicsSystem.NarrowPhaseQuery.CastRay(r, new RayCastSettings(), CollisionCollectorType.AllHit, results,
                broadPhaseFilter, objectLayerFilter, bodyFilter);
        }

        if (result)
        {
            var counter = 0;

            foreach (var hit in results)
            {
                if(counter >= hits.Length)
                {
                    return counter;
                }

                if (TryFindBody(hit.BodyID, out var body))
                {
                    hits[counter++] = new(body, hit.Fraction * maxDistance, ray.position + ray.direction * hit.Fraction * maxDistance);
                }
            }

            Physics.SortRaycastHits3D(hits[..counter], sortMode);

            return counter;
        }

        return 0;
    }

    public float GravityFactor(IBody3D body)
    {
        if(body is JoltBodyPair bodyPair)
        {
            lock (threadLock)
            {
                return physicsSystem.BodyInterface.GetGravityFactor(bodyPair.body.ID);
            }
        }
        else if (body is JoltCharacterPair characterPair)
        {
            lock (threadLock)
            {
                return characterPair.gravityFactor;
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
                physicsSystem.BodyInterfaceNoLock.SetGravityFactor(pair.body.ID, factor);
            }
        }
    }

    public void SetBodyPosition(IBody3D body, Vector3 newPosition)
    {
        if(body is JoltBodyPair bodyPair)
        {
            lock (threadLock)
            {
                physicsSystem.BodyInterface.SetPosition(bodyPair.body.ID, newPosition, bodyPair.body.IsActive ?
                    Activation.Activate : Activation.DontActivate);
            }
        }
        else if(body is JoltCharacterPair characterPair)
        {
            lock (threadLock)
            {
                characterPair.character.SetPosition(newPosition);
            }
        }
    }

    public void SetBodyRotation(IBody3D body, Quaternion newRotation)
    {
        newRotation = newRotation.SafeNormalize();

        if (body is JoltBodyPair pair)
        {
            lock(threadLock)
            {
                physicsSystem.BodyInterface.SetRotation(pair.body.ID, newRotation, pair.body.IsActive ?
                    Activation.Activate : Activation.DontActivate);
            }
        }
        else if (body is JoltCharacterPair characterPair)
        {
            lock (threadLock)
            {
                characterPair.character.SetRotation(newRotation);
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
            if(entity.Index < bodies.Length)
            {
                var body = bodies.Contents[entity.Index];

                if (body != null)
                {
                    return body;
                }
            }

            if(entity.Index < characters.Length)
            {
                var body = characters.Contents[entity.Index];

                if (body != null)
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
            if(TryFindBodyOrCharacter(bodyID, out var body))
            {
                return body;
            }
        }

        return null;
    }

    public void AddForce(IBody3D body, Vector3 force)
    {
        if(body is JoltBodyPair pair)
        {
            pair.body.AddForce(force);
        }
    }

    public void AddImpulse(IBody3D body, Vector3 impulse)
    {
        if (body is JoltBodyPair pair)
        {
            pair.body.AddImpulse(impulse);
        }
        else if(body is JoltCharacterPair characterPair)
        {
            characterPair.character.AddImpulse(impulse);
        }
    }

    public void AddAngularImpulse(IBody3D body, Vector3 impulse)
    {
        if (body is JoltBodyPair pair)
        {
            pair.body.AddAngularImpulse(impulse);
        }
    }

    public void DestroyAllBodies()
    {
        lock (threadLock)
        {
            foreach (var p in bodies.Contents)
            {
                if(p == null)
                {
                    continue;
                }

                var id = p.body.ID;

                physicsSystem.BodyInterface.RemoveAndDestroyBody(id);
            }

            foreach (var p in characters.Contents)
            {
                if(p == null)
                {
                    continue;
                }

                p.enabled = false;

                var id = p.character.BodyID;

                physicsSystem.BodyInterface.RemoveAndDestroyBody(id);
            }

            bodies.Clear();
            characters.Clear();
        }
    }

    public bool TryGetBodyPositionAndRotation(IBody3D body, out Vector3 position, out Quaternion rotation)
    {
        if (body is JoltBodyPair b)
        {
            lock (threadLock)
            {
                position = b.body.Position;
                rotation = b.body.Rotation;
            }

            return true;
        }

        if (body is JoltCharacterPair c)
        {
            lock (threadLock)
            {
                (position, rotation) = c.character.GetPositionAndRotation();
            }

            return true;
        }

        position = default;
        rotation = default;

        return false;
    }
}
