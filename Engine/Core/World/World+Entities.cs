using System.Collections.Generic;

namespace Staple
{
    public partial class World
    {
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
                    name = "Entity",
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

        public string GetEntityName(Entity entity)
        {
            if (entity.ID < 0 || entity.ID >= entities.Count || entities[entity.ID].alive == false || entities[entity.ID].generation != entity.generation)
            {
                return default;
            }

            return entities[entity.ID].name;
        }

        public void SetEntityName(Entity entity, string name)
        {
            if (entity.ID < 0 || entity.ID >= entities.Count || entities[entity.ID].alive == false || entities[entity.ID].generation != entity.generation)
            {
                return;
            }

            var t = entities[entity.ID];

            t.name = name;

            entities[entity.ID] = t;
        }
    }
}
