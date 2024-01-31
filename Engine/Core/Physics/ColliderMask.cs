using MessagePack;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staple
{
    /// <summary>
    /// Represents a collider mask, which specifies which layers may collider with each other
    /// </summary>
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

            internal Item Clone()
            {
                return new()
                {
                    A = A,
                    B = B,
                    value = value,
                };
            }
        }

        internal static List<Item> collideMask = new();

        /// <summary>
        /// Checks whether two layer indices should collider with each other
        /// </summary>
        /// <param name="A">The first layer</param>
        /// <param name="B">The second layer</param>
        /// <returns>Whether the layers collide</returns>
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

    [JsonSourceGenerationOptions(IncludeFields = true)]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(ColliderMask.Item))]
    internal partial class ColliderMaskItemSerializationContext : JsonSerializerContext
    {
    }
}
