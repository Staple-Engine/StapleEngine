using Staple.Internal;
using System;

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
    public readonly bool Enabled
    {
        get => World.Current?.IsEntityEnabled(this) ?? false;

        set => World.Current?.SetEntityEnabled(this, value);
    }

    /// <summary>
    /// Whether this entity is enabled in its hierarchy
    /// </summary>
    public readonly bool EnabledInHierarchy => World.Current?.IsEntityEnabled(this, true) ?? false;

    /// <summary>
    /// The entity's name
    /// </summary>
    public readonly string Name
    {
        get => World.Current?.GetEntityName(this);

        set => World.Current?.SetEntityName(this, value);
    }

    /// <summary>
    /// The entity's layer
    /// </summary>
    public readonly uint Layer
    {
        get => World.Current?.GetEntityLayer(this) ?? 0;

        set => World.Current?.SetEntityLayer(this, value);
    }

    /// <summary>
    /// Checks if this entity is valid
    /// </summary>
    public readonly bool IsValid => World.Current?.IsValidEntity(this) ?? false;

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

    /// <summary>
    /// Sets this entity's layer
    /// </summary>
    /// <param name="layer">The layer</param>
    /// <param name="recursive">Whether to apply to children</param>
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

    /// <summary>
    /// Creates an entity with specific component types
    /// </summary>
    /// <param name="componentTypes">The components to add</param>
    /// <returns>The entity, or default</returns>
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

        for(var i = 0; i < componentTypes.Length; i++)
        {
            entity.AddComponent(componentTypes[i]);
        }

        return entity;
    }

    /// <summary>
    /// Creates an entity with a name and specific component types
    /// </summary>
    /// <param name="name">The entity name</param>
    /// <param name="componentTypes">The components to add</param>
    /// <returns>The entity, or default</returns>
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

        for(var i = 0; i < componentTypes.Length; i++)
        {
            entity.AddComponent(componentTypes[i]);
        }

        return entity;
    }

    /// <summary>
    /// Creates an entity for a geometry primitive
    /// </summary>
    /// <param name="type">The type of primitive</param>
    /// <returns>The entity</returns>
    public static Entity CreatePrimitive(EntityPrimitiveType type)
    {
        var e = Create(type.ToString(), typeof(Transform), typeof(MeshRenderer));

        var r = e.GetComponent<MeshRenderer>();

        switch(type)
        {
            case EntityPrimitiveType.Cube:

                r.mesh = Mesh.Cube;

                e.AddComponent<BoxCollider3D>();

                break;

            case EntityPrimitiveType.Quad:

                r.mesh = Mesh.Quad;

                e.AddComponent<MeshCollider3D>().mesh = r.mesh;

                break;
        }

        r.materials = [ResourceManager.instance.LoadMaterial("Hidden/Materials/Checkerboard.mat")];

        return e;
    }

    /// <summary>
    /// Destroys this entity. The destruction will happen in the next frame.
    /// </summary>
    public readonly void Destroy()
    {
        if(World.Current == null)
        {
            return;
        }

        World.Current.DestroyEntity(this);
    }
}
