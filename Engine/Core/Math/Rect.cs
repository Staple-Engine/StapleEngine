using System.Numerics;

namespace Staple
{
    public struct Rect
    {
        public Vector2Int position;
        public Vector2Int size;

        public Rect()
        {
        }

        public Rect(Vector2Int position, Vector2Int size)
        {
            this.position = position;
            this.size = size;
        }

        public Rect(int x, int y, int width, int height)
        {
            position = new(x, y);
            size = new(width, height);
        }

        public readonly Vector2Int Min => position;

        public readonly Vector2Int Max => position + size;

        public readonly int Width => size.X;

        public readonly int Height => size.Y;

        public readonly int X => position.X;

        public readonly int Y => position.Y;

        public readonly int XMin => position.X;

        public readonly int YMin => position.Y;

        public readonly int XMax => position.X + size.X;

        public readonly int YMax => position.Y + size.Y;
    }
}
