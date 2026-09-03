using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple;

public enum EntityQueryMode
{
    /// <summary>
    /// Get the component from self
    /// </summary>
    Self,

    /// <summary>
    /// Get the component from the closest parent
    /// </summary>
    Parent,

    /// <summary>
    /// Gets the component from self or the closest parent
    /// </summary>
    SelfAndParent,

    /// <summary>
    /// Get multiple components from children
    /// </summary>
    Children,

    /// <summary>
    /// Get multiple components from self or children
    /// </summary>
    SelfAndChildren,
}

/// <summary>
/// Automatically queries for components related to an entity and stores the result.
/// It automatically updates as the world changes.
/// </summary>
/// <typeparam name="T">A type of component to get</typeparam>
public sealed class EntityQuery<T> : ISceneQuery
    where T : IComponent
{
    public struct EntityItem(Entity entity, T item)
    {
        public Entity entity = entity;
        public T item = item;

        public readonly bool IsValid => entity.IsValid && item != null;

        public void Deconstruct(out Entity e, out T i)
        {
            e = entity;
            i = item;
        }
    }

    private readonly EntityQueryMode queryMode;
    private readonly Entity target;
    private readonly bool getEntities;

    private readonly ExpandableContainer<T> contents = new();

    private readonly ExpandableContainer<EntityItem> contentEntities = new();

    /// <summary>
    /// Contained content. Only valid if we have a single element.
    /// </summary>
    public T Content { get; private set; }

    /// <summary>
    /// Contained content. Only valid if we have a single element.
    /// </summary>
    public Span<T> Contents => contents.Contents;

    /// <summary>
    /// The content with its entity, if available.
    /// </summary>
    public EntityItem ContentEntity { get; private set; }

    /// <summary>
    /// The content with its entity, if available.
    /// </summary>
    public Span<EntityItem> ContentEntities => contentEntities.Contents;

    /// <summary>
    /// Gets an entity and component at a specific index
    /// </summary>
    /// <param name="index">The index to get at</param>
    /// <returns>The entity and component as a tuple, if valid</returns>
    public ref EntityItem ContentEntityAt(int index) => ref ContentEntities[index];

    /// <summary>
    /// Creates an entity query for a specific entity.
    /// </summary>
    /// <param name="target">The target entity</param>
    /// <param name="queryMode">The query mode</param>
    /// <param name="getEntities">Whether to get the component entities as well</param>
    public EntityQuery(Entity target, EntityQueryMode queryMode, bool getEntities)
    {
        this.target = target;
        this.queryMode = queryMode;
        this.getEntities = getEntities;

        World.AddSceneQuery(this);
    }

    /// <summary>
    /// Unregisters this scene query from the world
    /// </summary>
    public void Unregister()
    {
        World.RemoveSceneQuery(this);

        Content = default;
        ContentEntity = default;

        contents.Clear();
        contentEntities.Clear();
    }

    public void WorldChanged(World world)
    {
        Content = default;
        ContentEntity = default;

        contents.Clear();
        contentEntities.Clear();

        if (!target.IsValid)
        {
            return;
        }

        switch (queryMode)
        {
            case EntityQueryMode.Self:

                {
                    if (target.TryGetComponent<T>(out var t))
                    {
                        contents.Add(t);
                    }
                }

                break;

            case EntityQueryMode.Parent:

                {
                    var transform = target.GetComponent<Transform>();

                    if (transform?.Parent != null)
                    {
                        var current = transform.Parent;

                        while (current != null)
                        {
                            if (current.Entity.TryGetComponent<T>(out var t))
                            {
                                contents.Add(t);

                                break;
                            }

                            current = current.Parent;
                        }
                    }
                }

                break;

            case EntityQueryMode.SelfAndParent:

                {
                    if (target.TryGetComponent<T>(out var t))
                    {
                        contents.Add(t);
                    }

                    var transform = target.GetComponent<Transform>();

                    if (transform?.Parent != null)
                    {
                        var current = transform.Parent;

                        while (current != null)
                        {
                            if (current.Entity.TryGetComponent<T>(out t))
                            {
                                contents.Add(t);

                                break;
                            }

                            current = current.Parent;
                        }
                    }
                }

                break;

            case EntityQueryMode.Children:

                {
                    var transform = target.GetComponent<Transform>();

                    void Recursive(Transform transform)
                    {
                        if (transform == null)
                        {
                            return;
                        }

                        if (transform.Entity.TryGetComponent<T>(out var t))
                        {
                            contents.Add(t);
                        }

                        foreach (var child in transform.Children)
                        {
                            Recursive(child);
                        }
                    }

                    if (transform != null)
                    {
                        foreach (var child in transform.Children)
                        {
                            Recursive(child);
                        }
                    }
                }

                break;

            case EntityQueryMode.SelfAndChildren:

                {
                    if (target.TryGetComponent<T>(out var t))
                    {
                        contents.Add(t);
                    }

                    var transform = target.GetComponent<Transform>();

                    void Recursive(Transform transform)
                    {
                        if (transform == null)
                        {
                            return;
                        }

                        if (transform.Entity.TryGetComponent<T>(out var t))
                        {
                            contents.Add(t);
                        }

                        foreach (var child in transform.Children)
                        {
                            Recursive(child);
                        }
                    }

                    if (transform != null)
                    {
                        foreach (var child in transform.Children)
                        {
                            Recursive(child);
                        }
                    }
                }

                break;
        }

        if (Contents.Length == 1)
        {
            Content = Contents[0];
        }

        if (!getEntities)
        {
            return;
        }

        foreach (ref var item in Contents)
        {
            var entity = world.GetComponentEntity(item);

            if (!entity.IsValid)
            {
                continue;
            }

            contentEntities.Add(new(entity, item));
        }

        if (contentEntities.Length == 1)
        {
            ContentEntity = ContentEntities[0];
        }
    }
}

