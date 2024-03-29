using System;
using System.Diagnostics.CodeAnalysis;

namespace Staple;

/// <summary>
/// Represents an entity
/// </summary>
public partial struct Entity
{
    /// <summary>
    /// The entity's identifier, containing the ID and generation
    /// </summary>
    internal EntityID Identifier;

    /// <summary>
    /// Whether this entity is enabled
    /// </summary>
    public bool Enabled
    {
        get => World.Current?.IsEntityEnabled(this) ?? false;

        set => World.Current?.SetEntityEnabled(this, value);
    }

    /// <summary>
    /// The entity's name
    /// </summary>
    public string Name
    {
        get => World.Current?.GetEntityName(this);

        set => World.Current?.SetEntityName(this, value);
    }

    /// <summary>
    /// The entity's layer
    /// </summary>
    public uint Layer
    {
        get => World.Current?.GetEntityLayer(this) ?? 0;

        set => World.Current?.SetEntityLayer(this, value);
    }

    /// <summary>
    /// Checks if this entity is valid
    /// </summary>
    public bool IsValid => World.Current?.IsValidEntity(this) ?? false;

    public static bool operator==(Entity a, Entity b)
    {
        return a.Identifier == b.Identifier;
    }

    public static bool operator!=(Entity a, Entity b)
    {
        return (a == b) == false;
    }

    public override bool Equals(object obj)
    {
        if(obj is null)
        {
            return false;
        }

        if(obj is Entity entity)
        {
            return this == entity;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Identifier.GetHashCode();
    }

    public override string ToString()
    {
        var nameString = Name ?? "Unnamed Entity";

        return $"{nameString} {Identifier.ID}:{Identifier.generation}";
    }

    public void SetLayer(uint layer, bool recursive = false)
    {
        Layer = layer;

        if(recursive && TryGetComponent<Transform>(out var transform))
        {
            foreach (var child in transform)
            {
                child.entity.SetLayer(layer, true);
            }
        }
    }

    public static Entity Create(params Type[] componentTypes)
    {
        if (World.Current == null)
        {
            return default;
        }

        var entity = World.Current.CreateEntity();

        if(entity.IsValid == false)
        {
            return default;
        }

        foreach(var component in componentTypes)
        {
            entity.AddComponent(component);
        }

        return entity;
    }

    public static Entity Create(string name, params Type[] componentTypes)
    {
        if(World.Current == null)
        {
            return default;
        }

        var entity = World.Current.CreateEntity();

        if (entity.IsValid == false)
        {
            return default;
        }

        entity.Name = name;

        foreach (var component in componentTypes)
        {
            entity.AddComponent(component);
        }

        return entity;
    }

    public void Destroy()
    {
        if(World.Current == null)
        {
            return;
        }

        World.Current.DestroyEntity(this);
    }
}
