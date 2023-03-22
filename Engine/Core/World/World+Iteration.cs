using System;

namespace Staple
{
    public partial class World
    {
        public void ForEach<T>(ForEachCallback<T> callback) where T : IComponent
        {
            var index = ComponentIndex(typeof(T));

            if (index < 0)
            {
                return;
            }

            foreach (var entity in entities)
            {
                if (entity.alive == false || entity.components.Contains(index) == false)
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
                catch (Exception e)
                {
                    Log.Error($"Failed to process entity {entity.ID}: {e}");
                }
            }
        }

        public void ForEach<T, T2>(ForEachCallback<T, T2> callback)
            where T : IComponent
            where T2 : IComponent
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
            where T3 : IComponent
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

        public int CountEntities<T>() where T : IComponent
        {
            return CountEntities(typeof(T));
        }

        public int CountEntities(Type t)
        {
            var componentIndex = ComponentIndex(t);

            if (componentIndex == -1)
            {
                return 0;
            }

            var counter = 0;

            foreach (var entity in entities)
            {
                if (entity.alive == false || entity.components.Contains(componentIndex) == false)
                {
                    continue;
                }

                counter++;
            }

            return counter;
        }

        public Entity FindEntity(int ID)
        {
            foreach (var entity in entities)
            {
                if (entity.ID == ID && entity.alive)
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
            foreach (var entity in entities)
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
            foreach (var e in entities)
            {
                if (e.ID != entity.ID || e.generation != entity.generation || e.alive == false)
                {
                    continue;
                }

                foreach (var index in e.components)
                {
                    var component = componentsRepository[index].components[e.ID];

                    callback(ref component);

                    componentsRepository[index].components[e.ID] = component;
                }

                break;
            }
        }
    }
}
