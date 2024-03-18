using System;

namespace Staple;

public partial class World
{
    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <param name="callback">The callback when handling an entity</param>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    public void ForEach<T>(ForEachCallback<T> callback, bool includeDisabled) where T : IComponent
    {
        if (ComponentIndex(typeof(T)) == -1)
        {
            return;
        }

        lock (lockObject)
        {
            collectionModified = false;

            foreach (var entity in entities)
            {
                entity.componentsModified = false;

                if (entity.alive == false ||
                    (includeDisabled == false && entity.enabled == false))
                {
                    continue;
                }

                var index = ComponentIndex(entity, typeof(T));

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

                    callback(e, ref t);

                    componentsRepository[index].components[entity.localID] = t;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }

                if (collectionModified)
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <param name="callback">The callback when handling an entity</param>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    public void ForEach<T, T2>(ForEachCallback<T, T2> callback, bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
    {
        if (ComponentIndex(typeof(T)) == -1 ||
            ComponentIndex(typeof(T2)) == -1)
        {
            return;
        }

        lock (lockObject)
        {
            collectionModified = false;

            foreach (var entity in entities)
            {
                entity.componentsModified = false;

                if (entity.alive == false ||
                    (includeDisabled == false && entity.enabled == false))
                {
                    continue;
                }

                var index = ComponentIndex(entity, typeof(T));
                var index2 = ComponentIndex(entity, typeof(T2));

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

                    callback(e, ref t, ref t2);

                    componentsRepository[index].components[entity.localID] = t;
                    componentsRepository[index2].components[entity.localID] = t2;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }

                if (collectionModified)
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <param name="callback">The callback when handling an entity</param>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    public void ForEach<T, T2, T3>(ForEachCallback<T, T2, T3> callback, bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
    {
        if (ComponentIndex(typeof(T)) == -1 ||
            ComponentIndex(typeof(T2)) == -1 ||
            ComponentIndex(typeof(T3)) == -1)
        {
            return;
        }

        lock (lockObject)
        {
            collectionModified = false;

            foreach (var entity in entities)
            {
                entity.componentsModified = false;

                if (entity.alive == false ||
                    (includeDisabled == false && entity.enabled == false))
                {
                    continue;
                }

                var index = ComponentIndex(entity, typeof(T));
                var index2 = ComponentIndex(entity, typeof(T2));
                var index3 = ComponentIndex(entity, typeof(T3));

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

                    callback(e, ref t, ref t2, ref t3);

                    componentsRepository[index].components[entity.localID] = t;
                    componentsRepository[index2].components[entity.localID] = t2;
                    componentsRepository[index3].components[entity.localID] = t3;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }

                if (collectionModified)
                {
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Iterates through entities, querying for components.
    /// </summary>
    /// <typeparam name="T">The type of the first component</typeparam>
    /// <typeparam name="T2">The type of the second component</typeparam>
    /// <typeparam name="T3">The type of the third component</typeparam>
    /// <typeparam name="T4">The type of the fourth component</typeparam>
    /// <param name="callback">The callback when handling an entity</param>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    public void ForEach<T, T2, T3, T4>(ForEachCallback<T, T2, T3, T4> callback, bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
    {
        if (ComponentIndex(typeof(T)) == -1 ||
            ComponentIndex(typeof(T2)) == -1 ||
            ComponentIndex(typeof(T3)) == -1 ||
            ComponentIndex(typeof(T4)) == -1)
        {
            return;
        }

        lock (lockObject)
        {
            collectionModified = false;

            foreach (var entity in entities)
            {
                entity.componentsModified = false;

                if (entity.alive == false ||
                    (includeDisabled == false && entity.enabled == false))
                {
                    continue;
                }

                var index = ComponentIndex(entity, typeof(T));
                var index2 = ComponentIndex(entity, typeof(T2));
                var index3 = ComponentIndex(entity, typeof(T3));
                var index4 = ComponentIndex(entity, typeof(T4));

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

                    callback(e, ref t, ref t2, ref t3, ref t4);

                    componentsRepository[index].components[entity.localID] = t;
                    componentsRepository[index2].components[entity.localID] = t2;
                    componentsRepository[index3].components[entity.localID] = t3;
                    componentsRepository[index4].components[entity.localID] = t4;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }

                if (collectionModified)
                {
                    return;
                }
            }
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
    /// <param name="callback">The callback when handling an entity</param>
    /// <param name="includeDisabled">Whether to include disabled entities</param>
    public void ForEach<T, T2, T3, T4, T5>(ForEachCallback<T, T2, T3, T4, T5> callback, bool includeDisabled)
        where T : IComponent
        where T2 : IComponent
        where T3 : IComponent
        where T4 : IComponent
        where T5 : IComponent
    {
        if (ComponentIndex(typeof(T)) == -1 ||
            ComponentIndex(typeof(T2)) == -1 ||
            ComponentIndex(typeof(T3)) == -1 ||
            ComponentIndex(typeof(T4)) == -1 ||
            ComponentIndex(typeof(T5)) == -1)
        {
            return;
        }

        lock (lockObject)
        {
            collectionModified = false;

            foreach (var entity in entities)
            {
                entity.componentsModified = false;

                if (entity.alive == false ||
                    (includeDisabled == false && entity.enabled == false))
                {
                    continue;
                }

                var index = ComponentIndex(entity, typeof(T));
                var index2 = ComponentIndex(entity, typeof(T2));
                var index3 = ComponentIndex(entity, typeof(T3));
                var index4 = ComponentIndex(entity, typeof(T4));
                var index5 = ComponentIndex(entity, typeof(T5));

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

                    callback(e, ref t, ref t2, ref t3, ref t4, ref t5);

                    componentsRepository[index].components[entity.localID] = t;
                    componentsRepository[index2].components[entity.localID] = t2;
                    componentsRepository[index3].components[entity.localID] = t3;
                    componentsRepository[index4].components[entity.localID] = t4;
                    componentsRepository[index5].components[entity.localID] = t5;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }

                if (collectionModified)
                {
                    return;
                }
            }
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
            var componentIndex = ComponentIndex(t);

            if (componentIndex == -1)
            {
                return 0;
            }

            var counter = 0;

            foreach (var entity in entities)
            {
                if (entity.alive == false ||
                    entity.components.Contains(componentIndex) == false)
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
            collectionModified = false;

            foreach (var entity in entities)
            {
                entity.componentsModified = false;

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

                if (collectionModified)
                {
                    return;
                }
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
            entityInfo.componentsModified = false;

            collectionModified = false;

            foreach (var index in entityInfo.components)
            {
                var component = componentsRepository[index].components[entityInfo.localID];

                callback(ref component);

                if(entityInfo.componentsModified)
                {
                    return;
                }

                componentsRepository[index].components[entityInfo.localID] = component;
            }

            if (collectionModified)
            {
                return;
            }
        }
    }
}
