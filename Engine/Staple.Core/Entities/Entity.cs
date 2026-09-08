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
    /// The entity's hierarchy visibility
    /// </summary>
    public readonly EntityHierarchyVisibility HierarchyVisibility
    {
        get => World.Current?.GetEntityHierarchyVisibility(this) ?? EntityHierarchyVisibility.None;

        set => World.Current?.SetEntityHierarchyVisibility(this, value);
    }

    /// <summary>
    /// Checks if this entity is valid
    /// </summary>
    public readonly bool IsValid => World.Current?.IsValidEntity(this) ?? false;

    /// <summary>
    /// Gets the <see cref="Transform"/> component ssociated with this entity
    /// </summary>
    public readonly Transform Transform => GetComponent<Transform>();

    public static bool operator==(Entity a, Entity b)
    {
        return a.Identifier == b.Identifier;
    }

    public static bool operator!=(Entity a, Entity b)
    {
        return a.Identifier != b.Identifier;
    }

    public override readonly bool Equals(object obj)
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

    public override readonly int GetHashCode()
    {
        return Identifier.GetHashCode();
    }

    public override readonly string ToString()
    {
        if(IsValid)
        {
            var nameString = Name ?? "Unnamed Entity";

            return $"{nameString} {Identifier.ID}:{Identifier.generation}";
        }
        else
        {
            return "Invalid Entity";
        }
    }

    /// <summary>
    /// Sets this entity's layer
    /// </summary>
    /// <param name="layer">The layer</param>
    /// <param name="includeChildren">Whether to apply to children</param>
    public void SetLayer(uint layer, bool includeChildren = false)
    {
        Layer = layer;

        if(includeChildren && TryGetComponent<Transform>(out var transform))
        {
            foreach (var child in transform.Children)
            {
                child.Entity.SetLayer(layer, true);
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

        if(!entity.IsValid)
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
    /// Creates an entity with specific component types
    /// </summary>
    /// <param name="A">The first component instance</param>
    /// <returns>The entity, or default</returns>
    /// <typeparam name="T">The first component type</typeparam>
    public static Entity Create<T>(out T A) where T: Component
    {
        if (World.Current == null)
        {
            A = default;

            return default;
        }

        var entity = World.Current.CreateEntity();

        if (!entity.IsValid)
        {
            A = default;

            return default;
        }

        A = entity.AddComponent<T>();

        return entity;
    }

    /// <summary>
    /// Creates an entity with specific component types
    /// </summary><
    /// <param name="A">The first component instance</param>
    /// <param name="B">The second component instance</param>
    /// <returns>The entity, or default</returns>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    public static Entity Create<T, T2>(out T A, out T2 B)
        where T : Component
        where T2 : Component
    {
        if (World.Current == null)
        {
            A = default;
            B = default;

            return default;
        }

        var entity = World.Current.CreateEntity();

        if (!entity.IsValid)
        {
            A = default;
            B = default;

            return default;
        }

        A = entity.AddComponent<T>();
        B = entity.AddComponent<T2>();

        return entity;
    }

    /// <summary>
    /// Creates an entity with specific component types
    /// </summary><
    /// <param name="A">The first component instance</param>
    /// <param name="B">The second component instance</param>
    /// <param name="C">The third component instance</param>
    /// <returns>The entity, or default</returns>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    /// <typeparam name="T3">The third component type</typeparam>
    public static Entity Create<T, T2, T3>(out T A, out T2 B, out T3 C)
        where T : Component
        where T2 : Component
        where T3 : Component
    {
        if (World.Current == null)
        {
            A = default;
            B = default;
            C = default;

            return default;
        }

        var entity = World.Current.CreateEntity();

        if (!entity.IsValid)
        {
            A = default;
            B = default;
            C = default;

            return default;
        }

        A = entity.AddComponent<T>();
        B = entity.AddComponent<T2>();
        C = entity.AddComponent<T3>();

        return entity;
    }

    /// <summary>
    /// Creates an entity with specific component types
    /// </summary><
    /// <param name="A">The first component instance</param>
    /// <param name="B">The second component instance</param>
    /// <param name="C">The third component instance</param>
    /// <param name="D">The fourth component instance</param>
    /// <returns>The entity, or default</returns>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    /// <typeparam name="T3">The third component type</typeparam>
    /// <typeparam name="T4">The fourth component type</typeparam>
    public static Entity Create<T, T2, T3, T4>(out T A, out T2 B, out T3 C, out T4 D)
        where T : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
    {
        if (World.Current == null)
        {
            A = default;
            B = default;
            C = default;
            D = default;

            return default;
        }

        var entity = World.Current.CreateEntity();

        if (!entity.IsValid)
        {
            A = default;
            B = default;
            C = default;
            D = default;

            return default;
        }

        A = entity.AddComponent<T>();
        B = entity.AddComponent<T2>();
        C = entity.AddComponent<T3>();
        D = entity.AddComponent<T4>();

        return entity;
    }

    /// <summary>
    /// Creates an entity with specific component types
    /// </summary><
    /// <param name="A">The first component instance</param>
    /// <param name="B">The second component instance</param>
    /// <param name="C">The third component instance</param>
    /// <param name="D">The fourth component instance</param>
    /// <param name="E">The fifth component instance</param>
    /// <returns>The entity, or default</returns>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    /// <typeparam name="T3">The third component type</typeparam>
    /// <typeparam name="T4">The fourth component type</typeparam>
    /// <typeparam name="T5">The fifth component type</typeparam>
    public static Entity Create<T, T2, T3, T4, T5>(out T A, out T2 B, out T3 C, out T4 D, out T5 E)
        where T : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        where T5 : Component
    {
        if (World.Current == null)
        {
            A = default;
            B = default;
            C = default;
            D = default;
            E = default;

            return default;
        }

        var entity = World.Current.CreateEntity();

        if (!entity.IsValid)
        {
            A = default;
            B = default;
            C = default;
            D = default;
            E = default;

            return default;
        }

        A = entity.AddComponent<T>();
        B = entity.AddComponent<T2>();
        C = entity.AddComponent<T3>();
        D = entity.AddComponent<T4>();
        E = entity.AddComponent<T5>();

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

        if (!entity.IsValid)
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
    /// Creates an entity with a specific name and specific component types
    /// </summary><
    /// <param name="A">The first component instance</param>
    /// <returns>The entity, or default</returns>
    /// <typeparam name="T">The first component type</typeparam>
    public static Entity Create<T>(string name, out T A)
        where T : Component
    {
        var entity = Create(out A);

        if (entity.IsValid)
        {
            entity.Name = name;
        }

        return entity;
    }

    /// <summary>
    /// Creates an entity with a specific name and specific component types
    /// </summary><
    /// <param name="A">The first component instance</param>
    /// <param name="B">The second component instance</param>
    /// <returns>The entity, or default</returns>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    public static Entity Create<T, T2>(string name, out T A, out T2 B)
        where T : Component
        where T2 : Component
    {
        var entity = Create(out A, out B);

        if (entity.IsValid)
        {
            entity.Name = name;
        }

        return entity;
    }

    /// <summary>
    /// Creates an entity with a specific name and specific component types
    /// </summary><
    /// <param name="A">The first component instance</param>
    /// <param name="B">The second component instance</param>
    /// <param name="C">The third component instance</param>
    /// <returns>The entity, or default</returns>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    /// <typeparam name="T3">The third component type</typeparam>
    public static Entity Create<T, T2, T3>(string name, out T A, out T2 B, out T3 C)
        where T : Component
        where T2 : Component
        where T3 : Component
    {
        var entity = Create(out A, out B, out C);

        if (entity.IsValid)
        {
            entity.Name = name;
        }

        return entity;
    }

    /// <summary>
    /// Creates an entity with a specific name and specific component types
    /// </summary><
    /// <param name="A">The first component instance</param>
    /// <param name="B">The second component instance</param>
    /// <param name="C">The third component instance</param>
    /// <param name="D">The fourth component instance</param>
    /// <returns>The entity, or default</returns>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    /// <typeparam name="T3">The third component type</typeparam>
    /// <typeparam name="T4">The fourth component type</typeparam>
    public static Entity Create<T, T2, T3, T4>(string name, out T A, out T2 B, out T3 C, out T4 D)
        where T : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
    {
        var entity = Create(out A, out B, out C, out D);

        if (entity.IsValid)
        {
            entity.Name = name;
        }

        return entity;
    }

    /// <summary>
    /// Creates an entity with a specific name and specific component types
    /// </summary><
    /// <param name="A">The first component instance</param>
    /// <param name="B">The second component instance</param>
    /// <param name="C">The third component instance</param>
    /// <param name="D">The fourth component instance</param>
    /// <param name="E">The fifth component instance</param>
    /// <returns>The entity, or default</returns>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    /// <typeparam name="T3">The third component type</typeparam>
    /// <typeparam name="T4">The fourth component type</typeparam>
    /// <typeparam name="T5">The fifth component type</typeparam>
    public static Entity Create<T, T2, T3, T4, T5>(string name, out T A, out T2 B, out T3 C, out T4 D, out T5 E)
        where T : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        where T5 : Component
    {
        var entity = Create(out A, out B, out C, out D, out E);

        if(entity.IsValid)
        {
            entity.Name = name;
        }

        return entity;
    }

    /// <summary>
    /// Creates an entity for a geometry primitive
    /// </summary>
    /// <param name="type">The type of primitive</param>
    /// <param name="addColliders">Whether to add colliders to the entity</param>
    /// <returns>The entity</returns>
    public static Entity CreatePrimitive(EntityPrimitiveType type, bool addColliders = true)
    {
        return CreatePrimitive(type, addColliders, out var _, out var _, out var _);
    }

    /// <summary>
    /// Creates an entity for a geometry primitive
    /// </summary>
    /// <param name="type">The type of primitive</param>
    /// <param name="addColliders">Whether to add colliders to the entity</param>
    /// <param name="transform">The transform of the primitive, if any</param>
    /// <param name="meshRenderer">The mesh renderer of the primitive, if any</param>
    /// <param name="collider">The mesh collider, if any</param>
    /// <returns>The entity</returns>
    public static Entity CreatePrimitive(EntityPrimitiveType type, bool addColliders, out Transform transform, out MeshRenderer meshRenderer,
        out Collider3D collider)
    {
        collider = default;

        var e = Create(type.ToString(), out transform, out meshRenderer);

        switch (type)
        {
            case EntityPrimitiveType.Cube:

                meshRenderer.mesh = Mesh.Cube;

                if (addColliders)
                {
                    collider = e.AddComponent<BoxCollider3D>();
                }

                break;

            case EntityPrimitiveType.Quad:

                meshRenderer.mesh = Mesh.Quad;

                if (addColliders)
                {
                    var c = e.AddComponent<MeshCollider3D>();
                    
                    c.mesh = meshRenderer.mesh;

                    collider = c;
                }

                break;

            case EntityPrimitiveType.Sphere:

                meshRenderer.mesh = Mesh.Sphere;

                if (addColliders)
                {
                    collider = e.AddComponent<SphereCollider3D>();
                }

                break;
        }

        meshRenderer.materials = [ResourceManager.instance.LoadMaterial($"Hidden/Materials/Checkerboard.{AssetSerialization.MaterialExtension}")];

        return e;
    }

    /// <summary>
    /// Creates an entity for a geometry primitive
    /// </summary>
    /// <param name="name">The name of the primitive</param>
    /// <param name="type">The type of primitive</param>
    /// <param name="addColliders">Whether to add colliders to the entity</param>
    /// <returns>The entity</returns>
    public static Entity CreatePrimitive(string name, EntityPrimitiveType type, bool addColliders = true)
    {
        var e = CreatePrimitive(type, addColliders);

        if(e.IsValid)
        {
            e.Name = name;
        }

        return e;
    }

    /// <summary>
    /// Creates an entity for a geometry primitive
    /// </summary>
    /// <param name="name">The name of the primitive</param>
    /// <param name="type">The type of primitive</param>
    /// <param name="addColliders">Whether to add colliders to the entity</param>
    /// <param name="transform">The transform of the primitive, if any</param>
    /// <param name="meshRenderer">The mesh renderer of the primitive, if any</param>
    /// <param name="collider">The mesh collider, if any</param>
    /// <returns>The entity</returns>
    public static Entity CreatePrimitive(string name, EntityPrimitiveType type, bool addColliders, out Transform transform,
        out MeshRenderer meshRenderer, out Collider3D collider)
    {
        var e = CreatePrimitive(type, addColliders, out transform, out meshRenderer, out collider);

        if (e.IsValid)
        {
            e.Name = name;
        }

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
