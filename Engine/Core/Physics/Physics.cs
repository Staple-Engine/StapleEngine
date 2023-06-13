namespace Staple
{
    public static class Physics
    {
        public static bool RayCast3D(Ray ray, out IBody3D body, out float fraction)
        {
            return Physics3D.Instance.RayCast(ray, out body, out fraction);
        }
    }
}
