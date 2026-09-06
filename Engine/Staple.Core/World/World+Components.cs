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
        sceneQueries.RemoveAll(assembly);
        worldChangeReceivers.RemoveAll(assembly);

        lock(lockObject)
        {
            var keys = componentCompatibilityCache.Keys.ToList();

            for(var i = 0; i < keys.Count; i++)
            {
                var key = keys[i];

                if (!componentNameHashes.TryGetValue(key, out var typeName))
                {
                    continue;
                }

                var type = TypeCache.GetType(typeName);

                if(type != null && type.Assembly == assembly)
                {
                    needsEmitWorldChange = true;

                    componentCompatibilityCache.Remove(key);
                    callableComponentTypes.Remove(key);

                    foreach (var pair in componentCompatibilityCache)
                    {
                        pair.Value.Remove(key);
                    }

                    foreach(var entity in entities.Contents)
                    {
                        if(entity.components.TryGetValue(key, out var container))
                        {
                            entity.componentsArray.Remove(container);

                            entity.components.Remove(key);
                        }
                    }
                }
            }
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
        (Entity entity) where T : Component
    {
        return (T)AddComponent(entity, typeof(T));
    }

    /// <summary>
    /// Adds a component to an entity
    /// </summary>
    /// <param name="entity">The entity to add the component to</param>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    public Component AddComponent(Entity entity,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        Type t)
    {
        if(t.GetCustomAttribute<AbstractComponentAttribute>() != null ||
            !TryGetEntity(entity, out var entityInfo))
        {
            return default;
        }

        lock (lockObject)
        {
            EnsureComponentInfo(t);

            var hash = t.FullName.GetHashCode();

            removedComponents.Remove((entity, hash));

            if (entityInfo.components.TryGetValue(hash, out var container))
            {
                //Already has one, return it

                return container.component;
            }

            var component = ObjectCreation.CreateObject<Component>(t);

            if (component == default)
            {
                return default;
            }

            container = new ComponentHolder(hash, component, TypeCache.ComponentShouldBeVersionedByWorld(t.FullName), 0);

            needsEmitWorldChange = true;

            entityInfo.components.Add(hash, container);
            entityInfo.componentsArray.Add(container);

            container.component.Entity = entity;

            var requiredComponents = t.GetCustomAttributes<RequireComponentAttribute>();

            if (container.component is Transform transform)
            {
                entityInfo.transform = transform;
            }

            if (entityInfo.transform != null)
            {
                container.component.Transform = entityInfo.transform;
            }

            foreach (var req in requiredComponents)
            {
                var instance = GetComponent(entity, req.type);

                if(instance == null)
                {
                    instance = AddComponent(entity, req.type);
                }

                if(instance == null)
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(req.field))
                {
                    try
                    {
                        var field = t.GetField(req.field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                        if(field != null)
                        {
                            field.SetValue(component, instance);
                        }
                        else
                        {
                            var property = t.GetProperty(req.field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                            if(property != null && property.CanWrite)
                            {
                                property.SetValue(component, instance);
                            }
                        }
                    }
                    catch(Exception e)
                    {
                        Log.Debug($"{entity.Name} ({t.FullName}): Failed to apply required component {req.type.FullName} to field or property {req.field}: {e}");
                    }
                }
            }

            if (!Scene.InstancingComponent)
            {
                EmitAddComponentEvent(entity, ref container.component);
            }

            if(!Scene.InstancingComponent &&
                callableComponentTypes.Count != 0 &&
                container.component is CallbackComponent callback &&
                callback.ShouldExecuteEvents)
            {
                try
                {
                    callback.Awake();
                }
                catch (Exception e)
                {
                    Log.Debug($"{entity.Name} ({callback.GetType().FullName}): Exception thrown while handling Awake: {e}");
                }
            }

            return container.component;
        }
    }

    private void EnsureComponentInfo(Type t)
    {
        var added = false;
        var hash = t.FullName.GetHashCode();

        if (!componentCompatibilityCache.TryGetValue(hash, out var compatibleTypes))
        {
            added = true;
            compatibleTypes = [];
            componentNameHashes.Add(hash, t.FullName);
        }

        if (added)
        {
            compatibleTypes.Add(hash);

            void RecursiveAdd(Type target)
            {
                foreach (var targetInterface in target.GetInterfaces())
                {
                    if (targetInterface != typeof(Component) && targetInterface.IsAssignableTo(typeof(Component)))
                    {
                        var targetHash = targetInterface.FullName.GetHashCode();

                        compatibleTypes.Add(targetHash);

                        EnsureComponentInfo(targetInterface);

                        if (componentCompatibilityCache.TryGetValue(targetHash, out var interfaceCompatibility))
                        {
                            interfaceCompatibility.Add(hash);
                        }
                    }
                }

                if (target.BaseType == null ||
                    target.BaseType == typeof(Component) ||
                    !target.BaseType.IsAssignableTo(typeof(Component)))
                {
                    return;
                }

                var baseHash = target.BaseType.FullName.GetHashCode();

                compatibleTypes.Add(baseHash);

                EnsureComponentInfo(target.BaseType);

                if(componentCompatibilityCache.TryGetValue(baseHash, out var baseCompatibility))
                {
                    baseCompatibility.Add(hash);
                }

                RecursiveAdd(target.BaseType);
            }

            RecursiveAdd(t);

            componentCompatibilityCache.Add(hash, compatibleTypes);

            if (t.IsSubclassOf(typeof(CallbackComponent)))
            {
                callableComponentTypes.Add(hash);
            }
        }
    }

    /// <summary>
    /// Removes a component from an entity
    /// </summary>
    /// <typeparam name="T">The type to remove</typeparam>
    /// <param name="entity">The entity to remove the component from</param>
    public void RemoveComponent<T>(Entity entity) where T : Component
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
        if(!TryGetEntity(entity, out var entityInfo))
        {
            return;
        }

        lock (lockObject)
        {
            var tHash = t.FullName.GetHashCode();

            if (!componentCompatibilityCache.TryGetValue(tHash, out var compatibility))
            {
                return;
            }

            foreach(var typeName in compatibility)
            {
                if (entityInfo.components.TryGetValue(typeName, out var container))
                {
                    needsEmitWorldChange = true;

                    removedComponents.Add((entity, typeName));

                    if(t == typeof(Transform))
                    {
                        entityInfo.transform = null;

                        foreach(var c in entityInfo.componentsArray.Contents)
                        {
                            c.component.Transform = null;
                        }
                    }

                    if (callableComponentTypes.Count != 0 &&
                        container.component is CallbackComponent callback &&
                        callback.ShouldExecuteEvents)
                    {
                        try
                        {
                            callback.OnDestroy();
                        }
                        catch (Exception e)
                        {
                            Log.Debug($"{entity.Name} ({callback.GetType().FullName}): Exception thrown while handling OnDestroy: {e}");
                        }
                    }

                    EmitRemoveComponentEvent(entity, ref container.component);

                    if(container.component is IComponentDisposable disposable)
                    {
                        disposable.DisposeComponent();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <param name="entityInfo">The entity to get from</param>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    internal Component GetComponent(EntityInfo entityInfo, Type t)
    {
        if (!typeof(Component).IsAssignableFrom(t))
        {
            return default;
        }

        lock (lockObject)
        {
            if (!componentCompatibilityCache.TryGetValue(t.FullName.GetHashCode(), out var compatibility))
            {
                return default;
            }

            foreach (var typeName in compatibility)
            {
                if (entityInfo.components.TryGetValue(typeName, out var container))
                {
                    return container.component;
                }
            }

            return default;
        }
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <param name="entity">The entity to get from</param>
    /// <param name="t">The component type</param>
    /// <returns>The component instance, or default</returns>
    public Component GetComponent(Entity entity, Type t)
    {
        if (!typeof(Component).IsAssignableFrom(t) ||
            !TryGetEntity(entity, out var entityInfo))
        {
            return default;
        }

        return GetComponent(entityInfo, t);
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <param name="entity">The entity to get from</param>
    /// <returns>The component instance, or default</returns>
    internal T GetComponent<T>(EntityInfo entity) where T : Component
    {
        return (T)GetComponent(entity, typeof(T));
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <param name="entity">The entity to get from</param>
    /// <returns>The component instance, or default</returns>
    public T GetComponent<T>(Entity entity) where T : Component
    {
        return (T)GetComponent(entity, typeof(T));
    }

    /// <summary>
    /// Attempts to get a component from an internal entity, without locking
    /// </summary>
    /// <param name="info">The entity info</param>
    /// <param name="t">The type</param>
    /// <param name="component">The component</param>
    /// <returns>Whether the component was found</returns>
    internal bool TryGetComponentNoLock(EntityInfo info, Type t, out Component component)
    {
        if (!componentCompatibilityCache.TryGetValue(t.FullName.GetHashCode(), out var compatibility))
        {
            component = default;

            return false;
        }

        foreach (var typeName in compatibility)
        {
            if (info.components.TryGetValue(typeName, out var container))
            {
                component = container.component;

                return true;
            }
        }

        component = default;

        return false;
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <param name="entity">The entity to get from</param>
    /// <param name="t">The component type</param>
    /// <param name="component">The component instance</param>
    /// <returns>Whether the component was found</returns>
    public bool TryGetComponent(Entity entity, Type t, out Component component)
    {
        if (!typeof(Component).IsAssignableFrom(t) ||
            !TryGetEntity(entity, out var entityInfo))
        {
            component = default;

            return false;
        }

        lock (lockObject)
        {
            return TryGetComponentNoLock(entityInfo, t, out component);
        }
    }

    /// <summary>
    /// Attempts to get a component from an entity
    /// </summary>
    /// <param name="entity">The entity to get from</param>
    /// <param name="component">The component instance</param>
    /// <typeparam name="T">The component type</typeparam>
    /// <returns>Whether the component was found</returns>
    public bool TryGetComponent<T>(Entity entity, out T component) where T: Component
    {
        if(TryGetComponent(entity, typeof(T), out Component c))
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
    /// <remarks>If the component doesn't exist, a new instance will be created and replaced with the new one</remarks>
    /// <param name="entity">The entity to update</param>
    /// <param name="component">The component instance to replace</param>
    public void SetComponent(Entity entity, Component component)
    {
        if (!TryGetEntity(entity, out var entityInfo) ||
            component is null)
        {
            return;
        }

        if(GetComponent(entity, component.GetType()) == null)
        {
            AddComponent(entity, component.GetType());
        }

        lock (lockObject)
        {
            if (!componentCompatibilityCache.TryGetValue(component.GetType().FullName.GetHashCode(), out var compatibility))
            {
                return;
            }

            foreach (var typeName in compatibility)
            {
                if (!entityInfo.components.ContainsKey(typeName))
                {
                    continue;
                }

                removedComponents.Remove((entity, typeName.GetHashCode()));

                entityInfo.components[typeName].component = component;

                component.Entity = entity;
                component.Transform = entityInfo.transform;

                needsEmitWorldChange = true;
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
        if(!componentType.IsSubclassOf(typeof(Component)))
        {
            return;
        }

        lock(globalLockObject)
        {
            if(!componentAddedCallbacks.TryGetValue(componentType.FullName.GetHashCode(), out var c))
            {
                c = [];

                componentAddedCallbacks.Add(componentType.FullName.GetHashCode(), c);
            }

            if(c.Contains(callback))
            {
                return;
            }

            c.Add(callback);
        }
    }

    /// <summary>
    /// Adds a callback for when a component is changed
    /// </summary>
    /// <param name="componentType">The component type</param>
    /// <param name="callback">The callback to call</param>
    public static void AddComponentChangedCallback([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] Type componentType,
        OnComponentChangedCallback callback)
    {
        if (!componentType.IsSubclassOf(typeof(Component)))
        {
            return;
        }

        lock (globalLockObject)
        {
            if (!componentChangedCallbacks.TryGetValue(componentType.FullName.GetHashCode(), out var c))
            {
                c = [];

                componentChangedCallbacks.Add(componentType.FullName.GetHashCode(), c);
            }

            if (c.Contains(callback))
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
        if (!componentType.IsSubclassOf(typeof(Component)))
        {
            return;
        }

        lock (globalLockObject)
        {
            if (!componentRemovedCallbacks.TryGetValue(componentType.FullName.GetHashCode(), out var c))
            {
                c = [];

                componentRemovedCallbacks.Add(componentType.FullName.GetHashCode(), c);
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
    internal void EmitAddComponentEvent(Entity entity, ref Component component)
    {
        var hash = component?.GetType().FullName.GetHashCode() ?? 0;

        if (component == null ||
            !TryGetEntity(entity, out var entityInfo) ||
            entityInfo.emittedAddComponents.Contains(hash))
        {
            return;
        }

        lock (globalLockObject)
        {
            if (componentAddedCallbacks.TryGetValue(hash, out var callbacks))
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

                    entityInfo.emittedAddComponents.Add(hash);
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
    /// Emits a component changed event
    /// </summary>
    /// <param name="entity">The entity to emit for</param>
    /// <param name="component">The component that was added</param>
    internal void EmitChangedComponentEvent(Entity entity, ref Component component)
    {
        var hash = component?.GetType().FullName.GetHashCode() ?? 0;

        if (component == null ||
            !TryGetEntity(entity, out var entityInfo))
        {
            return;
        }

        lock (globalLockObject)
        {
            if (componentChangedCallbacks.TryGetValue(hash, out var callbacks))
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
                        Log.Debug($"[World] ChangeComponent: Failed to handle a component change callback: {ex}");
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
    internal void EmitRemoveComponentEvent(Entity entity, ref Component component)
    {
        var hash = component?.GetType().FullName.GetHashCode() ?? 0;

        if (component == null ||
            !TryGetEntity(entity, out var entityInfo) ||
            !entityInfo.emittedAddComponents.Contains(hash))
        {
            return;
        }

        lock (globalLockObject)
        {
            if (componentRemovedCallbacks.TryGetValue(hash, out var callbacks))
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

                    entityInfo.emittedAddComponents.Remove(hash);
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
    /// Attempts to find the entity for a component. Mostly works with classes, since it compares each.
    /// </summary>
    /// <param name="component">The component to check</param>
    /// <returns>The entity, if valid</returns>
    public Entity GetComponentEntity(Component component)
    {
        if(component == null)
        {
            return default;
        }

        lock (lockObject)
        {
            if(!componentCompatibilityCache.TryGetValue(component.GetType().FullName.GetHashCode(), out var compatibility))
            {
                return default;
            }

            foreach (var typeName in compatibility)
            {
                foreach(var entity in entities.Contents)
                {
                    if (entity.alive &&
                        entity.components.TryGetValue(typeName, out var c) &&
                        c.component == component)
                    {
                        return entity.entityValue;
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
    public bool TryGetComponentEntity(Component component, out Entity entity)
    {
        entity = GetComponentEntity(component);

        return entity.IsValid;
    }
}
