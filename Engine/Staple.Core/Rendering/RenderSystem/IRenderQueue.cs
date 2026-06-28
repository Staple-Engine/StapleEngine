using System;

namespace Staple;

/// <summary>
/// Render Queue interface for abstracting the generic ones. You typically want to just use <see cref="GenericRenderQueue{T}"/> queue.
/// </summary>
public interface IRenderQueue
{
    /// <summary>
    /// Whether the render queue is empty
    /// </summary>
    bool Empty { get; }

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
    /// Iterates the renderables in this render queue. Will not do anything if the contained items are not renderabloes
    /// </summary>
    /// <param name="callback">A callback that will be called for each renderable in the render queue</param>
    void IterateRenderables(Action<Entity, Transform, Renderable> callback);
}
