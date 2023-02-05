using System;
using System.Collections.Generic;

namespace Staple
{
    public class World
    {
        private struct EntityInfo
        {
            public int ID;
            public int generation;
            public bool alive;
            public List<int> components;
        }

        private class ComponentInfo
        {
            public Type type;
            public List<IComponent> components = new List<IComponent>();

            public bool AddComponent()
            {
                try
                {
                    var t = (IComponent)Activator.CreateInstance(type);

                    if (t != null)
                    {
                        components.Add(t);

                        return true;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to add component {type.FullName}: {e}");
                }

                return false;
            }

            public bool Create(out IComponent component)
            {
                try
                {
                    var t = (IComponent)Activator.CreateInstance(type);

                    if (t != null)
                    {
                        component = t;

                        return true;
                    }
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to create component {type.FullName}: {e}");
                }

                component = default;

                return false;
            }
        }

        private object lockObject = new object();
        private List<EntityInfo> entities = new List<EntityInfo>();
        private Dictionary<int, ComponentInfo> componentsRepository = new Dictionary<int, ComponentInfo>();

        public delegate void ForEachCallback<T>(Entity entity, ref T a) where T: IComponent;

        public delegate void ForEachCallback<T, T2>(Entity entity, ref T a, ref T2 b)
            where T : IComponent
            where T2 : IComponent;

        public delegate void ForEachCallback<T, T2, T3>(Entity entity, ref T a, ref T2 b, ref T3 c)
            where T : IComponent
            where T2 : IComponent
            where T3 : IComponent;

        public delegate void ForEachCallback<T, T2, T3, T4>(Entity entity, ref T a, ref T2 b, ref T3 c, ref T4 d)
            where T : IComponent
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent;

        public delegate void ForEachCallback<T, T2, T3, T4, T5>(Entity entity, ref T a, ref T2 b, ref T3 c, ref T4 d, ref T5 e)
            where T : IComponent
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent
            where T5: IComponent;

        public delegate void IterateComponentCallback(ref IComponent component);

        internal int ComponentIndex(Type t)
        {
            lock(lockObject)
            {
                foreach (var pair in componentsRepository)
                {
                    if(pair.Value.type == t)
                    {
                        return pair.Key;
                    }
                }

                return -1;
            }
        }

        public Entity CreateEntity()
        {
            lock (lockObject)
            {
                for (var i = 0; i < entities.Count; i++)
                {
                    if (entities[i].alive == false)
                    {
                        var other = entities[i];

                        other.alive = true;

                        entities[i] = other;

                        return new Entity()
                        {
                            ID = other.ID,
                            generation = other.generation,
                        };
                    }
                }

                var newEntity = new EntityInfo()
                {
                    ID = entities.Count,
                    alive = true,
                    components = new List<int>(),
                };

                entities.Add(newEntity);

                foreach (var pair in componentsRepository)
                {
                    pair.Value.AddComponent();
                }

                return new Entity()
                {
                    ID = newEntity.ID,
                    generation = newEntity.generation,
                };
            }
        }

        public void DestroyEntity(Entity entity)
        {
            lock (lockObject)
            {
                if (entity.ID >= 0 && entity.ID < entities.Count)
                {
                    var e = entities[entity.ID];

                    if (e.generation != entity.generation)
                    {
                        return;
                    }

                    e.components.Clear();

                    e.generation++;

                    entities[e.ID] = e;
                }
            }
        }

        public T AddComponent<T>(Entity entity) where T : IComponent
        {
            return (T)AddComponent(entity, typeof(T));
        }

        public IComponent AddComponent(Entity entity, Type t)
        {
            lock (lockObject)
            {
                if (entity.ID < 0 || entity.ID >= entities.Count || entities[entity.ID].alive == false || entities[entity.ID].generation != entity.generation)
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
                    if(info.Create(out var component) == false)
                    {
                        return default;
                    }

                    info.components[entity.ID].Invoke("OnDestroy");

                    info.components[entity.ID] = component;
                }

                return info.components[entity.ID];
            }
        }

        public void RemoveComponent<T>(Entity entity) where T : IComponent
        {
            RemoveComponent(entity, typeof(T));
        }

        public void RemoveComponent(Entity entity, Type t)
        {
            lock (lockObject)
            {
                if (entity.ID < 0 || entity.ID >= entities.Count || entities[entity.ID].alive == false || entities[entity.ID].generation != entity.generation)
                {
                    return;
                }

                var componentIndex = ComponentIndex(t);

                if(componentIndex >= 0)
                {
                    entities[entity.ID].components.Remove(componentIndex);
                }
            }
        }

