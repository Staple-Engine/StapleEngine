namespace Staple
{
    [AbstractComponent]
    public class Collider3D : IComponent
    {
        /// <summary>
        /// The body of this collider
        /// </summary>
        internal IBody3D body;

        /// <summary>
        /// Gravity factor of this collider
        /// </summary>
        public float gravityFactor = 1.0f;

        /// <summary>
        /// Whether this collider is a trigger (doesn't collide, detects events)
        /// </summary>
        public bool isTrigger = false;

        /// <summary>
        /// The motion type of the collider's rigid body
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
    }
}
