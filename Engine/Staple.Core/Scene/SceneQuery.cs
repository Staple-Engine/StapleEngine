using Staple.Internal;
using System;

namespace Staple;

public class SceneQuery<T>: ISceneQuery
    where T: IComponent
{
    private (Entity, T)[] contents = [];
    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public (Entity, T)[] Contents => contents;

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T) this[int index] => contents[index];

    public void WorldChanged()
    {
        contents = Scene.Query<T>(includeDisabled);
    }
}

public class SceneQuery<T, T2>: ISceneQuery
    where T: IComponent
    where T2: IComponent
{
    private (Entity, T, T2)[] contents = [];
    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public (Entity, T, T2)[] Contents => contents;

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2) this[int index] => contents[index];

    public void WorldChanged()
    {
        contents = Scene.Query<T, T2>(includeDisabled);
    }
}

public class SceneQuery<T, T2, T3> : ISceneQuery
    where T : IComponent
    where T2 : IComponent
    where T3: IComponent
{
    private (Entity, T, T2, T3)[] contents = [];
    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public (Entity, T, T2, T3)[] Contents => contents;

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2, T3) this[int index] => contents[index];

    public void WorldChanged()
    {
        contents = Scene.Query<T, T2, T3>(includeDisabled);
    }
}

public class SceneQuery<T, T2, T3, T4> : ISceneQuery
    where T : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    private (Entity, T, T2, T3, T4)[] contents = [];
    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public (Entity, T, T2, T3, T4)[] Contents => contents;

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2, T3, T4) this[int index] => contents[index];

    public void WorldChanged()
    {
        contents = Scene.Query<T, T2, T3, T4>(includeDisabled);
    }
}

public class SceneQuery<T, T2, T3, T4, T5> : ISceneQuery
    where T : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
{
    internal (Entity, T, T2, T3, T4, T5)[] contents = [];

    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public (Entity, T, T2, T3, T4, T5)[] Contents => contents;

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2, T3, T4, T5) this[int index] => contents[index];

    public void WorldChanged()
    {
        contents = Scene.Query<T, T2, T3, T4, T5>(includeDisabled);
    }
}
