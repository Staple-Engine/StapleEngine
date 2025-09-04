using Staple.Internal;
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
    private T[] contents = [];
    private T content;
    private (Entity, T)[] contentEntities = [];
    private (Entity, T) contentEntity;

    private readonly EntityQueryMode queryMode;
    private readonly Entity target;
    private readonly bool getEntities;

    public int Length => contents.Length;

    /// <summary>
    /// Contained content. Only valid if we have a single element.
    /// </summary>
    public T Content => content;

    /// <summary>
    /// Contained content. Only valid if we have a single element.
    /// </summary>
    public T[] Contents => contents;

    /// <summary>
    /// The content with its entity, if available.
    /// </summary>
    public (Entity, T) ContentEntity => contentEntity;

    /// <summary>
    /// The content with its entity, if available.
    /// </summary>
    public (Entity, T)[] ContentEntities => contentEntities;

    public T this[int index] => contents[index];

    /// <summary>
    /// Gets an entity and component at a specific index
    /// </summary>
    /// <param name="index">The index to get at</param>
    /// <returns>The entity and component as a tuple, if valid</returns>
    public (Entity, T) ContentEntityAt(int index) => index >= 0 && index < contentEntities.Length ? contentEntities[index] : default;

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

        content = default;
        contentEntity = default;

        contents = [];
        contentEntities = [];
    }

    public void WorldChanged()
    {
        content = default;
        contentEntity = default;

        contents = [];
        contentEntities = [];

        if (target.IsValid == false)
        {
            return;
        }

        var items = queryMode switch
        {
            EntityQueryMode.Self => [target.GetComponent<T>()],
            EntityQueryMode.Parent => [target.GetComponentInParent<T>()],
            EntityQueryMode.SelfAndParent => [target.GetComponent<T>(), target.GetComponentInParent<T>()],
            EntityQueryMode.Children => target.GetComponentsInChildren<T>(false),
            EntityQueryMode.SelfAndChildren => target.GetComponentsInChildren<T>(true),
            _ => [],
        };

        var count = 0;

        for(var i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                continue;
            }

            count++;
        }

        contents = new T[count];

        for(int i = 0, counter = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                continue;
            }

            contents[counter++] = (T)items[i];
        }

        if(count == 1 && contents[0] != null)
        {
            content = contents[0];
        }

        if(getEntities)
        {
            contentEntities = new (Entity, T)[count];

            for(var i = 0; i < items.Length; i++)
            {
                contentEntities[i] = (World.Current.GetComponentEntity(contents[i]), contents[i]);
            }

            if(count == 1 && contents[0] != null)
            {
                contentEntity = contentEntities[0];
            }
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
    private (T, T2)[] contents = [];
    private (T, T2) content;
    private (Entity, T, T2)[] contentEntities = [];
    private (Entity, T, T2) contentEntity;

    private readonly EntityQueryMode queryMode;
    private readonly Entity target;
    private readonly bool getEntities;

    public int Length => contents.Length;

    /// <summary>
    /// Contained content. Only valid if we have a single element.
    /// </summary>
    public (T, T2) Content => content;

    /// <summary>
    /// Contained content. Only valid if we have a single element.
    /// </summary>
    public (T, T2)[] Contents => contents;

    /// <summary>
    /// The content with its entity, if available.
    /// </summary>
    public (Entity, T, T2) ContentEntity => contentEntity;

    /// <summary>
    /// The content with its entity, if available.
    /// </summary>
    public (Entity, T, T2)[] ContentEntities => contentEntities;

    public (T, T2) this[int index] => contents[index];

    /// <summary>
    /// Gets an entity and component at a specific index
    /// </summary>
    /// <param name="index">The index to get at</param>
    /// <returns>The entity and component as a tuple, if valid</returns>
    public (Entity, T, T2) ContentEntityAt(int index) => index >= 0 && index < contentEntities.Length ? contentEntities[index] : default;

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

        content = default;
        contentEntity = default;

        contents = [];
        contentEntities = [];
    }

    public void WorldChanged()
    {
        content = default;
        contentEntity = default;

        contents = [];
        contentEntities = [];

        if (target.IsValid == false)
        {
            return;
        }

        var items = new List<(T, T2)>();

        switch(queryMode)
        {
            case EntityQueryMode.Self:

                {
                    if (target.TryGetComponent<T>(out var t) && target.TryGetComponent<T2>(out var t2))
                    {
                        items.Add((t, t2));
                    }
                }

                break;

            case EntityQueryMode.Parent:

                {
                    var transform = target.GetComponent<Transform>();

                    if(transform?.parent != null)
                    {
                        var current = transform.parent;

                        while(current != null)
                        {
                            if(current.entity.TryGetComponent<T>(out var t) && current.entity.TryGetComponent<T2>(out var t2))
                            {
                                items.Add((t, t2));

                                break;
                            }

                            current = current.parent;
                        }
                    }
                }

                break;

            case EntityQueryMode.SelfAndParent:

                {
                    if (target.TryGetComponent<T>(out var t) && target.TryGetComponent<T2>(out var t2))
                    {
                        items.Add((t, t2));
                    }

                    var transform = target.GetComponent<Transform>();

                    if (transform?.parent != null)
                    {
                        var current = transform.parent;

                        while (current != null)
                        {
                            if (current.entity.TryGetComponent<T>(out t) && current.entity.TryGetComponent<T2>(out t2))
                            {
                                items.Add((t, t2));

                                break;
                            }

                            current = current.parent;
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

                        if (transform.entity.TryGetComponent<T>(out var t) && transform.entity.TryGetComponent<T2>(out var t2))
                        {
                            items.Add((t, t2));
                        }

                        foreach(var child in transform.Children)
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
                        items.Add((t, t2));
                    }

                    var transform = target.GetComponent<Transform>();

                    void Recursive(Transform transform)
                    {
                        if (transform == null)
                        {
                            return;
                        }

                        if (transform.entity.TryGetComponent<T>(out var t) && transform.entity.TryGetComponent<T2>(out var t2))
                        {
                            items.Add((t, t2));
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

        contents = items.ToArray();

        if (contents.Length == 1)
        {
            content = contents[0];
        }

        if (getEntities)
        {
            contentEntities = new (Entity, T, T2)[items.Count];

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                contentEntities[i] = (World.Current.GetComponentEntity(item.Item1), item.Item1, item.Item2);
            }

            if (items.Count == 1)
            {
                contentEntity = contentEntities[0];
            }
        }
    }
}
