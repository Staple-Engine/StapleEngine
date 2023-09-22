using System.Numerics;

namespace Staple
{
    public struct Rect
    {
        public int left;
        public int right;
        public int top;
        public int bottom;

        public Rect()
        {
        }

        public Rect(Vector2Int position, Vector2Int size)
        {
            left = position.X;
            top = position.Y;
            right = position.X + size.X;
            bottom = position.Y + size.Y;
        }

        public Rect(int left, int right, int top, int bottom)
        {
            this.left = left;
            this.right = right;
            this.top = top;
            this.bottom = bottom;
        }

        public readonly Vector2Int Min => new Vector2Int(left, top);

        public readonly Vector2Int Max => new Vector2Int(right, bottom);

        public readonly int Width => right - left;

        public readonly int Height => bottom - top;
    }
}
