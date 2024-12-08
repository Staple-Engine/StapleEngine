using Staple.Internal;
using System;

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
            EntityQueryMode.Self => [target.GetComponent(typeof(T))],
            EntityQueryMode.Parent => [target.GetComponentInParent(typeof(T))],
            EntityQueryMode.Children => target.GetComponentsInChildren(typeof(T), false),
            EntityQueryMode.SelfAndChildren => target.GetComponentsInChildren(typeof(T)),
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
