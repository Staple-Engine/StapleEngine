using System.Numerics;

namespace Staple
{
    public struct RectFloat
    {
        public float left;
        public float right;
        public float top;
        public float bottom;

        public RectFloat()
        {
        }

        public RectFloat(Vector2 position, Vector2 size)
        {
            left = position.X;
            top = position.Y;
            right = position.X + size.X;
            bottom = position.Y + size.Y;
        }

        public RectFloat(float left, float right, float top, float bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        public readonly Vector2 Min => new Vector2(left, top);

        public readonly Vector2 Max => new Vector2(right, bottom);

        public readonly float Width => right - left;

        public readonly float Height => bottom - top;
    }
}
