namespace Staple
{
    public abstract class Collider3DBase : IComponent
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

        protected abstract void Awake(Entity entity, Transform transform);

        protected abstract void OnDestroy();
    }
}