using System;
using System.Numerics;

namespace Staple;

/// <summary>
/// Render Queue interface for abstracting the generic ones. You typically want to just use <see cref="GenericRenderQueue{T}"/> queue.
/// </summary>
public interface IRenderQueue
{
    public struct Item
    {
        public Entity entity;
        public Transform transform;
        public Renderable renderable;
    }

    /// <summary>
    /// Whether the render queue is empty
    /// </summary>
    bool Empty { get; }

    /// <summary>
    /// A list of all renderables in this queue. Not sorted with <see cref="Sort(Vector3, RenderableSortMode)"/>
    /// </summary>
    Span<Item> Renderables { get; }

    /// <summary>
    /// Adds an item to the render queue
    /// </summary>
    /// <param name="entity">The entity to add</param>
    /// <param name="transform">The transform to add</param>
    /// <param name="component">The component to add</param>
    void Add(Entity entity, Transform transform, object component);

    /// <summary>
    /// Clears the render queue
    /// </summary>
    void Clear();

    /// <summary>
    /// Sorts the renderables, based on an origin point and a sort mode
    /// </summary>
    /// <param name="origin">The origin point</param>
    /// <param name="sortMode">The sort mode</param>
    void Sort(Vector3 origin, RenderableSortMode sortMode);
}
