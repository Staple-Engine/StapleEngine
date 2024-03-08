using System;
using System.Numerics;

namespace Staple;

/// <summary>
/// Physics 3D management proxy
/// </summary>
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

    internal IPhysics3D impl;

    private static readonly Vector3 DefaultGravity = new(0, -9.81f, 0);

    public delegate void OnBodyActivated3D(IBody3D body);
    public delegate void OnBodyDeactivated3D(IBody3D body);
    public delegate void OnContactAdded3D(IBody3D self, IBody3D other);
    public delegate void OnContactPersisted3D(IBody3D self, IBody3D other);
    public delegate bool OnContactValidate3D(IBody3D self, IBody3D other);

    /// <summary>
    /// Whether this has been destroyed
    /// </summary>
    public bool Destroyed => impl?.Destroyed ?? true;

    public SubsystemType type => SubsystemType.FixedUpdate;

    /// <summary>
    /// Current gravity
    /// </summary>
    public Vector3 Gravity
    {
        get => impl.Gravity;

        set => impl.Gravity = value;
    }

    public Physics3D(IPhysics3D impl)
    {
        this.impl = impl;

        Gravity = DefaultGravity;
    }

    #region API
    /// <summary>
    /// Creates the body for an entity based on the components it has
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <returns>The body, or null</returns>
    public IBody3D CreateBody(Entity entity, World world)
    {
        return impl.CreateBody(entity, world);
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
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    public bool CreateBox(Entity entity, Vector3 extents, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        out IBody3D body)
    {
        return impl.CreateBox(entity, extents, position, rotation, motionType, layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, out body);
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
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    public bool CreateSphere(Entity entity, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        out IBody3D body)
    {
        return impl.CreateSphere(entity, radius, position, rotation, motionType, layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, out body);
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
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    public bool CreateCapsule(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ,
        bool is2DPlane, out IBody3D body)
    {
        return impl.CreateCapsule(entity, height, radius, position, rotation, motionType, layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, out body);
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
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    public bool CreateCylinder(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType,
        ushort layer, bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ,
        bool is2DPlane, out IBody3D body)
    {
        return impl.CreateCylinder(entity, height, radius, position, rotation, motionType, layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, out body);
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
    /// <param name="body">The body, if valid</param>
    /// <returns>Whether the body was created</returns>
    public bool CreateMesh(Entity entity, Mesh mesh, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
        bool isTrigger, float gravityFactor, float friction, float restitution, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane,
        out IBody3D body)
    {
        return impl.CreateMesh(entity, mesh, position, rotation, motionType, layer, isTrigger, gravityFactor,
            friction, restitution, freezeX, freezeY, freezeZ, is2DPlane, out body);
    }

    /// <summary>
    /// Destroys a body
    /// </summary>
    /// <param name="body">The body to destroy</param>
    public void DestroyBody(IBody3D body)
    {
        if(body == null)
        {
            return;
        }

        impl.DestroyBody(body);
    }

    /// <summary>
    /// Adds a body to the simulation
    /// </summary>
    /// <param name="body">The body to add</param>
    /// <param name="activated">Whether it's activated</param>
    public void AddBody(IBody3D body, bool activated)
    {
        impl.AddBody(body, activated);
    }

    /// <summary>
    /// Removes a body from the simulation. This does not destroy it.
    /// </summary>
    /// <param name="body">The body to remove</param>
    public void RemoveBody(IBody3D body)
    {
        impl.RemoveBody(body);
    }

    /// <summary>
    /// Casts a ray and gets a collision result
    /// </summary>
    /// <param name="ray">The ray to cast</param>
    /// <param name="body">The body that was hit</param>
    /// <param name="fraction">The multiplier to hit the body from the ray position</param>
    /// <param name="triggerQuery">Whether to hit triggers</param>
    /// <param name="maxDistance">The maximum distance to hit</param>
    /// <returns>Whether we hit something</returns>
    public bool RayCast(Ray ray, out IBody3D body, out float fraction, PhysicsTriggerQuery triggerQuery, float maxDistance)
    {
        return impl.RayCast(ray, out body, out fraction, triggerQuery, maxDistance);
    }

    /// <summary>
    /// Gets the gravity factor for a body
    /// </summary>
    /// <param name="body">The body to check</param>
    /// <returns>The gravity factor</returns>
    public float GravityFactor(IBody3D body)
    {
        return impl.GravityFactor(body);
    }

    /// <summary>
    /// Sets the gravity factor for a body
    /// </summary>
    /// <param name="body">The body to set</param>
    /// <param name="factor">The gravity factor</param>
    public void SetGravityFactor(IBody3D body, float factor)
    {
        impl.SetGravityFactor(body, factor);
    }

    /// <summary>
    /// Sets a body's position
    /// </summary>
    /// <param name="body">The body to set</param>
    /// <param name="newPosition">The new position</param>
    public void SetBodyPosition(IBody3D body, Vector3 newPosition)
    {
        impl.SetBodyPosition(body, newPosition);
    }

    /// <summary>
    /// Sets a body's rotation
    /// </summary>
    /// <param name="body">The body to set</param>
    /// <param name="newRotation">The new rotation</param>
    public void SetBodyRotation(IBody3D body, Quaternion newRotation)
    {
        impl.SetBodyRotation(body, newRotation);
    }

    /// <summary>
    /// Sets whether a body is a trigger
    /// </summary>
    /// <param name="body">The body to set</param>
    /// <param name="value">Whether it should be a trigger</param>
    public void SetBodyTrigger(IBody3D body, bool value)
    {
        impl.SetBodyTrigger(body, value);
    }

    /// <summary>
    /// Gets the body that belongs to an entity
    /// </summary>
    /// <param name="entity">The entity to query</param>
    /// <returns>The body if available, or null</returns>
    public IBody3D GetBody(Entity entity)
    {
        return impl.GetBody(entity);
    }

    #endregion

    #region Internal
    public void Startup()
    {
        World.AddComponentAddedCallback(typeof(RigidBody3D), (World world, Entity entity, Transform transform, ref IComponent component) =>
        {
            if(Platform.IsPlaying == false)
            {
                return;
            }

            var rigidBody = (RigidBody3D)component;

            rigidBody.body = CreateBody(entity, world);
        });

        World.AddComponentRemovedCallback(typeof(RigidBody3D), (World world, Entity entity, Transform transform, ref IComponent component) =>
        {
            if (Platform.IsPlaying == false)
            {
                return;
            }

            var rigidBody = (RigidBody3D)component;

            DestroyBody(rigidBody.body);

            rigidBody.body = null;
        });
    }

    public void Shutdown()
    {
        World.Current.ForEach((Entity entity, ref RigidBody3D rigidBody) =>
        {
            DestroyBody(rigidBody.body);
        }, true);

        impl.Destroy();

        impl = null;
    }

    public void Update()
    {
        impl.Update(Time.fixedDeltaTime);
    }

    internal static void BodyActivated(IBody3D body)
    {
        var systems = EntitySystemManager.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach(var system in systems)
        {
            system.OnBodyActivated(body);
        }
    }

    internal static void BodyDeactivated(IBody3D body)
    {
        var systems = EntitySystemManager.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach (var system in systems)
        {
            system.OnBodyDeactivated(body);
        }
    }

    internal static void ContactAdded(IBody3D A, IBody3D B)
    {
        var systems = EntitySystemManager.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach (var system in systems)
        {
            system.OnContactAdded(A, B);
        }
    }

    internal static void ContactPersisted(IBody3D A, IBody3D B)
    {
        var systems = EntitySystemManager.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach (var system in systems)
        {
            system.OnContactPersisted(A, B);
        }
    }

    internal static void ContactRemoved(IBody3D A, IBody3D B)
    {
        var systems = EntitySystemManager.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

        foreach (var system in systems)
        {
            system.OnContactRemoved(A, B);
        }
    }

    internal static bool ContactValidate(IBody3D A, IBody3D B)
    {
        var systems = EntitySystemManager.FindEntitySystemsSubclassing<IPhysicsReceiver3D>();

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
