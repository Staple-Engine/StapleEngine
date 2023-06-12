using MessagePack;
using System;
using System.Collections.Generic;

namespace Staple
{
    public class ColliderMask
    {
        public class Item
        {
            public int A;
            public int B;
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
