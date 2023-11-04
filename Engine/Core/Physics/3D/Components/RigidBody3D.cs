namespace Staple
{
    public class RigidBody3D : IComponent
    {
        /// <summary>
        /// The actual body instance
        /// </summary>
        internal IBody3D body;

        /// <summary>
        /// The motion type of the body
        /// </summary>
        public BodyMotionType motionType = BodyMotionType.Dynamic;

        /// <summary>
        /// Whether to freeze rotation in the X axis
        /// </summary>
        public bool freezeRotationX = false;

        /// <summary>
        /// Whether to freeze rotation in the Y axis
        /// </summary>
        public bool freezeRotationY = false;

        /// <summary>
        /// Whether to freeze rotation in the Z axis
        /// </summary>
        public bool freezeRotationZ = false;

        /// <summary>
        /// Whether this body is acting as a 2D body
        /// </summary>
        public bool is2DPlane = false;

        /// <summary>
        /// Gravity factor of this body
        /// </summary>
        public float gravityFactor = 1.0f;

        /// <summary>
        /// Whether this body is a trigger (doesn't collide, detects events)
        /// </summary>
        public bool isTrigger = false;

        /// <summary>
        /// The friction factor of this body
        /// </summary>
        public float friction = 1;

        /// <summary>
        /// The restitution factor of this body
        /// </summary>
        public float restitution = 1;
    }
}