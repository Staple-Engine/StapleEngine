using Staple.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Staple;

public partial class World
{
    private class WorldIterationSimple<T> : IJobParallelFor
        where T: IComponent
    {
        public T[] contents;

        public Action<T, int> callback;

        public int chunkSize;

        public int BatchSize => chunkSize;

        public int ThreadCount => 0;

        public void Execute(int i)
        {
            try
            {
                callback(contents[i], i);
            }
            catch (Exception e)
            {
                Log.Debug($"[World] Threaded iteration exception:\nAt iteration {i}:\n{e}");
            }
        }

        public void Finish()
        {
        }
    }

    private class WorldIteration<T> : IJobParallelFor
        where T: IComponent
    {
        public (Entity, T)[] contents;

        public Action<(Entity, T), int> callback;

        public int chunkSize;

        public int BatchSize => chunkSize;

        public int ThreadCount => 0;

        public void Execute(int i)
        {
            try
            {
                callback(contents[i], i);
            }
            catch (Exception e)
            {
                Log.Debug($"[World] Threaded iteration exception:\nAt iteration {i}:\n{e}");
            }
        }

        public void Finish()
        {
        }
    }

    private class WorldIteration<T, T2> : IJobParallelFor
        where T : IComponent
        where T2 : IComponent
    {
        public (Entity, T, T2)[] contents;

        public Action<(Entity, T, T2), int> callback;

        public int chunkSize;

        public int BatchSize => chunkSize;

        public int ThreadCount => 0;

        public void Execute(int i)
        {
            try
            {
                callback(contents[i], i);
            }
            catch (Exception e)
            {
                Log.Debug($"[World] Threaded iteration exception:\nAt iteration {i}:\n{e}");
            }
        }

        public void Finish()
        {
        }
    }

    private class WorldIteration<T, T2, T3> : IJobParallelFor
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        public (Entity, T, T2, T3)[] contents;

        public Action<(Entity, T, T2, T3), int> callback;

        public int chunkSize;

        public int BatchSize => chunkSize;

        public int ThreadCount => 0;

        public void Execute(int i)
        {
            try
            {
                callback(contents[i], i);
            }
            catch (Exception e)
            {
                Log.Debug($"[World] Threaded iteration exception:\nAt iteration {i}:\n{e}");
            }
        }

