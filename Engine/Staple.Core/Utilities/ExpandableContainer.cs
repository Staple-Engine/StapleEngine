using System;
using System.Runtime.CompilerServices;

namespace Staple;

/// <summary>
/// An expandable container for a specific type of element.
/// Unlike a regular list or array, this is optimized to never actually clear its data
/// and to not act as IEnumerable, providing the raw array instead, for maximum performance.
/// This allows for fast reusable iterations with varying amounts of elements over each frame,
/// reallocating the least amount possible.
/// </summary>
/// <typeparam name="T">The type to use</typeparam>
public class ExpandableContainer<T>
{
    private T[] contents = [];

    /// <summary>
    /// The amount of elements contained
    /// </summary>
    public int Length { get; private set; } = 0;

    /// <summary>
    /// Gets the current contents.
    /// </summary>
    public Span<T> Contents => contents.AsSpan(0, Length);

    /// <summary>
    /// Clears the contents
    /// </summary>
    /// <remarks>This doesn't actually deallocate memory, just sets the length to 0.</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Length = 0;
    }

    /// <summary>
    /// Adds an item to the container
    /// </summary>
    /// <param name="item">The item to add</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        if(Length + 1 >= contents.Length)
        {
            Array.Resize(ref contents, (Length + 1) * 2);
        }

        contents[Length++] = item;
    }
}
