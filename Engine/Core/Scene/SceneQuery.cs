using Staple.Internal;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Staple;

public class SceneQuery<T>: ISceneQuery, IEnumerable<(Entity, T)> 
    where T: IComponent
{
    private (Entity, T)[] contents = [];
    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T) this[int index] => contents[index];

    public void IterateThreaded(Action<(Entity, T), int> callback)
    {
        if(Length == 0)
        {
            return;
        }

        World.IterateThreaded(contents, callback);
    }

    public void WorldChanged()
    {
        contents = Scene.Query<T>(includeDisabled);
    }

    public IEnumerator<(Entity, T)> GetEnumerator()
    {
        foreach(var result in contents)
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

public class SceneQuery<T, T2>: ISceneQuery, IEnumerable<(Entity, T, T2)> 
    where T: IComponent
    where T2: IComponent
{
    private (Entity, T, T2)[] contents = [];
    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2) this[int index] => contents[index];

    public void IterateThreaded(Action<(Entity, T, T2), int> callback)
    {
        if (Length == 0)
        {
            return;
        }

        World.IterateThreaded(contents, callback);
    }

    public void WorldChanged()
    {
        contents = Scene.Query<T, T2>(includeDisabled);
    }

    public IEnumerator<(Entity, T, T2)> GetEnumerator()
    {
        foreach(var result in contents)
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

public class SceneQuery<T, T2, T3> : ISceneQuery, IEnumerable<(Entity, T, T2, T3)>
    where T : IComponent
    where T2 : IComponent
    where T3: IComponent
{
    private (Entity, T, T2, T3)[] contents = [];
    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2, T3) this[int index] => contents[index];

    public void IterateThreaded(Action<(Entity, T, T2, T3), int> callback)
    {
        if (Length == 0)
        {
            return;
        }

        World.IterateThreaded(contents, callback);
    }

    public void WorldChanged()
    {
        contents = Scene.Query<T, T2, T3>(includeDisabled);
    }

    public IEnumerator<(Entity, T, T2, T3)> GetEnumerator()
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

public class SceneQuery<T, T2, T3, T4> : ISceneQuery, IEnumerable<(Entity, T, T2, T3, T4)>
    where T : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    private (Entity, T, T2, T3, T4)[] contents = [];
    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2, T3, T4) this[int index] => contents[index];

    public void IterateThreaded(Action<(Entity, T, T2, T3, T4), int> callback)
    {
        if (Length == 0)
        {
            return;
        }

        World.IterateThreaded(contents, callback);
    }

    public void WorldChanged()
    {
        contents = Scene.Query<T, T2, T3, T4>(includeDisabled);
    }

    public IEnumerator<(Entity, T, T2, T3, T4)> GetEnumerator()
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

public class SceneQuery<T, T2, T3, T4, T5> : ISceneQuery, IEnumerable<(Entity, T, T2, T3, T4, T5)>
    where T : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
{
    internal (Entity, T, T2, T3, T4, T5)[] contents = [];

    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2, T3, T4, T5) this[int index] => contents[index];

    public void IterateThreaded(Action<(Entity, T, T2, T3, T4, T5), int> callback)
    {
        if (Length == 0)
        {
            return;
        }

        World.IterateThreaded(contents, callback);
    }

    public void WorldChanged()
    {
        contents = Scene.Query<T, T2, T3, T4, T5>(includeDisabled);
    }

    public IEnumerator<(Entity, T, T2, T3, T4, T5)> GetEnumerator()
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
