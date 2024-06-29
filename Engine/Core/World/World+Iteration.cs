using System;
using System.Collections.Generic;
using System.Linq;

namespace Staple;

public partial class World
{
    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    /// <returns>An array of a tuple with each entity and the requested components</returns>
    public (Entity, T)[] ForEach<T>(bool includeDisabled) where T : IComponent
    {
        if (ComponentIndices(typeof(T)).Any() == false)
        {
            return [];
        }

        lock (lockObject)
        {
            //TODO: Figure out a way without allocations. We can have layers of iterations mixed in due to callbacks.
            var allEntities = entities.ToArray();
            var outValue = new List<(Entity, T)>();

            foreach (var entity in allEntities)
            {
                if (entity.alive == false ||
                    (includeDisabled == false && entity.enabled == false))
                {
                    continue;
                }

                var index = ComponentIndices(entity, typeof(T)).FirstOrDefault(x => entity.components.Contains(x), -1);

                if (index < 0)
                {
                    continue;
                }

                T t = (T)componentsRepository[index].components[entity.localID];

                try
                {
                    var e = new Entity()
                    {
                        Identifier = new()
                        {
                            ID = entity.ID,
                            generation = entity.generation,
                        },
                    };

                    outValue.Add((e, t));
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
    public (Entity, T, T2)[] ForEach<T, T2>(bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
    {
        if (ComponentIndices(typeof(T)).Any() == false ||
            ComponentIndices(typeof(T2)).Any() == false)
        {
            return [];
        }

        lock (lockObject)
        {
            //TODO: Figure out a way without allocations. We can have layers of iterations mixed in due to callbacks.
            var allEntities = entities.ToArray();
            var outValue = new List<(Entity, T, T2)>();

            foreach (var entity in allEntities)
            {
                if (entity.alive == false ||
                    (includeDisabled == false && entity.enabled == false))
                {
                    continue;
                }

                var index = ComponentIndices(entity, typeof(T)).FirstOrDefault(x => entity.components.Contains(x), -1);
                var index2 = ComponentIndices(entity, typeof(T2)).FirstOrDefault(x => entity.components.Contains(x), -1);

                if (index < 0 ||
                    index2 < 0)
                {
                    continue;
                }

                T t = (T)componentsRepository[index].components[entity.localID];
                T2 t2 = (T2)componentsRepository[index2].components[entity.localID];

                try
                {
                    var e = new Entity()
                    {
                        Identifier = new()
                        {
                            ID = entity.ID,
                            generation = entity.generation,
                        },
                    };

                    outValue.Add((e, t, t2));
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
    public (Entity, T, T2, T3)[] ForEach<T, T2, T3>(bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        if (ComponentIndices(typeof(T)).Any() == false ||
            ComponentIndices(typeof(T2)).Any() == false ||
            ComponentIndices(typeof(T3)).Any() == false)
        {
            return [];
        }

        lock (lockObject)
        {
            //TODO: Figure out a way without allocations. We can have layers of iterations mixed in due to callbacks.
            var allEntities = entities.ToArray();
            var outValue = new List<(Entity, T, T2, T3)>();

            foreach (var entity in allEntities)
            {
                if (entity.alive == false ||
                    (includeDisabled == false && entity.enabled == false))
                {
                    continue;
                }

                var index = ComponentIndices(entity, typeof(T)).FirstOrDefault(x => entity.components.Contains(x), -1);
                var index2 = ComponentIndices(entity, typeof(T2)).FirstOrDefault(x => entity.components.Contains(x), -1);
                var index3 = ComponentIndices(entity, typeof(T3)).FirstOrDefault(x => entity.components.Contains(x), -1);

                if (index < 0 ||
                    index2 < 0 ||
                    index3 < 0)
                {
                    continue;
                }

                T t = (T)componentsRepository[index].components[entity.localID];
                T2 t2 = (T2)componentsRepository[index2].components[entity.localID];
                T3 t3 = (T3)componentsRepository[index3].components[entity.localID];

                try
                {
                    var e = new Entity()
                    {
                        Identifier = new()
                        {
                            ID = entity.ID,
                            generation = entity.generation,
                        },
                    };

                    outValue.Add((e, t, t2, t3));
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
    public (Entity, T, T2, T3, T4)[] ForEach<T, T2, T3, T4>(bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        if (ComponentIndices(typeof(T)).Any() == false ||
            ComponentIndices(typeof(T2)).Any() == false ||
            ComponentIndices(typeof(T3)).Any() == false ||
            ComponentIndices(typeof(T4)).Any() == false)
        {
            return [];
        }

        lock (lockObject)
        {
            //TODO: Figure out a way without allocations. We can have layers of iterations mixed in due to callbacks.
            var allEntities = entities.ToArray();
            var outValue = new List<(Entity, T, T2, T3, T4)>();

            foreach (var entity in allEntities)
            {
                if (entity.alive == false ||
                    (includeDisabled == false && entity.enabled == false))
                {
                    continue;
                }

                var index = ComponentIndices(entity, typeof(T)).FirstOrDefault(x => entity.components.Contains(x), -1);
                var index2 = ComponentIndices(entity, typeof(T2)).FirstOrDefault(x => entity.components.Contains(x), -1);
                var index3 = ComponentIndices(entity, typeof(T3)).FirstOrDefault(x => entity.components.Contains(x), -1);
                var index4 = ComponentIndices(entity, typeof(T4)).FirstOrDefault(x => entity.components.Contains(x), -1);

                if (index < 0 ||
                    index2 < 0 ||
                    index3 < 0 ||
                    index4 < 0)
                {
                    continue;
                }

                T t = (T)componentsRepository[index].components[entity.localID];
                T2 t2 = (T2)componentsRepository[index2].components[entity.localID];
                T3 t3 = (T3)componentsRepository[index3].components[entity.localID];
                T4 t4 = (T4)componentsRepository[index4].components[entity.localID];

                try
                {
                    var e = new Entity()
                    {
                        Identifier = new()
                        {
                            ID = entity.ID,
                            generation = entity.generation,
                        },
                    };

                    outValue.Add((e, t, t2, t3, t4));
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
    public (Entity, T, T2, T3, T4, T5)[] ForEach<T, T2, T3, T4, T5>(bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        if (ComponentIndices(typeof(T)).Any() ||
            ComponentIndices(typeof(T2)).Any() ||
            ComponentIndices(typeof(T3)).Any() ||
            ComponentIndices(typeof(T4)).Any() ||
            ComponentIndices(typeof(T5)).Any())
        {
            return [];
        }

        lock (lockObject)
        {
            //TODO: Figure out a way without allocations. We can have layers of iterations mixed in due to callbacks.
            var allEntities = entities.ToArray();
            var outValue = new List<(Entity, T, T2, T3, T4, T5)>();

            foreach (var entity in allEntities)
            {
                if (entity.alive == false ||
                    (includeDisabled == false && entity.enabled == false))
                {
                    continue;
                }

                var index = ComponentIndices(entity, typeof(T)).FirstOrDefault(x => entity.components.Contains(x), -1);
                var index2 = ComponentIndices(entity, typeof(T2)).FirstOrDefault(x => entity.components.Contains(x), -1);
                var index3 = ComponentIndices(entity, typeof(T3)).FirstOrDefault(x => entity.components.Contains(x), -1);
                var index4 = ComponentIndices(entity, typeof(T4)).FirstOrDefault(x => entity.components.Contains(x), -1);
                var index5 = ComponentIndices(entity, typeof(T5)).FirstOrDefault(x => entity.components.Contains(x), -1);

                if (index < 0 ||
                    index2 < 0 ||
                    index3 < 0 ||
                    index4 < 0 ||
                    index5 < 0)
                {
                    continue;
                }

                T t = (T)componentsRepository[index].components[entity.localID];
                T2 t2 = (T2)componentsRepository[index2].components[entity.localID];
                T3 t3 = (T3)componentsRepository[index3].components[entity.localID];
                T4 t4 = (T4)componentsRepository[index4].components[entity.localID];
                T5 t5 = (T5)componentsRepository[index5].components[entity.localID];

                try
                {
                    var e = new Entity()
                    {
                        Identifier = new()
                        {
                            ID = entity.ID,
                            generation = entity.generation,
                        },
                    };

                    outValue.Add((e, t, t2, t3, t4, t5));
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
    /// Counts the amount of entities with a specific component
    /// </summary>
    /// <typeparam name="T">The type of the component</typeparam>
    /// <returns>The amount of entities with the component</returns>
    public int CountEntities<T>() where T : IComponent
    {
        return CountEntities(typeof(T));
    }

    /// <summary>
    /// Counts the amount of entities with a specific component
    /// </summary>
    /// <param name="t">The type of the component</param>
    /// <returns>The amount of entities with the component</returns>
    public int CountEntities(Type t)
    {
        lock (lockObject)
        {
            var counter = 0;

            foreach (var entity in entities)
            {
                if (entity.alive == false)
                {
                    continue;
                }

                var componentIndex = ComponentIndices(t).FirstOrDefault(x => entity.components.Contains(x), -1);

                if (componentIndex < 0)
                {
                    continue;
                }

                counter++;
            }

            return counter;
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
    /// <typeparam name="T"></typeparam>
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
    /// Iterates through each entity in the world
    /// </summary>
    /// <param name="callback">A callback to handle the entity</param>
    internal void Iterate(Action<Entity> callback)
    {
        lock (lockObject)
        {
            //TODO: Figure out a way without allocations. We can have layers of iterations mixed in due to callbacks.
            var allEntities = entities.ToArray();

            foreach (var entity in allEntities)
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

            foreach (var index in cache)
            {
                if(entityInfo.alive == false)
                {
                    break;
                }

                if(entityInfo.removedComponents.Contains(index))
                {
                    continue;
                }

                var component = componentsRepository[index].components[entityInfo.localID];

                callback(ref component);

                if (entityInfo.removedComponents.Contains(index))
                {
                    continue;
                }

                componentsRepository[index].components[entityInfo.localID] = component;
            }
        }
    }
}
