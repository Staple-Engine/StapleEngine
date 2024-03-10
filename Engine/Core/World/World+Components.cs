using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Staple;

public partial class World
{
    /// <summary>
    /// Gets an entity's internal data if valid
    /// </summary>
    /// <param name="entity">The entity</param>
    /// <returns>The entity info, or null</returns>
    internal bool TryGetEntity(Entity entity, out EntityInfo info)
    {
        var localID = entity.Identifier.ID - 1;

        lock (lockObject)
        {
            if (localID < 0 ||
                localID >= entities.Count ||
                entities[localID].alive == false ||
                entities[localID].generation != entity.Identifier.generation)
            {
                info = default;

                return false;
            }

            info = entities[localID];

            return true;
        }
    }

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
    /// Finds a component's index in an entity's components
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <param name="t">The type to check</param>
    /// <returns>The index or -1 on failure</returns>
    internal int ComponentIndex(EntityInfo entity, Type t)
    {
        lock (lockObject)
        {
            foreach(var componentIndex in entity.components)
            {
                if(componentsRepository.TryGetValue(componentIndex, out var info) &&
                    (info.type == t ||
                    info.type.IsSubclassOf(t) ||
                    info.type.IsAssignableTo(t)))
                {
                    return componentIndex;
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

            ForEach((Entity entity, ref Camera camera, ref Transform transform) =>
            {
                pieces.Add(new CameraInfo()
                {
                    entity = entity,
                    camera = camera,
                    transform = transform
                });
            }, false);

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
        <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        T>
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
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type t)
    {
        if(TryGetEntity(entity, out var entityInfo) == false)
        {
            return default;
        }

        lock (lockObject)
        {
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

            if (entityInfo.components.Contains(infoIndex) == false)
            {
                entityInfo.components.Add(infoIndex);

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

                info.components[entityInfo.localID] = component;
            }

            if(t.GetCustomAttribute<AutoAssignEntityAttribute>() != null)
            {
                try
                {
                    var outValue = info.components[entityInfo.localID];

                    var field = t.GetField("entity");

                    if (field != null)
                    {
                        field.SetValue(outValue, entity);
                    }

                    var property = t.GetProperty("entity");

                    if (property != null)
                    {
                        property.SetValue(outValue, entity);
                    }

                    info.components[entityInfo.localID] = outValue;
                }
                catch(Exception)
                {
                }

                entityInfo.componentsModified = true;
            }

            return info.components[entityInfo.localID];
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
        if(TryGetEntity(entity, out var entityInfo) == false)
        {
            return;
        }

        lock (lockObject)
        {
            var componentIndex = ComponentIndex(t);

            if (componentIndex >= 0)
            {
                if (componentsRepository.TryGetValue(componentIndex, out var info))
                {
                    var component = info.components[entityInfo.localID];

                    EmitRemoveComponentEvent(entity, ref component);

                    info.components[entityInfo.localID] = component;
                }

                entityInfo.components.Remove(componentIndex);

                entityInfo.componentsModified = true;
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
        if (typeof(IComponent).IsAssignableFrom(t) == false ||
            TryGetEntity(entity, out var entityInfo) == false)
        {
            return default;
        }

        lock (lockObject)
        {
            var componentIndex = ComponentIndex(t);

            if (entityInfo.components.Contains(componentIndex) == false ||
                componentsRepository.TryGetValue(componentIndex, out var info) == false)
            {
                return default;
            }

            return info.components[entityInfo.localID];
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
        if (typeof(IComponent).IsAssignableFrom(t) == false ||
            TryGetEntity(entity, out var entityInfo) == false)
        {
            component = default;

            return false;
        }

        lock (lockObject)
        {
            var componentIndex = ComponentIndex(t);

            if (entityInfo.components.Contains(componentIndex) == false ||
                componentsRepository.TryGetValue(componentIndex, out var info) == false)
            {
                component = default;

                return false;
            }

            component = info.components[entityInfo.localID];

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
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            return;
        }

        lock (lockObject)
        {
            var componentIndex = ComponentIndex(component.GetType());

            if (componentIndex < 0)
            {
                return;
            }

            componentsRepository[componentIndex].components[entityInfo.localID] = component;
        }
    }

    /// <summary>
    /// Adds a callback for when a component is added to an entity
    /// </summary>
    /// <param name="componentType">The component type</param>
    /// <param name="callback">The callback to call</param>
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

    /// <summary>
    /// Adds a callback for when a component is removed from an entity
    /// </summary>
    /// <param name="componentType">The component type</param>
    /// <param name="callback">The callback to call</param>
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

    /// <summary>
    /// Emits a component added event
    /// </summary>
    /// <param name="entity">The entity to emit for</param>
    /// <param name="component">The component that was added</param>
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

    /// <summary>
    /// Emits a remove component event
    /// </summary>
    /// <param name="entity">The entity the component was removed from</param>
    /// <param name="component">The component being removed</param>
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
