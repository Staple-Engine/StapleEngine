using Staple.Internal;
using System;

namespace Staple;

public class SceneQuery<T>: ISceneQuery
    where T: IComponent
{
    private readonly bool includeDisabled;

    public int Length => Contents.Length;

    public (Entity, T)[] Contents { get; private set; } = [];

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T) this[int index] => Contents[index];

    public void WorldChanged()
    {
        Contents = Scene.Query<T>(includeDisabled);
    }
}

public class SceneQuery<T, T2>: ISceneQuery
    where T: IComponent
    where T2: IComponent
{
    private readonly bool includeDisabled;

    public int Length => Contents.Length;

    public (Entity, T, T2)[] Contents { get; private set; } = [];

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2) this[int index] => Contents[index];

    public void WorldChanged()
    {
        Contents = Scene.Query<T, T2>(includeDisabled);
    }
}

public class SceneQuery<T, T2, T3> : ISceneQuery
    where T : IComponent
    where T2 : IComponent
    where T3: IComponent
{
    private readonly bool includeDisabled;

    public int Length => Contents.Length;

    public (Entity, T, T2, T3)[] Contents { get; private set; } = [];

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2, T3) this[int index] => Contents[index];

    public void WorldChanged()
    {
        Contents = Scene.Query<T, T2, T3>(includeDisabled);
    }
}

public class SceneQuery<T, T2, T3, T4> : ISceneQuery
    where T : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    private readonly bool includeDisabled;

    public int Length => Contents.Length;

    public (Entity, T, T2, T3, T4)[] Contents { get; private set; } = [];

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2, T3, T4) this[int index] => Contents[index];

    public void WorldChanged()
    {
        Contents = Scene.Query<T, T2, T3, T4>(includeDisabled);
    }
}

public class SceneQuery<T, T2, T3, T4, T5> : ISceneQuery
    where T : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
    where T5 : IComponent
{
    private readonly bool includeDisabled;

    public int Length => Contents.Length;

    public (Entity, T, T2, T3, T4, T5)[] Contents { get; private set; } = [];

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public (Entity, T, T2, T3, T4, T5) this[int index] => Contents[index];

    public void WorldChanged()
    {
        Contents = Scene.Query<T, T2, T3, T4, T5>(includeDisabled);
    }
}
