using Staple.Internal;
using System.Collections;
using System.Collections.Generic;

namespace Staple;

public enum EntityQueryMode
{
    Self,
    Parent,
    Children,
    SelfAndChildren,
}

public class EntityQuery<T> : ISceneQuery, IEnumerable<T>
    where T : IComponent
{
    private T[] contents = [];
    private readonly EntityQueryMode queryMode;
    private readonly Entity target;

    public int Length => contents.Length;

    public EntityQuery(Entity target, EntityQueryMode queryMode)
    {
        this.target = target;
        this.queryMode = queryMode;

        World.AddSceneQuery(this);
    }

    public T this[int index] => contents[index];

    public void WorldChanged()
    {
        if(target.IsValid == false)
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