/// <summary>
/// Automatically queries for components related to an entity and stores the result.
/// It automatically updates as the world changes.
/// </summary>
/// <typeparam name="T">A type of component to get</typeparam>
/// <typeparam name="T2">A type of component to get</typeparam>
public sealed class EntityQuery<T, T2> : ISceneQuery
    where T : IComponent
    where T2 : IComponent
{
    public struct Item(T first, T2 second)
    {
        public T first = first;
        public T2 second = second;

        public readonly bool IsValid => first != null && second != null;

        public void Deconstruct(out T f, out T2 s)
        {
            f = first;
            s = second;
        }
    }

    public struct EntityItem(Entity entity, T first, T2 second)
    {
        public Entity entity = entity;
        public T first = first;
        public T2 second = second;

        public readonly bool IsValid => entity.IsValid && first != null && second != null;

        public void Deconstruct(out Entity e, out T f, out T2 s)
        {
            e = entity;
            f = first;
            s = second;
        }
    }

    private readonly EntityQueryMode queryMode;
    private readonly Entity target;
    private readonly bool getEntities;

    private readonly ExpandableContainer<Item> contents = new();

    private readonly ExpandableContainer<EntityItem> contentEntities = new();

    /// <summary>
    /// Contained content. Only valid if we have a single element.
    /// </summary>
    public Item Content { get; private set; }

    /// <summary>
    /// Contained content. Only valid if we have a single element.
    /// </summary>
    public Span<Item> Contents => contents.Contents;

    /// <summary>
    /// The content with its entity, if available.
    /// </summary>
    public EntityItem ContentEntity { get; private set; }

    /// <summary>
    /// The content with its entity, if available.
    /// </summary>
    public Span<EntityItem> ContentEntities => contentEntities.Contents;

    /// <summary>
    /// Gets an entity and component at a specific index
    /// </summary>
    /// <param name="index">The index to get at</param>
    /// <returns>The entity and component as a tuple, if valid</returns>
    public ref EntityItem ContentEntityAt(int index) => ref ContentEntities[index];

    /// <summary>
    /// Creates an entity query for a specific entity.
    /// </summary>
    /// <param name="target">The target entity</param>
    /// <param name="queryMode">The query mode</param>
    /// <param name="getEntities">Whether to get the component entities as well</param>
    public EntityQuery(Entity target, EntityQueryMode queryMode, bool getEntities)
    {
        this.target = target;
        this.queryMode = queryMode;
        this.getEntities = getEntities;

        World.AddSceneQuery(this);
    }

    /// <summary>
    /// Unregisters this scene query from the world
    /// </summary>
    public void Unregister()
    {
        World.RemoveSceneQuery(this);

        Content = default;
        ContentEntity = default;

        contents.Clear();
        contentEntities.Clear();
    }

    public void WorldChanged(World world)
    {
        Content = default;
        ContentEntity = default;

        contents.Clear();
        contentEntities.Clear();

        if (!target.IsValid)
        {
            return;
        }

        switch(queryMode)
        {
            case EntityQueryMode.Self:

                {
                    if (target.TryGetComponent<T>(out var t) && target.TryGetComponent<T2>(out var t2))
                    {
                        contents.Add(new(t, t2));
                    }
                }

                break;

            case EntityQueryMode.Parent:

                {
                    var transform = target.GetComponent<Transform>();

                    if(transform?.Parent != null)
                    {
                        var current = transform.Parent;

                        while(current != null)
                        {
                            if(current.Entity.TryGetComponent<T>(out var t) &&
                                current.Entity.TryGetComponent<T2>(out var t2))
                            {
                                contents.Add(new(t, t2));

                                break;
                            }

                            current = current.Parent;
                        }
                    }
                }

                break;

            case EntityQueryMode.SelfAndParent:

                {
                    if (target.TryGetComponent<T>(out var t) && target.TryGetComponent<T2>(out var t2))
                    {
                        contents.Add(new(t, t2));
                    }

                    var transform = target.GetComponent<Transform>();

                    if (transform?.Parent != null)
                    {
                        var current = transform.Parent;

                        while (current != null)
                        {
                            if (current.Entity.TryGetComponent<T>(out t) &&
                                current.Entity.TryGetComponent<T2>(out t2))
                            {
                                contents.Add(new(t, t2));

                                break;
                            }

                            current = current.Parent;
                        }
                    }
                }

                break;

            case EntityQueryMode.Children:

                {
                    var transform = target.GetComponent<Transform>();

                    void Recursive(Transform transform)
                    {
                        if(transform == null)
                        {
                            return;
                        }

                        if (transform.Entity.TryGetComponent<T>(out var t) &&
                            transform.Entity.TryGetComponent<T2>(out var t2))
                        {
                            contents.Add(new(t, t2));
                        }

                        foreach (var child in transform.Children)
                        {
                            Recursive(child);
                        }
                    }

                    if(transform != null)
                    {
                        foreach(var child in transform.Children)
                        {
                            Recursive(child);
                        }
                    }
                }

                break;

            case EntityQueryMode.SelfAndChildren:

                {
                    if (target.TryGetComponent<T>(out var t) && target.TryGetComponent<T2>(out var t2))
                    {
                        contents.Add(new(t, t2));
                    }

                    var transform = target.GetComponent<Transform>();

                    void Recursive(Transform transform)
                    {
                        if (transform == null)
                        {
                            return;
                        }

                        if (transform.Entity.TryGetComponent<T>(out var t) &&
                            transform.Entity.TryGetComponent<T2>(out var t2))
                        {
                            contents.Add(new(t, t2));
                        }

                        foreach (var child in transform.Children)
                        {
                            Recursive(child);
                        }
                    }

                    if (transform != null)
                    {
                        foreach (var child in transform.Children)
                        {
                            Recursive(child);
                        }
                    }
                }

                break;
        }

        if (Contents.Length == 1)
        {
            Content = Contents[0];
        }

        if (!getEntities)
        {
            return;
        }

        foreach(ref var item in Contents)
        {
            var entity = world.GetComponentEntity(item.first);

            if(!entity.IsValid)
            {
                continue;
            }

            contentEntities.Add(new(entity, item.first, item.second));
        }

        if (contentEntities.Length == 1)
        {
            ContentEntity = ContentEntities[0];
        }
    }
}
