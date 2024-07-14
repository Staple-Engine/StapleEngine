using Staple.Internal;
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

                if(value.Assembly == assembly)
                {
                    componentsRepository.Remove(key);

                    foreach(var entity in entities)
                    {
                        if(entity.componentIndices.Contains(i))
                        {
                            entity.componentIndices.Remove(i);

                            for(var j = 0; j < entity.components.Count; j++)
                            {
                                if (entity.components[j].GetType() == value)
                                {
                                    entity.components.RemoveAt(j);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Finds a component's indices in the components repository
    /// </summary>
    /// <param name="t">The type to check</param>
    /// <returns>The indices</returns>
    internal IEnumerable<int> ComponentIndices(Type t)
    {
        lock (lockObject)
        {
            foreach (var pair in componentsRepository)
            {
                if (pair.Value == t ||
                    pair.Value.IsSubclassOf(t) ||
                    pair.Value.IsAssignableTo(t))
                {
                    yield return pair.Key;
                }
            }
        }
    }

    /// <summary>
    /// Finds a component's index in an entity's components
    /// </summary>
    /// <param name="entity">The entity to check</param>
    /// <param name="t">The type to check</param>
    /// <returns>The index or -1 on failure</returns>
    internal IEnumerable<int> ComponentIndices(EntityInfo entity, Type t)
    {
        lock (lockObject)
        {
            foreach(var componentIndex in entity.componentIndices)
            {
                if(componentsRepository.TryGetValue(componentIndex, out var info) &&
                    (info == t ||
                    info.IsSubclassOf(t) ||
                    info.IsAssignableTo(t)))
                {
                    yield return componentIndex;
                }
            }
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

            var cameras = ForEach<Camera, Transform>(false);

            foreach((Entity e, Camera c, Transform t) in cameras)
            {
                pieces.Add(new()
                {
                    camera = c,
                    entity = e,
                    transform = t,
                });
            }

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
        if(t.GetCustomAttribute(typeof(AbstractComponentAttribute)) != null)
        {
            return default;
        }

        if(TryGetEntity(entity, out var entityInfo) == false)
        {
            return default;
        }

        lock (lockObject)
        {
            var componentIndex = -1;

            foreach (var pair in componentsRepository)
            {
                if (pair.Value == t)
                {
                    componentIndex = pair.Key;
                }
            }

            var added = componentIndex < 0;

            if (componentIndex < 0)
            {
                componentIndex = componentsRepository.Keys.Count;

                componentsRepository.Add(componentIndex, t);

                if(t.IsSubclassOf(typeof(CallbackComponent)))
                {
                    callableComponentIndices.Add(componentIndex);
                }
            }

            var index = entityInfo.componentIndices.IndexOf(componentIndex);

            if (index < 0)
            {
                var component = ObjectCreation.CreateObject<IComponent>(t);

                if (component == default)
                {
                    return default;
                }

                entityInfo.componentIndices.Add(componentIndex);

                if(Scene.InstancingComponent == false)
                {
                    EmitAddComponentEvent(entity, ref component);
                }

                entityInfo.components.Add(component);

                index = entityInfo.componentIndices.Count - 1;
            }
            else //Already has one, return it
            {
                return entityInfo.components[index];
            }

            if(t.GetCustomAttribute<AutoAssignEntityAttribute>() != null)
            {
                try
                {
                    var outValue = entityInfo.components[index];

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

                    //Assign in case it's a value type
                    entityInfo.components[index] = outValue;
                }
                catch(Exception)
                {
                }
            }

            if(Platform.IsPlaying &&
                callableComponentIndices.Count != 0 &&
                t.IsSubclassOf(typeof(CallbackComponent)))
            {
                var instance = entityInfo.components[index] as CallbackComponent;

                try
                {
                    instance.Awake();
                }
                catch (Exception e)
                {
                    Log.Debug($"{entity.Name} ({instance.GetType().FullName}): Exception thrown while handling Awake: {e}");
                }
            }

            return entityInfo.components[index];
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
            foreach(var componentIndex in ComponentIndices(t))
            {
                if (entityInfo.TryGetComponentIndex(componentIndex, out var index))
                {
                    entityInfo.removedComponents.Add(componentIndex);

                    var component = entityInfo.components[index];

                    if (Platform.IsPlaying &&
                        callableComponentIndices.Count != 0 &&
                        component is CallbackComponent callable)
                    {
                        try
                        {
                            callable.OnDestroy();
                        }
                        catch (Exception e)
                        {
                            Log.Debug($"{entity.Name} ({callable.GetType().FullName}): Exception thrown while handling OnDestroy: {e}");
                        }
                    }

                    EmitRemoveComponentEvent(entity, ref component);

                    entityInfo.components[index] = component;
                }
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
            foreach (var componentIndex in ComponentIndices(t))
            {
                if (entityInfo.TryGetComponentIndex(componentIndex, out var index))
                {
                    return entityInfo.components[index];
                }
            }

            return default;
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
            foreach(var componentIndex in ComponentIndices(t))
            {
                if (entityInfo.TryGetComponentIndex(componentIndex, out var index))
                {
                    component = entityInfo.components[index];

                    return true;
                }
            }

            component = default;

            return false;
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
    public void SetComponent(Entity entity, IComponent component)
    {
        if (TryGetEntity(entity, out var entityInfo) == false)
        {
            return;
        }

        lock (lockObject)
        {
            foreach(var componentIndex in ComponentIndices(component.GetType()))
            {
                if(entityInfo.TryGetComponentIndex(componentIndex, out var index))
                {
                    entityInfo.components[index] = component;
                }
            }
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
                        callback?.Invoke(this, entity, ref component);
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
                        callback?.Invoke(this, entity, ref component);
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

    internal void IterateCallableComponents(CallableComponentCallback callback)
    {
        if(Platform.IsPlaying == false)
        {
            return;
        }

        lock(lockObject)
        {
            if(callableComponentIndices.Count == 0)
            {
                return;
            }

            //TODO: Figure out a way without allocations. We can have layers of iterations mixed in due to callbacks.
            var allEntities = entities.ToArray();

            foreach(var entity in allEntities)
            {
                if(entity.alive == false)
                {
                    continue;
                }

                foreach(var component in entity.componentIndices)
                {
                    if(callableComponentIndices.Count != 0 &&
                        callableComponentIndices.Contains(component) &&
                        entity.TryGetComponentIndex(component, out var index) &&
                        entity.components[index] is CallbackComponent callbackComponent)
                    {
                        try
                        {
                            callback?.Invoke(new Entity()
                            {
                                Identifier = new()
                                {
                                    ID = entity.ID,
                                    generation = entity.generation,
                                }
                            },
                            callbackComponent);
                        }
                        catch(Exception e)
                        {
                            Log.Error($"[World] Failed to handle callable component callback: {e}");
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attempts to find the entity for a component. Mostly works with classes, since it compares each.
    /// </summary>
    /// <param name="component">The component to check</param>
    /// <returns>The entity, if valid</returns>
    public Entity GetComponentEntity(IComponent component)
    {
        if(component == null)
        {
            return default;
        }

        lock (lockObject)
        {
            foreach (var componentIndex in ComponentIndices(component.GetType()))
            {
                foreach(var entity in entities)
                {
                    if (entity.TryGetComponentIndex(componentIndex, out var index) &&
                        entity.components[index] == component)
                    {
                        return new Entity()
                        {
                            Identifier = new()
                            {
                                ID = entity.ID,
                                generation = entity.generation,
                            }
                        };
                    }
                }
            }

            return default;
        }
    }

    /// <summary>
    /// Attempts to get the entity for a component. Mostly works with classes, since it compares each.
    /// </summary>
    /// <param name="component">The component to check</param>
    /// <param name="entity">The entity, if valid</param>
    /// <returns>Whether the entity was found</returns>
    public bool TryGetComponentEntity(IComponent component, out Entity entity)
    {
        entity = GetComponentEntity(component);

        return entity.IsValid;
    }
}
