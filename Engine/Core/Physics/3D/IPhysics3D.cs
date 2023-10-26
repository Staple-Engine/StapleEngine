using System.Numerics;

namespace Staple
{
    /// <summary>
    /// 3D Physics interface
    /// </summary>
    internal interface IPhysics3D
    {
        /// <summary>
        /// Whether this has been destroyed
        /// </summary>
        bool Destroyed { get; }

        /// <summary>
        /// Current gravity
        /// </summary>
        Vector3 Gravity { get; set; }

        /// <summary>
        /// Destroy this immediately
        /// </summary>
        void Destroy();

        /// <summary>
        /// UPdate with a specific delta time. Typically this is the fixed time step delta time.
        /// </summary>
        /// <param name="deltaTime">The delta time to use</param>
        void Update(float deltaTime);

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
        /// <param name="freezeX">Whether to freeze X rotation</param>
        /// <param name="freezeY">Whether to freeze Y rotation</param>
        /// <param name="freezeZ">Whether to freeze Z rotation</param>
        /// <param name="is2DPlane">Whether this collider should act as a 2D plane (X/Y movement, Z rotation)</param>
        /// <param name="body">The body, if valid</param>
        /// <returns>Whether the body was created</returns>
        bool CreateBox(Entity entity, Vector3 extents, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
            bool isTrigger, float gravityFactor, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane, out IBody3D body);

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
        /// <param name="freezeX">Whether to freeze X rotation</param>
        /// <param name="freezeY">Whether to freeze Y rotation</param>
        /// <param name="freezeZ">Whether to freeze Z rotation</param>
        /// <param name="is2DPlane">Whether this collider should act as a 2D plane (X/Y movement, Z rotation)</param>
        /// <param name="body">The body, if valid</param>
        /// <returns>Whether the body was created</returns>
        bool CreateSphere(Entity entity, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
            bool isTrigger, float gravityFactor, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane, out IBody3D body);

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
        /// <param name="freezeX">Whether to freeze X rotation</param>
        /// <param name="freezeY">Whether to freeze Y rotation</param>
        /// <param name="freezeZ">Whether to freeze Z rotation</param>
        /// <param name="is2DPlane">Whether this collider should act as a 2D plane (X/Y movement, Z rotation)</param>
        /// <param name="body">The body, if valid</param>
        /// <returns>Whether the body was created</returns>
        bool CreateCapsule(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
            bool isTrigger, float gravityFactor, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane, out IBody3D body);

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
        /// <param name="freezeX">Whether to freeze X rotation</param>
        /// <param name="freezeY">Whether to freeze Y rotation</param>
        /// <param name="freezeZ">Whether to freeze Z rotation</param>
        /// <param name="is2DPlane">Whether this collider should act as a 2D plane (X/Y movement, Z rotation)</param>
        /// <param name="body">The body, if valid</param>
        /// <returns>Whether the body was created</returns>
        bool CreateCylinder(Entity entity, float height, float radius, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
            bool isTrigger, float gravityFactor, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane, out IBody3D body);

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
        /// <param name="freezeX">Whether to freeze X rotation</param>
        /// <param name="freezeY">Whether to freeze Y rotation</param>
        /// <param name="freezeZ">Whether to freeze Z rotation</param>
        /// <param name="is2DPlane">Whether this collider should act as a 2D plane (X/Y movement, Z rotation)</param>
        /// <param name="body">The body, if valid</param>
        /// <returns>Whether the body was created</returns>
        bool CreateMesh(Entity entity, Mesh mesh, Vector3 position, Quaternion rotation, BodyMotionType motionType, ushort layer,
            bool isTrigger, float gravityFactor, bool freezeX, bool freezeY, bool freezeZ, bool is2DPlane, out IBody3D body);

        /// <summary>
        /// Destroys a body
        /// </summary>
        /// <param name="body">The body to destroy</param>
        void DestroyBody(IBody3D body);

        /// <summary>
        /// Adds a body to the simulation
        /// </summary>
        /// <param name="body">The body to add</param>
        /// <param name="activated">Whether it's activated</param>
        void AddBody(IBody3D body, bool activated);

        /// <summary>
        /// Removes a body from the simulation. This does not destroy it.
        /// </summary>
        /// <param name="body">The body to remove</param>
        void RemoveBody(IBody3D body);

        /// <summary>
        /// Casts a ray and gets a collision result
        /// </summary>
        /// <param name="ray">The ray to cast</param>
        /// <param name="body">The body that was hit</param>
        /// <param name="fraction">The multiplier to hit the body from the ray position</param>
        /// <param name="triggerQuery">Whether to hit triggers</param>
        /// <param name="maxDistance">The maximum distance to hit</param>
        /// <returns>Whether we hit something</returns>
        bool RayCast(Ray ray, out IBody3D body, out float fraction, PhysicsTriggerQuery triggerQuery, float maxDistance);

        /// <summary>
        /// Gets the gravity factor for a body
        /// </summary>
        /// <param name="body">The body to check</param>
        /// <returns>The gravity factor</returns>
        float GravityFactor(IBody3D body);

        /// <summary>
        /// Sets the gravity factor for a body
        /// </summary>
        /// <param name="body">The body to set</param>
        /// <param name="factor">The gravity factor</param>
        void SetGravityFactor(IBody3D body, float factor);

        /// <summary>
        /// Sets a body's position
        /// </summary>
        /// <param name="body">The body to set</param>
        /// <param name="newPosition">The new position</param>
        void SetBodyPosition(IBody3D body, Vector3 newPosition);

        /// <summary>
        /// Sets a body's rotation
        /// </summary>
        /// <param name="body">The body to set</param>
        /// <param name="newRotation">The new rotation</param>
        void SetBodyRotation(IBody3D body, Quaternion newRotation);

        /// <summary>
        /// Sets whether a body is a trigger
        /// </summary>
        /// <param name="body">The body to set</param>
        /// <param name="value">Whether it should be a trigger</param>
        void SetBodyTrigger(IBody3D body, bool value);

        /// <summary>
        /// Gets the body that belongs to an entity
        /// </summary>
        /// <param name="entity">The entity to query</param>
        /// <returns>The body if available, or null</returns>
        IBody3D GetBody(Entity entity);
    }
}
