using Staple;

namespace TestGame
{
    public class CircularMovementComponent : Component
    {
        public float t;
        public float speed = 1;
        public float distance = 1;
        public bool followMouse = false;
    }
}