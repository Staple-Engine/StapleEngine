using MessagePack;
using System.Collections.Generic;

namespace Staple
{
    public class ColliderMask
    {
        [MessagePackObject]
        public class Item
        {
            [Key(0)]
            public int A;

            [Key(1)]
            public int B;

            [Key(2)]
            public bool value;
        }

        internal static List<Item> collideMask = new();

        public static bool ShouldCollide(int A, int B)
        {
            var t = collideMask.Find(x => x.A == A && x.B == B);

            if(t == null)
            {
                return true;
            }

            return t.value;
        }
    }
}
