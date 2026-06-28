using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Generic render queue for more efficient data checks
/// </summary>
/// <typeparam name="T">The component type</typeparam>
public class GenericRenderQueue<T> : IRenderQueue where T: IComponent
{
    /// <summary>
    /// An item of this render queue
    /// </summary>
    public struct Item
    {
        public Entity entity;
        public Transform transform;
        public T component;
    }

    private readonly List<Item> items = [];

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
}
