using System.Diagnostics.CodeAnalysis;
using System;

namespace Staple;

public partial struct Entity
{
    /// <summary>
    /// Adds a component to this entity
    /// </summary>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    public IComponent AddComponent(
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
    public T AddComponent
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
    public void RemoveComponent(Type t)
    {
        World.Current.RemoveComponent(this, t);
    }

    /// <summary>
    /// Removes a component from this entity
    /// </summary>
    /// <typeparam name="T">The type to remove</typeparam>
    public void RemoveComponent<T>() where T : IComponent
    {
        World.Current?.RemoveComponent<T>(this);
    }

    /// <summary>
    /// Attempts to get a component from this entity
    /// </summary>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    public IComponent GetComponent(Type t)
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
    public T GetComponent<T>() where T : IComponent
    {
        if (World.Current == null)
        {
            return default;
        }

        return World.Current.GetComponent<T>(this);
    }

    /// <summary>
    /// Attempts to get a component from a parent entity
    /// </summary>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    public IComponent GetComponentInParent(Type t)
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
    public T GetComponentInParent<T>() where T : IComponent
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
    public bool TryGetComponent(out IComponent component, Type t)
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
    public bool TryGetComponent<T>(out T component) where T : IComponent
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
    public void UpdateComponent(IComponent component)
    {
        if (World.Current == null)
        {
            return;
        }

        World.Current.UpdateComponent(this, component);
    }
}
