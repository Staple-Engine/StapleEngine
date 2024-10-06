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
            var keys = componentCompatibilityCache.Keys.ToList();

            for(var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];

                var type = TypeCache.GetType(key);

                if(type != null && type.Assembly == assembly)
                {
                    componentCompatibilityCache.Remove(key);

                    foreach(var pair in componentCompatibilityCache)
                    {
                        pair.Value.Remove(key);
                    }

                    foreach(var entity in entities)
                    {
                        entity.components.Remove(key);
                    }
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

            var cameras = Query<Camera, Transform>(false);

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
        if(t.GetCustomAttribute(typeof(AbstractComponentAttribute)) != null ||
            TryGetEntity(entity, out var entityInfo) == false)
        {
            return default;
        }

        lock (lockObject)
        {
            EnsureComponentInfo(t);

            if (entityInfo.components.TryGetValue(t.FullName, out var component))
            {
                //Already has one, return it

                return component;
            }
            else
            {
                component = ObjectCreation.CreateObject<IComponent>(t);

                if (component == default)
                {
                    return default;
                }

                entityInfo.components.Add(t.FullName, component);

                if (Scene.InstancingComponent == false)
                {
                    EmitAddComponentEvent(entity, ref component);
                }
            }

            if(t.GetCustomAttribute<AutoAssignEntityAttribute>() != null)
            {
                try
                {
                    var field = t.GetField("entity");

                    field?.SetValue(component, entity);

                    var property = t.GetProperty("entity");

                    if(property != null)
                    {
                        if(property.CanWrite)
                        {
                            property.SetValue(component, entity);
                        }
                        else
                        {
                            Log.Debug($"[{t.FullName}]: Can't auto assign entity: Property isn't writable");
                        }
                    }

                    if(t.IsValueType)
                    {
                        entityInfo.components.AddOrSetKey(t.FullName, component);
                    }
                }
                catch(Exception e)
                {
                    Log.Debug($"[{t.FullName}]: Failed to auto assign entity: {e}");
                }
            }

            if(Platform.IsPlaying &&
                callableComponentTypes.Count != 0 &&
                t.IsSubclassOf(typeof(CallbackComponent)))
            {
                var instance = component as CallbackComponent;

                try
                {
                    instance.Awake();
                }
                catch (Exception e)
                {
                    Log.Debug($"{entity.Name} ({instance.GetType().FullName}): Exception thrown while handling Awake: {e}");
                }
            }

            return component;
        }
    }

    private void EnsureComponentInfo(Type t)
    {
        var added = false;

        if (componentCompatibilityCache.TryGetValue(t.FullName, out var compatibleTypes) == false)
        {
            added = true;
            compatibleTypes = [];
        }

        if (added)
        {
            compatibleTypes.Add(t.FullName);

            void RecursiveAdd(Type target)
            {
                foreach (var targetInterface in target.GetInterfaces())
                {
                    if (targetInterface != typeof(IComponent) && targetInterface.IsAssignableTo(typeof(IComponent)))
                    {
                        compatibleTypes.Add(targetInterface.FullName);

                        EnsureComponentInfo(targetInterface);

                        if (componentCompatibilityCache.TryGetValue(targetInterface.FullName, out var interfaceCompatibility))
                        {
                            interfaceCompatibility.Add(t.FullName);
                        }
                    }
                }

                if (target.BaseType == null ||
                    target.BaseType == typeof(IComponent) ||
                    target.BaseType.IsAssignableTo(typeof(IComponent)) == false)
                {
                    return;
                }

                compatibleTypes.Add(target.BaseType.FullName);

                EnsureComponentInfo(target.BaseType);

                if(componentCompatibilityCache.TryGetValue(target.BaseType.FullName, out var baseCompatibility))
                {
                    baseCompatibility.Add(t.FullName);
                }

                RecursiveAdd(target.BaseType);
            }

            RecursiveAdd(t);

            componentCompatibilityCache.Add(t.FullName, compatibleTypes);

            if (t.IsSubclassOf(typeof(CallbackComponent)))
            {
                callableComponentTypes.Add(t.FullName);
            }
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
            var tHash = t.FullName.GetHashCode();

            if (componentCompatibilityCache.TryGetValue(t.FullName, out var compatibility) == false)
            {
                return;
            }

            foreach(var typeName in compatibility)
            {
                if (entityInfo.components.TryGetValue(typeName, out var component))
                {
                    entityInfo.removedComponents.Add(typeName);

                    if (Platform.IsPlaying &&
                        callableComponentTypes.Count != 0 &&
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

                    entityInfo.components[typeName] = component;
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
            if (componentCompatibilityCache.TryGetValue(t.FullName, out var compatibility) == false)
            {
                return default;
            }

            foreach (var typeName in compatibility)
            {
                if (entityInfo.components.TryGetValue(typeName, out var component))
                {
                    return component;
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
            if (componentCompatibilityCache.TryGetValue(t.FullName, out var compatibility) == false)
            {
                component = default;

                return false;
            }

            foreach (var typeName in compatibility)
            {
                if (entityInfo.components.TryGetValue(typeName, out component))
                {
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
            if (componentCompatibilityCache.TryGetValue(component.GetType().FullName, out var compatibility) == false)
            {
                return;
            }

            foreach (var typeName in compatibility)
            {
                if(entityInfo.components.ContainsKey(typeName))
                {
                    entityInfo.components[typeName] = component;
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
                c = [];

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
                c = [];

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
            if(callableComponentTypes.Count == 0)
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

                foreach(var pair in entity.components)
                {
                    if(pair.Value is CallbackComponent callbackComponent)
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
            if(componentCompatibilityCache.TryGetValue(component.GetType().FullName, out var compatibility) == false)
            {
                return default;
            }

            foreach (var typeName in compatibility)
            {
                foreach(var entity in entities)
                {
                    if (entity.alive &&
                        entity.components.TryGetValue(typeName, out var c) &&
                        c == component)
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