        public IComponent GetComponent(Entity entity, Type t)
        {
            if(typeof(IComponent).IsAssignableFrom(t) == false)
            {
                return default;
            }

            lock(lockObject)
            {
                if (entity.ID < 0 || entity.ID >= entities.Count || entities[entity.ID].alive == false || entities[entity.ID].generation != entity.generation)
                {
                    return default;
                }

                var componentIndex = ComponentIndex(t);

                if (componentsRepository.TryGetValue(componentIndex, out var info) == false)
                {
                    return default;
                }

                return info.components[entity.ID];
            }
        }

        public T GetComponent<T>(Entity entity) where T: IComponent
        {
            return (T)GetComponent(entity, typeof(T));
        }

        public void ForEach<T>(ForEachCallback<T> callback) where T: IComponent
        {
            var index = ComponentIndex(typeof(T));

            if(index < 0)
            {
                return;
            }

            foreach(var entity in entities)
            {
                if(entity.alive == false || entity.components.Contains(index) == false)
                {
                    continue;
                }

                T t = (T)componentsRepository[index].components[entity.ID];

                try
                {
                    callback(new Entity()
                    {
                        ID = entity.ID,
                        generation = entity.generation,
                    }, ref t);

                    componentsRepository[index].components[entity.ID] = t;
                }
                catch(Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }
        }

        public void ForEach<T, T2>(ForEachCallback<T, T2> callback)
            where T : IComponent
            where T2: IComponent
        {
            var index = ComponentIndex(typeof(T));
            var index2 = ComponentIndex(typeof(T2));

            if (index < 0 ||
                index2 < 0)
            {
                return;
            }

            foreach (var entity in entities)
            {
                if (entity.alive == false ||
                    entity.components.Contains(index) == false ||
                    entity.components.Contains(index2) == false)
                {
                    continue;
                }

                T t = (T)componentsRepository[index].components[entity.ID];
                T2 t2 = (T2)componentsRepository[index2].components[entity.ID];

                try
                {
                    callback(new Entity()
                    {
                        ID = entity.ID,
                        generation = entity.generation,
                    }, ref t, ref t2);

                    componentsRepository[index].components[entity.ID] = t;
                    componentsRepository[index2].components[entity.ID] = t2;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }
        }

        public void ForEach<T, T2, T3>(ForEachCallback<T, T2, T3> callback)
            where T : IComponent
            where T2 : IComponent
            where T3: IComponent
        {
            var index = ComponentIndex(typeof(T));
            var index2 = ComponentIndex(typeof(T2));
            var index3 = ComponentIndex(typeof(T3));

            if (index < 0 ||
                index2 < 0 ||
                index3 < 0)
            {
                return;
            }

            foreach (var entity in entities)
            {
                if (entity.alive == false ||
                    entity.components.Contains(index) == false ||
                    entity.components.Contains(index2) == false ||
                    entity.components.Contains(index3) == false)
                {
                    continue;
                }

                T t = (T)componentsRepository[index].components[entity.ID];
                T2 t2 = (T2)componentsRepository[index2].components[entity.ID];
                T3 t3 = (T3)componentsRepository[index3].components[entity.ID];

                try
                {
                    callback(new Entity()
                    {
                        ID = entity.ID,
                        generation = entity.generation,
                    }, ref t, ref t2, ref t3);

                    componentsRepository[index].components[entity.ID] = t;
                    componentsRepository[index2].components[entity.ID] = t2;
                    componentsRepository[index3].components[entity.ID] = t3;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }
        }

        public void ForEach<T, T2, T3, T4>(ForEachCallback<T, T2, T3, T4> callback)
            where T : IComponent
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent
        {
            var index = ComponentIndex(typeof(T));
            var index2 = ComponentIndex(typeof(T2));
            var index3 = ComponentIndex(typeof(T3));
            var index4 = ComponentIndex(typeof(T4));

            if (index < 0 ||
                index2 < 0 ||
                index3 < 0 ||
                index4 < 0)
            {
                return;
            }

            foreach (var entity in entities)
            {
                if (entity.alive == false ||
                    entity.components.Contains(index) == false ||
                    entity.components.Contains(index2) == false ||
                    entity.components.Contains(index3) == false ||
                    entity.components.Contains(index4) == false)
                {
                    continue;
                }

                T t = (T)componentsRepository[index].components[entity.ID];
                T2 t2 = (T2)componentsRepository[index2].components[entity.ID];
                T3 t3 = (T3)componentsRepository[index3].components[entity.ID];
                T4 t4 = (T4)componentsRepository[index4].components[entity.ID];

                try
                {
                    callback(new Entity()
                    {
                        ID = entity.ID,
                        generation = entity.generation,
                    }, ref t, ref t2, ref t3, ref t4);

                    componentsRepository[index].components[entity.ID] = t;
                    componentsRepository[index2].components[entity.ID] = t2;
                    componentsRepository[index3].components[entity.ID] = t3;
                    componentsRepository[index4].components[entity.ID] = t4;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }
        }

        public void ForEach<T, T2, T3, T4, T5>(ForEachCallback<T, T2, T3, T4, T5> callback)
            where T : IComponent
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent
            where T5 : IComponent
        {
            var index = ComponentIndex(typeof(T));
            var index2 = ComponentIndex(typeof(T2));
            var index3 = ComponentIndex(typeof(T3));
            var index4 = ComponentIndex(typeof(T4));
            var index5 = ComponentIndex(typeof(T5));

            if (index < 0 ||
                index2 < 0 ||
                index3 < 0 ||
                index4 < 0 ||
                index5 < 0)
            {
                return;
            }

            foreach (var entity in entities)
            {
                if (entity.alive == false ||
                    entity.components.Contains(index) == false ||
                    entity.components.Contains(index2) == false ||
                    entity.components.Contains(index3) == false ||
                    entity.components.Contains(index4) == false ||
                    entity.components.Contains(index5) == false)
                {
                    continue;
                }

                T t = (T)componentsRepository[index].components[entity.ID];
                T2 t2 = (T2)componentsRepository[index2].components[entity.ID];
                T3 t3 = (T3)componentsRepository[index3].components[entity.ID];
                T4 t4 = (T4)componentsRepository[index4].components[entity.ID];
                T5 t5 = (T5)componentsRepository[index5].components[entity.ID];

                try
                {
                    callback(new Entity()
                    {
                        ID = entity.ID,
                        generation = entity.generation,
                    }, ref t, ref t2, ref t3, ref t4, ref t5);

                    componentsRepository[index].components[entity.ID] = t;
                    componentsRepository[index2].components[entity.ID] = t2;
                    componentsRepository[index3].components[entity.ID] = t3;
                    componentsRepository[index4].components[entity.ID] = t4;
                    componentsRepository[index5].components[entity.ID] = t5;
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }
        }

        public int CountEntities<T>() where T: IComponent
        {
            return CountEntities(typeof(T));
        }

        public int CountEntities(Type t)
        {
            var componentIndex = ComponentIndex(t);

            if(componentIndex == -1)
            {
                return 0;
            }

            var counter = 0;

            foreach(var entity in entities)
            {
                if(entity.alive == false || entity.components.Contains(componentIndex) == false)
                {
                    continue;
                }

                counter++;
            }

            return counter;
        }

        public Entity FindEntity(int ID)
        {
            foreach(var entity in entities)
            {
                if(entity.ID == ID && entity.alive)
                {
                    return new Entity()
                    {
                        ID = entity.ID,
                        generation = entity.generation,
                    };
                }
            }

            return Entity.Empty;
        }

        internal void Iterate(Action<Entity> callback)
        {
            foreach(var entity in entities)
            {
                callback(new Entity()
                {
                    ID = entity.ID,
                    generation = entity.generation,
                });
            }
        }

        internal void IterateComponents(Entity entity, IterateComponentCallback callback)
        {
            foreach(var e in entities)
            {
                if(e.ID != entity.ID || e.generation != entity.generation || e.alive == false)
                {
                    continue;
                }

                foreach(var index in e.components)
                {
                    var component = componentsRepository[index].components[e.ID];

                    callback(ref component);

                    componentsRepository[index].components[e.ID] = component;
                }

                break;
            }
        }

        public void UpdateComponent(Entity entity, IComponent component)
        {
            var componentIndex = ComponentIndex(component.GetType());

            if(componentIndex < 0)
            {
                return;
            }

            foreach(var e in entities)
            {
                if (e.ID != entity.ID || e.generation != entity.generation || e.alive == false)
                {
                    continue;
                }

                componentsRepository[componentIndex].components[e.ID] = component;
            }
        }
    }
}
