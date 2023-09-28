using MessagePack;

namespace Staple
{
    [MessagePackObject]
    public struct Rect
    {
        [Key(0)]
        public int left;

        [Key(1)]
        public int right;

        [Key(2)]
        public int top;

        [Key(3)]
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

        [IgnoreMember]
        public readonly Vector2Int Min => new(left, top);

        [IgnoreMember]
        public readonly Vector2Int Max => new(right, bottom);

        [IgnoreMember]
        public int Width
        {
            readonly get => right - left;

            set
            {
                right = value + left;
            }
        }

        [IgnoreMember]
        public int Height
        {
            readonly get => bottom - top;

            set
            {
                bottom = value + top;
            }
        }

        public bool ShouldSerializeMin() => false;

        public bool ShouldSerializeMax() => false;

        public bool ShouldSerializeWidth() => false;

        public bool ShouldSerializeHeight() => false;
    }
}
