using System.Numerics;

namespace Staple
{
    public struct RectFloat
    {
        public Vector2 position;
        public Vector2 size;

        public RectFloat()
        {
        }

        public RectFloat(Vector2 position, Vector2 size)
        {
            this.position = position;
            this.size = size;
        }

        public RectFloat(float x, float y, float width, float height)
        {
            position = new(x, y);
            size = new(width, height);
        }

        public readonly Vector2 Min => position;

        public readonly Vector2 Max => position + size;

        public readonly float Width => size.X;

        public readonly float Height => size.Y;

        public readonly float X => position.X;

        public readonly float Y => position.Y;

        public readonly float XMin => position.X;

        public readonly float YMin => position.Y;

        public readonly float XMax => position.X + size.X;

        public readonly float YMax => position.Y + size.Y;
    }
}
