using Staple.Internal;
using System.Collections;
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
public class EntityQuery<T> : ISceneQuery, IEnumerable<T>
    where T : IComponent
{
    private T[] contents = [];
    private T content;
    private readonly EntityQueryMode queryMode;
    private readonly Entity target;

    public int Length => contents.Length;

    public T Content => content;

    public T this[int index] => contents[index];

    public EntityQuery(Entity target, EntityQueryMode queryMode)
    {
        this.target = target;
        this.queryMode = queryMode;

        World.AddSceneQuery(this);
    }

    public void WorldChanged()
    {
        content = default;

        if (target.IsValid == false)
        {
            contents = [];

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
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var result in contents)
        {
            yield return result;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        foreach (var result in contents)
        {
            yield return result;
        }
    }
}
