using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Staple;

public partial class World
{
    /// <summary>
    /// Unloads all components from an assembly (Used for editor purposes)
    /// </summary>
    /// <param name="assembly">The assembly to unload from</param>
    internal void UnloadComponentsFromAssembly(Assembly assembly)
    {
        lock(lockObject)
        {
            var keys = componentsRepository.Keys.ToList();

            for(var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];
                var value = componentsRepository[key];

                if(value.type.Assembly == assembly)
                {
                    componentsRepository.Remove(key);

                    foreach(var entity in entities)
                    {
                        if(entity.components.Contains(i))
                        {
                            entity.components.Remove(i);
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds a component's index in the components repository
    /// </summary>
    /// <param name="t">The type to check</param>
    /// <returns>The index or -1 on failure</returns>
    internal int ComponentIndex(Type t)
    {
        lock (lockObject)
        {
            foreach (var pair in componentsRepository)
            {
                if (pair.Value.type == t)
                {
                    return pair.Key;
                }
            }

            return -1;
        }
    }

    /// <summary>
    /// Gets all available cameras sorted by depth
    /// </summary>
    public CameraInfo[] SortedCameras
    {
        get
        {
            var pieces = new List<CameraInfo>();

            ForEach((Entity entity, bool enabled, ref Camera camera, ref Transform transform) =>
            {
                if(enabled == false)
                {
                    return;
                }

                pieces.Add(new CameraInfo()
                {
                    entity = entity,
                    camera = camera,
                    transform = transform
                });
            });

            pieces.Sort((x, y) => x.camera.depth.CompareTo(y.camera.depth));

            return pieces.ToArray();
        }
    }

    /// <summary>
    /// Adds a component to an entity
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <param name="entity">The entity to add the component to</param>
    /// <returns>The component instance, or default</returns>
    public T AddComponent
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] T>
        (Entity entity) where T : IComponent
    {
        return (T)AddComponent(entity, typeof(T));
    }

    /// <summary>
    /// Adds a component to an entity
    /// </summary>
    /// <param name="entity">The entity to add the component to</param>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    public IComponent AddComponent(Entity entity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        Type t)
    {
        lock (lockObject)
        {
            if (entity.ID < 0 ||
                entity.ID >= entities.Count ||
                entities[entity.ID].alive == false ||
                entities[entity.ID].generation != entity.generation)
            {
                return default;
            }

            ComponentInfo info = null;
            var infoIndex = 0;

            foreach (var pair in componentsRepository)
            {
                if (pair.Value.type == t)
                {
                    infoIndex = pair.Key;
                    info = pair.Value;
                }
            }

            var added = info == null;

            if (info == null)
            {
                infoIndex = componentsRepository.Keys.Count;

                info = new ComponentInfo()
                {
                    type = t,
                };

                for (var i = 0; i < entities.Count; i++)
                {
                    if (info.AddComponent() == false)
                    {
                        return default;
                    }
                }

                componentsRepository.Add(infoIndex, info);
            }

            var e = entities[entity.ID];

            if (e.components.Contains(infoIndex) == false)
            {
                e.components.Add(infoIndex);

                //Reset the component data if it already was there
                if (info.Create(out var component) == false)
                {
                    return default;
                }

                if(added == false)
                {
                    EmitRemoveComponentEvent(entity, ref component);
                }

                if(Scene.InstancingComponent == false)
                {
                    EmitAddComponentEvent(entity, ref component);
                }

                info.components[entity.ID] = component;
            }

            collectionModified = true;

            return info.components[entity.ID];
        }
    }

    /// <summary>
    /// Removes a component from an entity
    /// </summary>
    /// <typeparam name="T">The type to remove</typeparam>
    /// <param name="entity">The entity to remove the component from</param>
    public void RemoveComponent<T>(Entity entity) where T : IComponent
    {
        RemoveComponent(entity, typeof(T));
    }

    /// <summary>
    /// Removes a component from an entity
    /// </summary>
    /// <param name="entity">The entity to remove the component from</param>
    /// <param name="t">The type to remove</param>
    public void RemoveComponent(Entity entity, Type t)
    {
        lock (lockObject)
        {
            if (entity.ID < 0 ||
                entity.ID >= entities.Count ||
                entities[entity.ID].alive == false ||
                entities[entity.ID].generation != entity.generation)
            {
                return;
            }

            var componentIndex = ComponentIndex(t);

            if (componentIndex >= 0)
            {
                if (componentsRepository.TryGetValue(componentIndex, out var info))
                {
                    var component = info.components[entity.ID];

                    EmitRemoveComponentEvent(entity, ref component);

                    info.components[entity.ID] = component;
                }

                entities[entity.ID].components.Remove(componentIndex);

                collectionModified = true;
            }
        }
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <param name="entity">The entity to get from</param>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    public IComponent GetComponent(Entity entity, Type t)
    {
        if (typeof(IComponent).IsAssignableFrom(t) == false)
        {
            return default;
        }

        lock (lockObject)
        {
            if (entity.ID < 0 ||
                entity.ID >= entities.Count ||
                entities[entity.ID].alive == false ||
                entities[entity.ID].generation != entity.generation)
            {
                return default;
            }

            var componentIndex = ComponentIndex(t);

            if (entities[entity.ID].components.Contains(componentIndex) == false ||
                componentsRepository.TryGetValue(componentIndex, out var info) == false)
            {
                return default;
            }

            return info.components[entity.ID];
        }
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <param name="entity">The entity to get from</param>
    /// <returns>The component instance, or default</returns>
    public T GetComponent<T>(Entity entity) where T : IComponent
    {
        return (T)GetComponent(entity, typeof(T));
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <param name="entity">The entity to get from</param>
    /// <param name="component">The component instance</param>
    /// <param name="t">The component type</param>
    /// <returns>Whether the component was found</returns>
    public bool TryGetComponent(Entity entity, out IComponent component, Type t)
    {
        if (typeof(IComponent).IsAssignableFrom(t) == false)
        {
            component = default;

            return false;
        }

        lock (lockObject)
        {
            if (entity.ID < 0 ||
                entity.ID >= entities.Count ||
                entities[entity.ID].alive == false ||
                entities[entity.ID].generation != entity.generation)
            {
                component = default;

                return false;
            }

            var componentIndex = ComponentIndex(t);

            if (entities[entity.ID].components.Contains(componentIndex) == false ||
                componentsRepository.TryGetValue(componentIndex, out var info) == false)
            {
                component = default;

                return false;
            }

            component = info.components[entity.ID];

            return true;
        }
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <param name="entity">The entity to get from</param>
    /// <param name="component">The component instance</param>
    /// <typeparam name="T">The component type</typeparam>
    /// <returns>Whether the component was found</returns>
    public bool TryGetComponent<T>(Entity entity, out T component) where T: IComponent
    {
        if(TryGetComponent(entity, out IComponent c, typeof(T)))
        {
            component = (T)c;

            return true;
        }

        component = default;

        return false;
    }

    /// <summary>
    /// Updates an entity's component.
    /// This is required if the component type is a struct.
    /// </summary>
    /// <param name="entity">The entity to update</param>
    /// <param name="component">The component instance to replace</param>
    public void UpdateComponent(Entity entity, IComponent component)
    {
        lock(lockObject)
        {
            var componentIndex = ComponentIndex(component.GetType());

            if (componentIndex < 0 ||
                entity.ID < 0 ||
                entity.ID >= entities.Count ||
                entities[entity.ID].alive == false ||
                entities[entity.ID].generation != entity.generation)
            {
                return;
            }

            componentsRepository[componentIndex].components[entity.ID] = component;
        }
    }

    public static void AddComponentAddedCallback([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type componentType,
        OnComponentChangedCallback callback)
    {
        if(componentType.GetInterface(typeof(IComponent).FullName) == null)
        {
            return;
        }

        lock(globalLockObject)
        {
            if(componentAddedCallbacks.TryGetValue(componentType, out var c) == false)
            {
                c = new();

                componentAddedCallbacks.Add(componentType, c);
            }

            if(c.Contains(callback))
            {
                return;
            }

            c.Add(callback);
        }
    }

    public static void AddComponentRemovedCallback([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type componentType,
        OnComponentChangedCallback callback)
    {
        if (componentType.GetInterface(typeof(IComponent).FullName) == null)
        {
            return;
        }

        lock (globalLockObject)
        {
            if (componentRemovedCallbacks.TryGetValue(componentType, out var c) == false)
            {
                c = new();

                componentRemovedCallbacks.Add(componentType, c);
            }

            if (c.Contains(callback))
            {
                return;
            }

            c.Add(callback);
        }
    }

    internal void EmitAddComponentEvent(Entity entity, ref IComponent component)
    {
        if (component == null)
        {
            return;
        }

        lock (globalLockObject)
        {
            var transform = GetComponent<Transform>(entity);

            if (componentAddedCallbacks.TryGetValue(component.GetType(), out var callbacks))
            {
                var removedCallbacks = new Stack<int>();

                for (var i = 0; i < callbacks.Count; i++)
                {
                    var callback = callbacks[i];

                    if (callback == null)
                    {
                        removedCallbacks.Push(i);

                        continue;
                    }

                    try
                    {
                        callback?.Invoke(this, entity, transform, ref component);
                    }
                    catch (Exception ex)
                    {
                        Log.Debug($"[World] AddComponent: Failed to handle a component added callback: {ex}");
                    }
                }

                while (removedCallbacks.Count > 0)
                {
                    var item = removedCallbacks.Pop();

                    callbacks.RemoveAt(item);
                }
            }
        }
    }

    internal void EmitRemoveComponentEvent(Entity entity, ref IComponent component)
    {
        if(component == null)
        {
            return;
        }

        lock (globalLockObject)
        {
            var transform = GetComponent<Transform>(entity);

            if (componentRemovedCallbacks.TryGetValue(component.GetType(), out var callbacks))
            {
                var removedCallbacks = new Stack<int>();

                for (var i = 0; i < callbacks.Count; i++)
                {
                    var callback = callbacks[i];

                    if (callback == null)
                    {
                        removedCallbacks.Push(i);

                        continue;
                    }

                    try
                    {
                        callback?.Invoke(this, entity, transform, ref component);
                    }
                    catch (Exception e)
                    {
                        Log.Debug($"[World] RemoveComponent: Failed to handle a component removed callback: {e}");
                    }
                }

                while (removedCallbacks.Count > 0)
                {
                    var item = removedCallbacks.Pop();

                    callbacks.RemoveAt(item);
                }
            }
        }
    }
}