        public void Finish()
        {
        }
    }

    private class WorldIteration<T, T2, T3, T4> : IJobParallelFor
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        public (Entity, T, T2, T3, T4)[] contents;

        public Action<(Entity, T, T2, T3, T4), int> callback;

        public int chunkSize;

        public int BatchSize => chunkSize;

        public int ThreadCount => 0;

        public void Execute(int i)
        {
            try
            {
                callback(contents[i], i);
            }
            catch (Exception e)
            {
                Log.Debug($"[World] Threaded iteration exception:\nAt iteration {i}:\n{e}");
            }
        }

        public void Finish()
        {
        }
    }

    private class WorldIteration<T, T2, T3, T4, T5> : IJobParallelFor
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        public (Entity, T, T2, T3, T4, T5)[] contents;

        public Action<(Entity, T, T2, T3, T4, T5), int> callback;

        public int chunkSize;

        public int BatchSize => chunkSize;

        public int ThreadCount => 0;

        public void Execute(int i)
        {
            try
            {
                callback(contents[i], i);
            }
            catch (Exception e)
            {
                Log.Debug($"[World] Threaded iteration exception:\nAt iteration {i}:\n{e}");
            }
        }

        public void Finish()
        {
        }
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public (Entity, T)[] Query<T>(bool includeDisabled) where T : IComponent
    {
        var tName = typeof(T).FullName.GetHashCode();

        if (componentCompatibilityCache.TryGetValue(tName, out var c1) == false)
        {
            return [];
        }

        lock (lockObject)
        {
            var outValue = new List<(Entity, T)>();

            foreach (var entity in cachedEntityList)
            {
                if (entity.alive == false ||
                    (includeDisabled == false && IsEntityEnabled(entity.ToEntity(), true) == false))
                {
                    continue;
                }

                IComponent tComponent = default;

                foreach(var key in c1)
                {
                    if (entity.components.TryGetValue(key, out tComponent))
                    {
                        break;
                    }
                }

                if(tComponent == null)
                {
                    continue;
                }

                var t = (T)tComponent;

                try
                {
                    outValue.Add((entity.ToEntity(), t));
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }

            return outValue.ToArray();
        }
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public (Entity, T, T2)[] Query<T, T2>(bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
    {
        var tName = typeof(T).FullName.GetHashCode();
        var t2Name = typeof(T2).FullName.GetHashCode();

        if (componentCompatibilityCache.TryGetValue(tName, out var c1) == false ||
            componentCompatibilityCache.TryGetValue(t2Name, out var c2) == false)
        {
            return [];
        }

        lock (lockObject)
        {
            var outValue = new List<(Entity, T, T2)>();

            foreach (var entity in cachedEntityList)
            {
                if (entity.alive == false ||
                    (includeDisabled == false && IsEntityEnabled(entity.ToEntity(), true) == false))
                {
                    continue;
                }

                IComponent tComponent = default;
                IComponent t2Component = default;

                foreach (var key in c1)
                {
                    if (entity.components.TryGetValue(key, out tComponent))
                    {
                        break;
                    }
                }

                foreach (var key in c2)
                {
                    if (entity.components.TryGetValue(key, out t2Component))
                    {
                        break;
                    }
                }

                if (tComponent == null ||
                    t2Component == null)
                {
                    continue;
                }

                var t = (T)tComponent;
                var t2 = (T2)t2Component;

                try
                {
                    outValue.Add((entity.ToEntity(), t, t2));
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }

            return outValue.ToArray();
        }
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public (Entity, T, T2, T3)[] Query<T, T2, T3>(bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        var tName = typeof(T).FullName.GetHashCode();
        var t2Name = typeof(T2).FullName.GetHashCode();
        var t3Name = typeof(T3).FullName.GetHashCode();

        if (componentCompatibilityCache.TryGetValue(tName, out var c1) == false ||
            componentCompatibilityCache.TryGetValue(t2Name, out var c2) == false ||
            componentCompatibilityCache.TryGetValue(t3Name, out var c3) == false)
        {
            return [];
        }

        lock (lockObject)
        {
            var outValue = new List<(Entity, T, T2, T3)>();

            foreach (var entity in cachedEntityList)
            {
                if (entity.alive == false ||
                    (includeDisabled == false && IsEntityEnabled(entity.ToEntity(), true) == false))
                {
                    continue;
                }

                IComponent tComponent = default;
                IComponent t2Component = default;
                IComponent t3Component = default;

                foreach (var key in c1)
                {
                    if (entity.components.TryGetValue(key, out tComponent))
                    {
                        break;
                    }
                }

                foreach (var key in c2)
                {
                    if (entity.components.TryGetValue(key, out t2Component))
                    {
                        break;
                    }
                }

                foreach (var key in c3)
                {
                    if (entity.components.TryGetValue(key, out t3Component))
                    {
                        break;
                    }
                }

                if (tComponent == null ||
                    t2Component == null ||
                    t3Component == null)
                {
                    continue;
                }

                var t = (T)tComponent;
                var t2 = (T2)t2Component;
                var t3 = (T3)t3Component;

                try
                {
                    outValue.Add((entity.ToEntity(), t, t2, t3));
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }

            return outValue.ToArray();
        }
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <typeparam name="T4">The type of the fourth component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public (Entity, T, T2, T3, T4)[] Query<T, T2, T3, T4>(bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        var tName = typeof(T).FullName.GetHashCode();
        var t2Name = typeof(T2).FullName.GetHashCode();
        var t3Name = typeof(T3).FullName.GetHashCode();
        var t4Name = typeof(T4).FullName.GetHashCode();

        if (componentCompatibilityCache.TryGetValue(tName, out var c1) == false ||
            componentCompatibilityCache.TryGetValue(t2Name, out var c2) == false ||
            componentCompatibilityCache.TryGetValue(t3Name, out var c3) == false ||
            componentCompatibilityCache.TryGetValue(t4Name, out var c4) == false)
        {
            return [];
        }

        lock (lockObject)
        {
            var outValue = new List<(Entity, T, T2, T3, T4)>();

            foreach (var entity in cachedEntityList)
            {
                if (entity.alive == false ||
                    (includeDisabled == false && IsEntityEnabled(entity.ToEntity(), true) == false))
                {
                    continue;
                }

                IComponent tComponent = default;
                IComponent t2Component = default;
                IComponent t3Component = default;
                IComponent t4Component = default;

                foreach (var key in c1)
                {
                    if (entity.components.TryGetValue(key, out tComponent))
                    {
                        break;
                    }
                }

                foreach (var key in c2)
                {
                    if (entity.components.TryGetValue(key, out t2Component))
                    {
                        break;
                    }
                }

                foreach (var key in c3)
                {
                    if (entity.components.TryGetValue(key, out t3Component))
                    {
                        break;
                    }
                }

                foreach (var key in c4)
                {
                    if (entity.components.TryGetValue(key, out t4Component))
                    {
                        break;
                    }
                }

                if (tComponent == null ||
                    t2Component == null ||
                    t3Component == null ||
                    t4Component == null)
                {
                    continue;
                }

                var t = (T)tComponent;
                var t2 = (T2)t2Component;
                var t3 = (T3)t3Component;
                var t4 = (T4)t4Component;

                try
                {
                    outValue.Add((entity.ToEntity(), t, t2, t3, t4));
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }

            return outValue.ToArray();
        }
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <typeparam name="T4">The type of the fourth component</typeparam>
    /// <typeparam name="T5">The type of the fifth component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public (Entity, T, T2, T3, T4, T5)[] Query<T, T2, T3, T4, T5>(bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        var tName = typeof(T).FullName.GetHashCode();
        var t2Name = typeof(T2).FullName.GetHashCode();
        var t3Name = typeof(T3).FullName.GetHashCode();
        var t4Name = typeof(T4).FullName.GetHashCode();
        var t5Name = typeof(T5).FullName.GetHashCode();

        if (componentCompatibilityCache.TryGetValue(tName, out var c1) == false ||
            componentCompatibilityCache.TryGetValue(t2Name, out var c2) == false ||
            componentCompatibilityCache.TryGetValue(t3Name, out var c3) == false ||
            componentCompatibilityCache.TryGetValue(t4Name, out var c4) == false ||
            componentCompatibilityCache.TryGetValue(t5Name, out var c5) == false)
        {
            return [];
        }

        lock (lockObject)
        {
            var outValue = new List<(Entity, T, T2, T3, T4, T5)>();

            foreach (var entity in cachedEntityList)
            {
                if (entity.alive == false ||
                    (includeDisabled == false && IsEntityEnabled(entity.ToEntity(), true) == false))
                {
                    continue;
                }

                IComponent tComponent = default;
                IComponent t2Component = default;
                IComponent t3Component = default;
                IComponent t4Component = default;
                IComponent t5Component = default;

                foreach (var key in c1)
                {
                    if (entity.components.TryGetValue(key, out tComponent))
                    {
                        break;
                    }
                }

                foreach (var key in c2)
                {
                    if (entity.components.TryGetValue(key, out t2Component))
                    {
                        break;
                    }
                }

                foreach (var key in c3)
                {
                    if (entity.components.TryGetValue(key, out t3Component))
                    {
                        break;
                    }
                }

                foreach (var key in c4)
                {
                    if (entity.components.TryGetValue(key, out t4Component))
                    {
                        break;
                    }
                }

                foreach (var key in c5)
                {
                    if (entity.components.TryGetValue(key, out t5Component))
                    {
                        break;
                    }
                }

                if (tComponent == null ||
                    t2Component == null ||
                    t3Component == null ||
                    t4Component == null ||
                    t5Component == null)
                {
                    continue;
                }

                var t = (T)tComponent;
                var t2 = (T2)t2Component;
                var t3 = (T3)t3Component;
                var t4 = (T4)t4Component;
                var t5 = (T5)t5Component;

                try
                {
                    outValue.Add((entity.ToEntity(), t, t2, t3, t4, t5));
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }

            return outValue.ToArray();
        }
    }

    /// <summary>
    /// Gets an entity by ID
    /// </summary>
    /// <param name="ID">The entity's ID</param>
    /// <returns>The entity if valid, or Entity.Empty</returns>
    public Entity FindEntity(int ID)
    {
        var localID = ID - 1;

        lock (lockObject)
        {
            if(localID < 0 || localID >= entities.Count)
            {
                return default;
            }

            var e = entities[localID];

            if(e.alive == false)
            {
                return default;
            }

            return new Entity()
            {
                Identifier = new()
                {
                    ID = e.ID,
                    generation = e.generation,
                },
            };
        }
    }

    /// <summary>
    /// Attempts to find an entity by name
    /// </summary>
    /// <param name="name">The entity's name</param>
    /// <param name="allowDisabled">Whether to allow finding disabled entities</param>
    /// <returns>The entity if valid, or Entity.Empty</returns>
    public Entity FindEntity(string name, bool allowDisabled = false)
    {
        lock(lockObject)
        {
            foreach(var pair in entities)
            {
                if(pair.alive == false || (pair.enabled == false && allowDisabled == false))
                {
                    continue;
                }

                if(pair.name == name)
                {
                    return new Entity()
                    {
                        Identifier = new()
                        {
                            ID = pair.ID,
                            generation = pair.generation,
                        },
                    };
                }
            }
        }

        return default;
    }

    /// <summary>
    /// Attempts to find an entity and get a specific component
    /// </summary>
    /// <param name="name">The entity's name</param>
    /// <param name="allowDisabled">Whether to allow finding disabled entities</param>
    /// <param name="componentType">The component's type</param>
    /// <param name="component">The returned component if successful</param>
    /// <returns>Whether the entity and component were found</returns>
    public bool TryFindEntityComponent(string name, bool allowDisabled, Type componentType, out IComponent component)
    {
        var e = FindEntity(name, allowDisabled);

        return TryGetComponent(e, out component, componentType);
    }

    /// <summary>
    /// Attempts to find an entity and get a specific component
    /// </summary>
    /// <typeparam name="T">The component type</typeparam>
    /// <param name="name">The entity's name</param>
    /// <param name="allowDisabled">Whether to allow finding disabled entities</param>
    /// <param name="component">The returned component if successful</param>
    /// <returns>Whether the entity and component were found</returns>
    public bool TryFindEntityComponent<T>(string name, bool allowDisabled, out T component) where T: IComponent
    {
        var e = FindEntity(name, allowDisabled);

        return TryGetComponent(e, out component);
    }

    /// <summary>
    /// Iterates through threading
    /// </summary>
    /// <typeparam name="T">The first component type</typeparam>
    /// <param name="contents">The contents to iterate through</param>
    /// <param name="callback">The callback for each iteration, with the elements and the index</param>
    public static void IterateThreaded<T>(T[] contents, Action<T, int> callback) where T : IComponent
    {
        if ((contents?.Length ?? 0) == 0)
        {
            return;
        }

        var handle = JobScheduler.Schedule(new WorldIterationSimple<T>()
        {
            callback = callback,
            contents = contents,
            chunkSize = JobScheduler.ChunkSize(contents.Length),
        }, contents.Length);

        handle.Complete();
    }

    /// <summary>
    /// Iterates through threading
    /// </summary>
    /// <typeparam name="T">The first component type</typeparam>
    /// <param name="contents">The contents to iterate through</param>
    /// <param name="callback">The callback for each iteration, with the elements and the index</param>
    public static void IterateThreaded<T>((Entity, T)[] contents, Action<(Entity, T), int> callback) where T: IComponent
    {
        if((contents?.Length ?? 0) == 0)
        {
            return;
        }

        var handle = JobScheduler.Schedule(new WorldIteration<T>()
        {
            callback = callback,
            contents = contents,
            chunkSize = JobScheduler.ChunkSize(contents.Length),
        }, contents.Length);

        handle.Complete();
    }

    /// <summary>
    /// Iterates through threading
    /// </summary>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    /// <param name="contents">The contents to iterate through</param>
    /// <param name="callback">The callback for each iteration, with the elements and the index</param>
    public static void IterateThreaded<T, T2>((Entity, T, T2)[] contents, Action<(Entity, T, T2), int> callback)
        where T : IComponent
        where T2 : IComponent
    {
        if ((contents?.Length ?? 0) == 0)
        {
            return;
        }

        var handle = JobScheduler.Schedule(new WorldIteration<T, T2>()
        {
            callback = callback,
            contents = contents,
            chunkSize = JobScheduler.ChunkSize(contents.Length),
        }, contents.Length);

        handle.Complete();
    }

    /// <summary>
    /// Iterates through threading
    /// </summary>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    /// <typeparam name="T3">The third component type</typeparam>
    /// <param name="contents">The contents to iterate through</param>
    /// <param name="callback">The callback for each iteration, with the elements and the index</param>
    public static void IterateThreaded<T, T2, T3>((Entity, T, T2, T3)[] contents, Action<(Entity, T, T2, T3), int> callback)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        if ((contents?.Length ?? 0) == 0)
        {
            return;
        }

        var handle = JobScheduler.Schedule(new WorldIteration<T, T2, T3>()
        {
            callback = callback,
            contents = contents,
            chunkSize = JobScheduler.ChunkSize(contents.Length),
        }, contents.Length);

        handle.Complete();
    }

    /// <summary>
    /// Iterates through threading
    /// </summary>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    /// <typeparam name="T3">The third component type</typeparam>
    /// <typeparam name="T4">The fourth component type</typeparam>
    /// <param name="contents">The contents to iterate through</param>
    /// <param name="callback">The callback for each iteration, with the elements and the index</param>
    public static void IterateThreaded<T, T2, T3, T4>((Entity, T, T2, T3, T4)[] contents, Action<(Entity, T, T2, T3, T4), int> callback)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        if ((contents?.Length ?? 0) == 0)
        {
            return;
        }

        var handle = JobScheduler.Schedule(new WorldIteration<T, T2, T3, T4>()
        {
            callback = callback,
            contents = contents,
            chunkSize = JobScheduler.ChunkSize(contents.Length),
        }, contents.Length);

        handle.Complete();
    }

    /// <summary>
    /// Iterates through threading
    /// </summary>
    /// <typeparam name="T">The first component type</typeparam>
    /// <typeparam name="T2">The second component type</typeparam>
    /// <typeparam name="T3">The third component type</typeparam>
    /// <typeparam name="T4">The fourth component type</typeparam>
    /// <typeparam name="T5">The fifth component type</typeparam>
    /// <param name="contents">The contents to iterate through</param>
    /// <param name="callback">The callback for each iteration, with the elements and the index</param>
    public static void IterateThreaded<T, T2, T3, T4, T5>((Entity, T, T2, T3, T4, T5)[] contents, Action<(Entity, T, T2, T3, T4, T5), int> callback)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        if ((contents?.Length ?? 0) == 0)
        {
            return;
        }

        var handle = JobScheduler.Schedule(new WorldIteration<T, T2, T3, T4, T5>()
        {
            callback = callback,
            contents = contents,
            chunkSize = JobScheduler.ChunkSize(contents.Length),
        }, contents.Length);

        handle.Complete();
    }

    /// <summary>
    /// Iterates through each entity in the world
    /// </summary>
    /// <param name="callback">A callback to handle the entity</param>
    internal void Iterate(Action<Entity> callback)
    {
        lock (lockObject)
        {
            foreach (var entity in cachedEntityList)
            {
                if (entity.alive == false)
                {
                    continue;
                }

                callback(new Entity()
                {
                    Identifier = new()
                    {
                        ID = entity.ID,
                        generation = entity.generation,
                    }
                });
            }
        }
    }

    /// <summary>
    /// Iterates through the components of an entity
    /// </summary>
    /// <param name="entity">The entity to iterate</param>
    /// <param name="callback">A callback to handle the component</param>
    internal void IterateComponents(Entity entity, IterateComponentCallback callback)
    {
        if(TryGetEntity(entity, out var entityInfo) == false)
        {
            return;
        }

        lock (lockObject)
        {
            //TODO: Figure out a way without allocations. We can have layers of iterations mixed in due to callbacks.
            var cache = entityInfo.components.ToArray();

            foreach (var pair in cache)
            {
                if(entityInfo.alive == false)
                {
                    break;
                }

                var component = pair.Value;

                callback(ref component);

                if(component.GetType().IsValueType)
                {
                    entityInfo.components[pair.Key] = component;
                }
            }
        }
    }

    /// <summary>
    /// Iterates through every callable component type
    /// </summary>
    /// <param name="callback">A callback to execute with the component</param>
    internal void IterateCallableComponents(CallableComponentCallback callback)
    {
        if (Platform.IsPlaying == false)
        {
            return;
        }

        callableComponents ??= new();

        lock (lockObject)
        {
            if (callableComponentTypes.Count == 0)
            {
                return;
            }

            foreach ((Entity entity, CallbackComponent component) in callableComponents)
            {
                try
                {
                    callback?.Invoke(entity, component);
                }
                catch (Exception e)
                {
                    Log.Error($"[World] Failed to handle callable component callback: {e}");
                }
            }
        }
    }
}
