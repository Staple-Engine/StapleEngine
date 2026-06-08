using System;
using System.Runtime.CompilerServices;

namespace Staple
{
    /// <summary>
    /// Int-based lookup cache.
    /// Optimized for fast access, will be slow to add.
    /// </summary>
    /// <typeparam name="T">The type to store</typeparam>
    internal class IntLookupCache<T>
    {
        public T[] storage = [];
        public int[] indices = [];

        public T this[int index]
        {
            get => storage[index];

            set => storage[index] = value;
        }

        public int Length { get; private set; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int hash)
        {
            var l = Length;

            for (var i = 0; i < l; i++)
            {
                if (indices[i] == hash)
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetValue(int hash, out T value)
        {
            var l = Length;

            for (var i = 0; i < l; i++)
            {
                if (indices[i] == hash)
                {
                    value = storage[i];

                    return true;
                }
            }

            value = default;

            return false;
        }

        public void Add(int hash, T item)
        {
            if(Length + 1 >= indices.Length)
            {
                Array.Resize(ref storage, (indices.Length + 1) * 2);
                Array.Resize(ref indices, (indices.Length + 1) * 2);
            }

            indices[Length] = hash;
            storage[Length++] = item;
        }
    }
}
