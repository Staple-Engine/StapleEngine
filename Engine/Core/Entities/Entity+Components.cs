using System.Diagnostics.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace Staple;

public partial struct Entity
{
    /// <summary>
    /// Adds a component to this entity
    /// </summary>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    public readonly IComponent AddComponent(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type t)
    {
        if (World.Current == null)
        {
            return default;
        }

        return World.Current.AddComponent(this, t);
    }

    /// <summary>
    /// Adds a component to this entity
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <returns>The component instance, or default</returns>
    public readonly T AddComponent
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        () where T : IComponent
    {
        if (World.Current == null)
        {
            return default;
        }

        return World.Current.AddComponent<T>(this);
    }

    /// <summary>
    /// Removes a component from this entity
    /// </summary>
    /// <param name="t">The type to remove</param>
    public readonly void RemoveComponent(Type t)
    {
        World.Current.RemoveComponent(this, t);
    }

    /// <summary>
    /// Removes a component from this entity
    /// </summary>
    /// <typeparam name="T">The type to remove</typeparam>
    public readonly void RemoveComponent<T>() where T : IComponent
    {
        World.Current?.RemoveComponent<T>(this);
    }

    /// <summary>
    /// Attempts to get a component from this entity
    /// </summary>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    public readonly IComponent GetComponent(Type t)
    {
        if (World.Current == null)
        {
            return default;
        }

        return World.Current.GetComponent(this, t);
    }

    /// <summary>
    /// Attempts to get a component from this entity
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <returns>The component instance, or default</returns>
    public readonly T GetComponent<T>() where T : IComponent
    {
        if (World.Current == null)
        {
            return default;
        }

        return World.Current.GetComponent<T>(this);
    }

    /// <summary>
    /// Attempts to get a component from this entity
    /// </summary>
    /// <param name="t">The component type</param>
    /// <param name="includeSelf">Whether to include components from this entity</param>
    /// <returns>The component instance, or default</returns>
    public readonly IComponent[] GetComponentsInChildren(Type t, bool includeSelf = true)
    {
        if (World.Current == null)
        {
            return default;
        }

        var result = new List<IComponent>();

        if(includeSelf && World.Current.TryGetComponent(this, out var c, t))
        {
            result.Add(c);
        }

        void Recursive(Entity e)
        {
            if(e.TryGetComponent(out c, t))
            {
                result.Add(c);
            }

            var transform = e.GetComponent<Transform>();

            if(transform != null)
            {
                foreach (var child in transform.Children)
                {
                    Recursive(child.entity);
                }
            }
        }

        var transform = GetComponent<Transform>();

        if(transform == null)
        {
            return result.ToArray();
        }

        foreach(var child in transform.Children)
        {
            Recursive(child.entity);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Attempts to get a component from this entity
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <param name="includeSelf">Whether to include components from this entity</param>
    /// <returns>The component instance, or default</returns>
    public readonly T[] GetComponentsInChildren<T>(bool includeSelf = false) where T : IComponent
    {
        if (World.Current == null)
        {
            return default;
        }

        var result = new List<T>();

        if (includeSelf && World.Current.TryGetComponent<T>(this, out var c))
        {
            result.Add(c);
        }

        void Recursive(Entity e)
        {
            if (e.TryGetComponent(out c))
            {
                result.Add(c);
            }

            var transform = e.GetComponent<Transform>();

            if (transform != null)
            {
                foreach (var child in transform.Children)
                {
                    Recursive(child.entity);
                }
            }
        }

        var transform = GetComponent<Transform>();

        if (transform == null)
        {
            return result.ToArray();
        }

        foreach (var child in transform.Children)
        {
            Recursive(child.entity);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Attempts to get a component from this entity
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <param name="includeSelf">Whether to include components from this entity</param>
    /// <returns>A list of entities and component instance tuples, or empty</returns>
    public readonly (Entity, T)[] GetComponentEntitiesInChildren<T>(bool includeSelf = false) where T : IComponent
    {
        if (World.Current == null)
        {
            return [];
        }

        var result = new List<(Entity, T)>();

        if (includeSelf && World.Current.TryGetComponent<T>(this, out var c))
        {
            result.Add((this, c));
        }

        void Recursive(Entity e)
        {
            if (e.TryGetComponent(out c))
            {
                result.Add((e, c));
            }

            var transform = e.GetComponent<Transform>();

            if (transform != null)
            {
                foreach (var child in transform.Children)
                {
                    Recursive(child.entity);
                }
            }
        }

        var transform = GetComponent<Transform>();

        if (transform == null)
        {
            return result.ToArray();
        }

        foreach (var child in transform.Children)
        {
            Recursive(child.entity);
        }

        return result.ToArray();
    }

    /// <summary>
    /// Attempts to get a component from a parent entity
    /// </summary>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    public readonly IComponent GetComponentInParent(Type t)
    {
        if (World.Current == null)
        {
            return default;
        }

        var transform = GetComponent<Transform>();

        if(transform == null || transform.parent == null)
        {
            return default;
        }

        if(transform.parent.entity.TryGetComponent(out var value, t))
        {
            return value;
        }

        return transform.parent.entity.GetComponentInParent(t);
    }

    /// <summary>
    /// Attempts to get a component from this entity
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <returns>The component instance, or default</returns>
    public readonly T GetComponentInParent<T>() where T : IComponent
    {
        if (World.Current == null)
        {
            return default;
        }

        var transform = GetComponent<Transform>();

        if (transform == null || transform.parent == null)
        {
            return default;
        }

        if (transform.parent.entity.TryGetComponent(out T value))
        {
            return value;
        }

        return transform.parent.entity.GetComponentInParent<T>();
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <param name="component">The component instance</param>
    /// <param name="t">The component type</param>
    /// <returns>Whether the component was found</returns>
    public readonly bool TryGetComponent(out IComponent component, Type t)
    {
        if (World.Current == null)
        {
            component = default;

            return false;
        }

        return World.Current.TryGetComponent(this, out component, t);
    }

    /// <summary>
    /// Attempts to get a component from this entity
    /// </summary>
    /// <param name="component">The component instance</param>
    /// <typeparam name="T">The component type</typeparam>
    /// <returns>Whether the component was found</returns>
    public readonly bool TryGetComponent<T>(out T component) where T : IComponent
    {
        if (World.Current == null)
        {
            component = default;

            return false;
        }

        return World.Current.TryGetComponent<T>(this, out component);
    }

    /// <summary>
    /// Updates a component in this entity.
    /// This is required if the component type is a struct.
    /// </summary>
    /// <param name="component">The component instance to replace</param>
    public readonly void SetComponent(IComponent component)
    {
        if (World.Current == null)
        {
            return;
        }

        World.Current.SetComponent(this, component);
    }

    /// <summary>
    /// Attempts to get a component's entity
    /// </summary>
    /// <param name="component">The component</param>
    /// <param name="entity">The entity</param>
    /// <returns>Whether the entity was found</returns>
    public static bool TryGetComponentEntity(IComponent component, out Entity entity)
    {
        if(World.Current == null)
        {
            entity = default;

            return false;
        }

        return World.Current.TryGetComponentEntity(component, out entity);
    }
}
