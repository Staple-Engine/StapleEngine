using System.Runtime.CompilerServices;

namespace Staple
{
    /// <summary>
    /// Int-based lookup cache.
    /// Optimizewd for fast access, will be slow to add.
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

        public int Length => indices.Length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int IndexOf(int hash)
        {
            var l = indices.Length;

            for (var i = 0; i < l; i++)
            {
                if (indices[i] == hash)
                {
                    return i;
                }
            }

            return -1;
        }

        public void Add(int hash, T item)
        {
            var l = indices.Length;
            var newIndices = new int[l + 1];
            var newStorage = new T[newIndices.Length];

            for (var i = 0; i < l; i++)
            {
                newIndices[i] = indices[i];
                newStorage[i] = storage[i];
            }

            newIndices[indices.Length] = hash;
            newStorage[indices.Length] = item;

            indices = newIndices;
            storage = newStorage;
        }
    }
}
