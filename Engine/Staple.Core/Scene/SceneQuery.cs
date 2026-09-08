using Staple.Internal;
using System;

namespace Staple;

public class SceneQuery<T>: ISceneQuery
    where T: Component
{
    private readonly ExpandableContainer<T> contents = new();

    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public Span<T> Contents => contents.Contents;

    public ref T this[int index] => ref Contents[index];

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public void WorldChanged(World world)
    {
        var result = Scene.Query<T>(includeDisabled);

        contents.Clear();

        foreach(var item in result)
        {
            contents.Add(item);
        }
    }
}

public class SceneQuery<T, T2>: ISceneQuery
    where T: Component
    where T2: Component
{
    private readonly ExpandableContainer<(T, T2)> contents = new();

    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public Span<(T, T2)> Contents => contents.Contents;

    public ref (T, T2) this[int index] => ref Contents[index];

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public void WorldChanged(World world)
    {
        var result = Scene.Query<T, T2>(includeDisabled);

        contents.Clear();

        foreach (var item in result)
        {
            contents.Add(item);
        }
    }
}

public class SceneQuery<T, T2, T3> : ISceneQuery
    where T : Component
    where T2 : Component
    where T3: Component
{
    private readonly ExpandableContainer<(T, T2, T3)> contents = new();

    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public Span<(T, T2, T3)> Contents => contents.Contents;

    public ref (T, T2, T3) this[int index] => ref Contents[index];

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public void WorldChanged(World world)
    {
        var result = Scene.Query<T, T2, T3>(includeDisabled);

        contents.Clear();

        foreach (var item in result)
        {
            contents.Add(item);
        }
    }
}

public class SceneQuery<T, T2, T3, T4> : ISceneQuery
    where T : Component
    where T2 : Component
    where T3 : Component
    where T4 : Component
{
    private readonly ExpandableContainer<(T, T2, T3, T4)> contents = new();

    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public Span<(T, T2, T3, T4)> Contents => contents.Contents;

    public (T, T2, T3, T4) this[int index] => Contents[index];

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public void WorldChanged(World world)
    {
        var result = Scene.Query<T, T2, T3, T4>(includeDisabled);

        contents.Clear();

        foreach (var item in result)
        {
            contents.Add(item);
        }
    }
}

public class SceneQuery<T, T2, T3, T4, T5> : ISceneQuery
    where T : Component
    where T2 : Component
    where T3 : Component
    where T4 : Component
    where T5 : Component
{
    private readonly ExpandableContainer<(T, T2, T3, T4, T5)> contents = new();

    private readonly bool includeDisabled;

    public int Length => contents.Length;

    public Span<(T, T2, T3, T4, T5)> Contents => contents.Contents;

    public ref (T, T2, T3, T4, T5) this[int index] => ref Contents[index];

    public SceneQuery(bool includeDisabled = false)
    {
        this.includeDisabled = includeDisabled;

        World.AddSceneQuery(this);
    }

    public void WorldChanged(World world)
    {
        var result = Scene.Query<T, T2, T3, T4, T5>(includeDisabled);

        contents.Clear();

        foreach (var item in result)
        {
            contents.Add(item);
        }
    }
}
