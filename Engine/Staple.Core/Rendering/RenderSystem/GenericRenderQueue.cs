using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Generic render queue for more efficient data checks
/// </summary>
/// <typeparam name="T">The component type</typeparam>
public class GenericRenderQueue<T> : IRenderQueue where T: Component
{
    /// <summary>
    /// An item of this render queue
    /// </summary>
    public class Item
    {
        public Entity entity;
        public Transform transform;
        public T component;
        public Renderable renderable;
    }

    private readonly List<Item> items = [];

    private readonly bool IsRenderable = typeof(T).IsAssignableTo(typeof(Renderable));

    public bool Empty => items.Count == 0;

    /// <summary>
    /// Gets all items in a more efficient way than querying the internal list each iteration
    /// </summary>
    public Span<Item> Items => CollectionsMarshal.AsSpan(items);

    public void Add(Entity entity, Transform transform, object item)
    {
        if(item is not T component)
        {
            return;
        }

        items.Add(new()
        {
            entity = entity,
            transform = transform,
            component = component,
            renderable = component as Renderable,
        });
    }

    public void Clear()
    {
        items.Clear();
    }

    public void IterateRenderables(Action<Entity, Transform, Renderable> callback)
    {
        if(typeof(T) != typeof(Renderable) && !typeof(T).IsSubclassOf(typeof(Renderable)))
        {
            return;
        }

        var items = Items;

        foreach(var i in items)
        {
            callback(i.entity, i.transform, i.component as Renderable);
        }
    }

    public void Sort(Vector3 origin, RenderableSortMode sortMode)
    {
        if(!IsRenderable || sortMode == RenderableSortMode.None)
        {
            return;
        }

        items.Sort((a, b) => RenderSystem.RenderQueueSortComparer(a.renderable.isVisible, b.renderable.isVisible,
            a.transform.Position, b.transform.Position, origin, sortMode));
    }
}
