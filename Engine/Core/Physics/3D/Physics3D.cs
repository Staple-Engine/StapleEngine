using System;
using System.Numerics;

namespace Staple.Internal;

/// <summary>
/// Physics 3D management proxy
/// </summary>
public class Physics3D : ISubsystem
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

        internal set
        {
            instance = value;
        }
    }

    /// <summary>
    /// The implementation type to use for this subsystem
    /// </summary>
    internal static Type ImplType;

    internal static readonly byte Priority = 2;

    public IPhysics3D Impl { get; internal set; }

    private static readonly Vector3 DefaultGravity = new(0, -9.81f, 0);

    internal static float PhysicsDeltaTime = 1 / 60.0f;

    private float deltaTimer = 0.0f;

    public delegate void OnBodyActivated3D(IBody3D body);
    public delegate void OnBodyDeactivated3D(IBody3D body);
    public delegate void OnContactAdded3D(IBody3D self, IBody3D other);
    public delegate void OnContactPersisted3D(IBody3D self, IBody3D other);
    public delegate bool OnContactValidate3D(IBody3D self, IBody3D other);

    /// <summary>
    /// Whether this has been destroyed
    /// </summary>
    public bool Destroyed => Impl?.Destroyed ?? true;

    public SubsystemType type => SubsystemType.Update;

    /// <summary>
    /// Current gravity
    /// </summary>
    public Vector3 Gravity
    {
        get => Impl.Gravity;

        set => Impl.Gravity = value;
    }

    public Physics3D(IPhysics3D impl)
    {
        Impl = impl;
    }

    #region API
    /// <summary>
    /// Creates the body for an entity based on the components it has
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <returns>The body, or null</returns>
    internal IBody3D CreateBody(Entity entity, World world)
    {
        return Impl.CreateBody(entity, world);
    }

    /// <summary>
    /// Create a box body
    /// </summary>
    /// <param name="entity">The entity this belongs to</param>
    /// <param name="extents">The size of the box</param>
    /// <param name="position">The position of the body</param>
    /// <param name="rotation">The rotation of the body</param>
    /// <param name="motionType">The motion type of the body</param>
    /// <param name="layer">The layer this body belongs to</param>
    /// <param name="isTrigger">Whether this is a trigger collider</param>
    /// <param name="gravityFactor">The gravity multiplier for the rigid body</param>
    /// <param name="friction">The friction factor of the rigid body</param>
    /// <param name="restitution">The restitution factor of the rigid body</param>
    /// <param name="freezeX">Whether to freeze X rotation</param>
    /// <param name="freezeY">Whether to freeze Y rotation</param>
    /// <param name="freezeZ">Whether to freeze Z rotation</param>
    /// <param name="is2DPlane">Whether this collider should act as a 2D plane (X/Y movement, Z rotation)</param>
    /// <param name="mass">The mass of the body</param>
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    internal bool CreateBox(Entity entity, Vector3 extents, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        float mass, out IBody3D body)
    {
        return Impl.CreateBox(entity, extents, position, rotation, motionType, layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    /// <summary>
    /// Create a sphere body
    /// </summary>
    /// <param name="entity">The entity this belongs to</param>
    /// <param name="radius">The radius of the sphere</param>
    /// <param name="position">The position of the body</param>
    /// <param name="rotation">The rotation of the body</param>
    /// <param name="motionType">The motion type of the body</param>
    /// <param name="layer">The layer this body belongs to</param>
    /// <param name="isTrigger">Whether this is a trigger collider</param>
    /// <param name="gravityFactor">The gravity multiplier for the rigid body</param>
    /// <param name="friction">The friction factor of the rigid body</param>
    /// <param name="restitution">The restitution factor of the rigid body</param>
    /// <param name="freezeX">Whether to freeze X rotation</param>
    /// <param name="freezeY">Whether to freeze Y rotation</param>
    /// <param name="freezeZ">Whether to freeze Z rotation</param>
    /// <param name="is2DPlane">Whether this collider should act as a 2D plane (X/Y movement, Z rotation)</param>
    /// <param name="mass">The mass of the body</param>
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    internal bool CreateSphere(Entity entity, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        float mass, out IBody3D body)
    {
        return Impl.CreateSphere(entity, radius, position, rotation, motionType, layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    /// <summary>
    /// Create a capsule body
    /// </summary>
    /// <param name="entity">The entity this belongs to</param>
    /// <param name="height">The height of the capsule</param>
    /// <param name="radius">The radius of the capsule</param>
    /// <param name="position">The position of the body</param>
    /// <param name="rotation">The rotation of the body</param>
    /// <param name="motionType">The motion type of the body</param>
    /// <param name="layer">The layer this body belongs to</param>
    /// <param name="isTrigger">Whether this is a trigger collider</param>
    /// <param name="gravityFactor">The gravity multiplier for the rigid body</param>
    /// <param name="friction">The friction factor of the rigid body</param>
    /// <param name="restitution">The restitution factor of the rigid body</param>
    /// <param name="freezeX">Whether to freeze X rotation</param>
    /// <param name="freezeY">Whether to freeze Y rotation</param>
    /// <param name="freezeZ">Whether to freeze Z rotation</param>
    /// <param name="is2DPlane">Whether this collider should act as a 2D plane (X/Y movement, Z rotation)</param>
    /// <param name="mass">The mass of the body</param>
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    internal bool CreateCapsule(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ,
        bool is2DPlane, float mass, out IBody3D body)
    {
        return Impl.CreateCapsule(entity, height, radius, position, rotation, motionType, layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    /// <summary>
    /// Create a cylinder body
    /// </summary>
    /// <param name="entity">The entity this belongs to</param>
    /// <param name="height">The height of the cylinder</param>
    /// <param name="radius">The radius of the cylinder</param>
    /// <param name="position">The position of the body</param>
    /// <param name="rotation">The rotation of the body</param>
    /// <param name="motionType">The motion type of the body</param>
    /// <param name="layer">The layer this body belongs to</param>
    /// <param name="isTrigger">Whether this is a trigger collider</param>
    /// <param name="gravityFactor">The gravity multiplier for the rigid body</param>
    /// <param name="friction">The friction factor of the rigid body</param>
    /// <param name="restitution">The restitution factor of the rigid body</param>
    /// <param name="freezeX">Whether to freeze X rotation</param>
    /// <param name="freezeY">Whether to freeze Y rotation</param>
    /// <param name="freezeZ">Whether to freeze Z rotation</param>
    /// <param name="is2DPlane">Whether this collider should act as a 2D plane (X/Y movement, Z rotation)</param>
    /// <param name="mass">The mass of the body</param>
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    internal bool CreateCylinder(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ,
        bool is2DPlane, float mass, out IBody3D body)
    {
        return Impl.CreateCylinder(entity, height, radius, position, rotation, motionType, layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    /// <summary>
    /// Create a mesh body
    /// </summary>
    /// <param name="entity">The entity this belongs to</param>
    /// <param name="mesh">The mesh to use. Must be readable and be triangular</param>
    /// <param name="position">The position of the body</param>
    /// <param name="rotation">The rotation of the body</param>
    /// <param name="motionType">The motion type of the body</param>
    /// <param name="layer">The layer this body belongs to</param>
    /// <param name="isTrigger">Whether this is a trigger collider</param>
    /// <param name="gravityFactor">The gravity multiplier for the rigid body</param>
    /// <param name="friction">The friction factor of the rigid body</param>
    /// <param name="restitution">The restitution factor of the rigid body</param>
    /// <param name="freezeX">Whether to freeze X rotation</param>
    /// <param name="freezeY">Whether to freeze Y rotation</param>
    /// <param name="freezeZ">Whether to freeze Z rotation</param>
    /// <param name="is2DPlane">Whether this collider should act as a 2D plane (X/Y movement, Z rotation)</param>
    /// <param name="mass">The mass of the body</param>
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    internal bool CreateMesh(Entity entity, Mesh mesh, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        float mass, out IBody3D body)
    {
        return Impl.CreateMesh(entity, mesh, position, rotation, motionType, layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, mass, out body);
    }

    /// <summary>
    /// Creates a height map body
    /// </summary>
    /// <param name="entity">The entity this belongs to</param>
    /// <param name="heights">The heights for the heightmap</param>
    /// <param name="offset">The offset of the heightmap</param>
    /// <param name="scale">The scale of the heightmap</param>
    /// <param name="position">The position of the body</param>
    /// <param name="rotation">The rotation of the body</param>
    /// <param name="layer">The layer this body belongs to</param>
    /// <param name="friction">The friction factor of the rigid body</param>
    /// <param name="restitution">The restitution factor of the rigid body</param>
    /// <param name="mass">The mass of the body</param>
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    internal bool CreateHeightMap(Entity entity, float[] heights, Vector3 offset, Vector3 scale, Vector3 position, Quaternion rotation,
        ushort layer, float friction, float restitution, float mass, out IBody3D body)
    {
        return Impl.CreateHeightMap(entity, heights, offset, scale, position, rotation, layer, friction, restitution, mass, out body);
    }

    /// <summary>
    /// Destroys a body
    /// </summary>
    /// <param name="body">The body to destroy</param>
    internal void DestroyBody(IBody3D body)
    {
        if(body == null)
        {
            return;
        }

        Impl.DestroyBody(body);
    }

    /// <summary>
    /// Adds a body to the simulation
    /// </summary>
    /// <param name="body">The body to add</param>
    /// <param name="activated">Whether it's activated</param>
    internal void AddBody(IBody3D body, bool activated)
    {
        Impl.AddBody(body, activated);
    }

    /// <summary>
    /// Removes a body from the simulation. This does not destroy it.
    /// </summary>
    /// <param name="body">The body to remove</param>
    internal void RemoveBody(IBody3D body)
    {
        Impl.RemoveBody(body);
    }

    /// <summary>
    /// Casts a ray and gets a collision result
    /// </summary>
    /// <param name="ray">The ray to cast</param>
    /// <param name="body">The body that was hit</param>
    /// <param name="fraction">The multiplier to hit the body from the ray position</param>
    /// <param name="layerMask">The layer mask to use, or LayerMask.Everything.value</param>
    /// <param name="triggerQuery">Whether to hit triggers</param>
    /// <param name="maxDistance">The maximum distance to hit</param>
    /// <returns>Whether we hit something</returns>
    public bool RayCast(Ray ray, out IBody3D body, out float fraction, LayerMask layerMask, PhysicsTriggerQuery triggerQuery, float maxDistance)
    {
        return Impl.RayCast(ray, out body, out fraction, layerMask, triggerQuery, maxDistance);
    }

    /// <summary>
    /// Gets the gravity factor for a body
    /// </summary>
    /// <param name="body">The body to check</param>
    /// <returns>The gravity factor</returns>
    public float GravityFactor(IBody3D body)
    {
        return Impl.GravityFactor(body);
    }

    /// <summary>
    /// Sets the gravity factor for a body
    /// </summary>
    /// <param name="body">The body to set</param>
    /// <param name="factor">The gravity factor</param>
    public void SetGravityFactor(IBody3D body, float factor)
    {
        Impl.SetGravityFactor(body, factor);
    }

    /// <summary>
    /// Sets a body's position
    /// </summary>
    /// <param name="body">The body to set</param>
    /// <param name="newPosition">The new position</param>
    public void SetBodyPosition(IBody3D body, Vector3 newPosition)
    {
        Impl.SetBodyPosition(body, newPosition);
    }

    /// <summary>
    /// Sets a body's rotation
    /// </summary>
    /// <param name="body">The body to set</param>
    /// <param name="newRotation">The new rotation</param>
    public void SetBodyRotation(IBody3D body, Quaternion newRotation)
    {
        Impl.SetBodyRotation(body, newRotation);
    }

    /// <summary>
    /// Sets whether a body is a trigger
    /// </summary>
    /// <param name="body">The body to set</param>
    /// <param name="value">Whether it should be a trigger</param>
    public void SetBodyTrigger(IBody3D body, bool value)
    {
        Impl.SetBodyTrigger(body, value);
    }

    /// <summary>
    /// Gets the body that belongs to an entity
    /// </summary>
    /// <param name="entity">The entity to query</param>
    /// <returns>The body if available, or null</returns>
    public IBody3D GetBody(Entity entity)
    {
        return Impl.GetBody(entity);
    }

    /// <summary>
    /// Adds force to a body
    /// </summary>
    /// <param name="body">The body to apply to</param>
    /// <param name="force">The force to add</param>
    public void AddForce(IBody3D body, Vector3 force)
    {
        Impl.AddForce(body, force);
    }

    /// <summary>
    /// Adds impulse to a body
    /// </summary>
    /// <param name="body">The body to apply to</param>
    /// <param name="impulse">The impulse to add</param>
    public void AddImpulse(IBody3D body, Vector3 impulse)
    {
        Impl.AddImpulse(body, impulse);
    }

    /// <summary>
    /// Adds angular impulse to a body
    /// </summary>
    /// <param name="body">The body to apply to</param>
    /// <param name="impulse">The impulse to add</param>
    public void AddAngularImpulse(IBody3D body, Vector3 impulse)
    {
        Impl.AddAngularImpulse(body, impulse);
    }

    /// <summary>
    /// Recreate an entity's rigid body
    /// </summary>
    /// <param name="entity">The entity to recreate the body of (if able)</param>
    public void RecreateBody(Entity entity)
    {
        if (entity.TryGetComponent<RigidBody3D>(out var rigidBody) == false)
        {
            return;
        }

        if (rigidBody.body != null)
        {
            DestroyBody(rigidBody.body);
        }

        rigidBody.body = CreateBody(entity, World.Current);
    }

    #endregion

    #region Internal
    public void Startup()
    {
        World.AddComponentAddedCallback(typeof(RigidBody3D), (World world, Entity entity, ref IComponent component) =>
        {
            if(Platform.IsPlaying == false)
            {
                return;
            }

            var rigidBody = (RigidBody3D)component;

            rigidBody.body = CreateBody(entity, world);
        });

        World.AddComponentRemovedCallback(typeof(RigidBody3D), (World world, Entity entity, ref IComponent component) =>
        {
            if (Platform.IsPlaying == false)
            {
                return;
            }

            var rigidBody = (RigidBody3D)component;

            DestroyBody(rigidBody.body);

            rigidBody.body = null;
        });

        Impl.Startup();

        Gravity = DefaultGravity;
    }

    public void Shutdown()
    {
        World.Current.ForEach((Entity entity, ref RigidBody3D rigidBody) =>
        {
            DestroyBody(rigidBody.body);
        }, true);

        Impl.Shutdown();
    }

    public void Update()
    {
        deltaTimer += Time.unscaledDeltaTime;

        if(deltaTimer >= PhysicsDeltaTime)
        {
            deltaTimer -= PhysicsDeltaTime;

            Impl.Update(PhysicsDeltaTime);
        }
    }

    public static void BodyActivated(IBody3D body)
    {
        var systems = EntitySystemManager.Instance.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach (var system in systems)
        {
            system.OnBodyActivated(body);
        }
    }

    public static void BodyDeactivated(IBody3D body)
    {
        var systems = EntitySystemManager.Instance.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach (var system in systems)
        {
            system.OnBodyDeactivated(body);
        }
    }

    public static void ContactAdded(IBody3D A, IBody3D B)
    {
        var systems = EntitySystemManager.Instance.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach (var system in systems)
        {
            system.OnContactAdded(A, B);
        }
    }

    public static void ContactPersisted(IBody3D A, IBody3D B)
    {
        var systems = EntitySystemManager.Instance.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach (var system in systems)
        {
            system.OnContactPersisted(A, B);
        }
    }

    public static void ContactRemoved(IBody3D A, IBody3D B)
    {
        var systems = EntitySystemManager.Instance.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach (var system in systems)
        {
            system.OnContactRemoved(A, B);
        }
    }

    public static bool ContactValidate(IBody3D A, IBody3D B)
    {
        var systems = EntitySystemManager.Instance.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach (var system in systems)
        {
            if(system.OnContactValidate(A, B) == false)
            {
                return false;
            }
        }

        return true;
    }
    #endregion
}
