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
    private T[] contents;

    private readonly bool ShouldResetOnClear = typeof(T).IsClass;

    /// <summary>
    /// The amount of elements contained
    /// </summary>
    public int Length { get; private set; } = 0;

    /// <summary>
    /// How much storage capacity the container has
    /// </summary>
    public int Capacity => contents.Length;

    /// <summary>
    /// Gets the current contents.
    /// </summary>
    public Span<T> Contents => contents.AsSpan(0, Length);

    /// <summary>
    /// Gets the raw contents, including any extra space. Necessary for raw pointer access.
    /// </summary>
    public T[] RawContents => contents;

    public ExpandableContainer()
    {
        contents = new T[1024];
    }

    public ExpandableContainer(int length)
    {
        contents = new T[length];

        Length = length;
    }

    /// <summary>
    /// Clears the contents
    /// </summary>
    /// <remarks>This doesn't actually deallocate memory, just sets the length to 0.
    /// If the type is a class, the contents will be marked as null to allow them to be collected by GC</remarks>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        Length = 0;

        if (!ShouldResetOnClear)
        {
            return;
        }

        ClearValues();
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
            Resize(Length + 1, true);

            contents[Length - 1] = item;

            return;
        }

        contents[Length++] = item;
    }

    /// <summary>
    /// Adds a range of items to the container
    /// </summary>
    /// <param name="items">The items to add</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddRange(Span<T> items)
    {
        if (Length + items.Length >= contents.Length)
        {
            Resize(Length + 1, true);

            items.CopyTo(contents.AsSpan(Length - items.Length, items.Length));

            return;
        }

        items.CopyTo(contents.AsSpan(Length, items.Length));

        Length += items.Length;
    }

    /// <summary>
    /// Resizes this container to a specific size. The size is guaranteed to be the same or larger.
    /// </summary>
    /// <param name="newSize">The new size</param>
    /// <param name="copyElements">Whether to copy the previous elements over</param>
    /// <remarks>May not resize if the requested size is less or equal to the <see cref="Capacity"/></remarks>
    public void Resize(int newSize, bool copyElements)
    {
        Length = newSize;

        if (newSize <= contents.Length)
        {
            return;
        }

        var targetSize = contents.Length;

        while(newSize > targetSize)
        {
            targetSize *= 2;
        }

        if (copyElements)
        {
            Array.Resize(ref contents, targetSize);
        }
        else
        {
            contents = new T[targetSize];
        }
    }

    /// <summary>
    /// Clears all values in this container to their default value
    /// </summary>
    public void ClearValues()
    {
        var defaultValue = default(T);

        for(var i = 0; i < contents.Length; i++)
        {
            contents[i] = defaultValue;
        }
    }
}
