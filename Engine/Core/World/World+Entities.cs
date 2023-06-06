using System.Collections.Generic;

namespace Staple
{
    public partial class World
    {
        private const string DefaultEntityName = "Entity";

        /// <summary>
        /// Checks whether an entity is valid
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>Whether it is valid</returns>
        public bool IsValidEntity(Entity entity)
        {
            lock(lockObject)
            {
                if (entity.ID >= 0 &&
                    entity.ID < entities.Count &&
                    entities[entity.ID].alive &&
                    entities[entity.ID].generation == entity.generation)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates an empty entity. It might be a recycled entity.
        /// </summary>
        /// <returns>The entity</returns>
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
                    name = DefaultEntityName,
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

        /// <summary>
        /// Destroys an entity.
        /// </summary>
        /// <param name="entity">The entity to destroy</param>
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
                    e.name = DefaultEntityName;

                    entities[e.ID] = e;
                }
            }
        }

        /// <summary>
        /// Gets an entity's name.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>The current name of the entity</returns>
        public string GetEntityName(Entity entity)
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

                return entities[entity.ID].name;
            }
        }

        /// <summary>
        /// Sets an entity's name.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="name">The new name</param>
        public void SetEntityName(Entity entity, string name)
        {
            lock(lockObject)
            {
                if (entity.ID < 0 ||
                    entity.ID >= entities.Count ||
                    entities[entity.ID].alive == false ||
                    entities[entity.ID].generation != entity.generation)
                {
                    return;
                }

                var t = entities[entity.ID];

                t.name = name;

                entities[entity.ID] = t;
            }
        }

        /// <summary>
        /// Gets an entity's layer.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <returns>The current layer of the entity</returns>
        public uint GetEntityLayer(Entity entity)
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

                return entities[entity.ID].layer;
            }
        }

        /// <summary>
        /// Sets an entity's layer.
        /// </summary>
        /// <param name="entity">The entity</param>
        /// <param name="layer">The new layter</param>
        public void SetEntityLayer(Entity entity, uint layer)
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

                var t = entities[entity.ID];

                t.layer = layer;

                entities[entity.ID] = t;
            }
        }
    }
}
