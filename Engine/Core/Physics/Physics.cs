namespace Staple
{
    /// <summary>
    /// General Physics interface
    /// </summary>
    public static class Physics
    {
        /// <summary>
        /// Casts a ray and checks for a hit
        /// </summary>
        /// <param name="ray">The ray to cast</param>
        /// <param name="body">The hit body</param>
        /// <param name="fraction">The fraction of distance to hit the body</param>
        /// <param name="triggerQuery">Whether to hit triggers</param>
        /// <param name="maxDistance">The maximum distance to hit</param>
        /// <returns>Whether the body has been hit</returns>
        public static bool RayCast3D(Ray ray, out IBody3D body, out float fraction, PhysicsTriggerQuery triggerQuery = PhysicsTriggerQuery.Ignore, float maxDistance = 1.0f)
        {
            return Physics3D.Instance.RayCast(ray, out body, out fraction, triggerQuery, maxDistance);
        }
    }
}
