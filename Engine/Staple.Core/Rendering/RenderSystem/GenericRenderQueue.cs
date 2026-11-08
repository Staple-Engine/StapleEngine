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

    private readonly ExpandableContainer<Item> items = new(false);

    private readonly ExpandableContainer<IRenderQueue.Item> queueItems = new(false);

    private readonly bool IsRenderable = typeof(T).IsAssignableTo(typeof(Renderable));

    public bool Empty => items.Length == 0;

    public Span<Item> Items => items.Contents;

    public Span<IRenderQueue.Item> Renderables => queueItems.Contents;

    public void Add(Entity entity, Transform transform, object component)
    {
        if(component is not T c)
        {
            return;
        }

        ref var item = ref items.AddDefault();

        item ??= new();

        item.entity = entity;
        item.transform = transform;
        item.component = c;

        if(IsRenderable)
        {
            item.renderable = c as Renderable;

            ref var queueItem = ref queueItems.AddDefault();

            queueItem.entity = entity;
            queueItem.transform = transform;
            queueItem.renderable = item.renderable;
        }
    }

    public void Clear()
    {
        items.Clear();
        queueItems.Clear();
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
