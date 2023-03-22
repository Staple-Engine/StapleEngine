using System;
using System.Collections.Generic;

namespace Staple
{
    public partial class World
    {
        internal int ComponentIndex(Type t)
        {
            lock (lockObject)
            {
                foreach (var pair in componentsRepository)
                {
                    if (pair.Value.type == t)
                    {
                        return pair.Key;
                    }
                }

                return -1;
            }
        }

        public CameraInfo[] SortedCameras
        {
            get
            {
                var pieces = new List<CameraInfo>();

                ForEach((Entity entity, ref Camera camera, ref Transform transform) =>
                {
                    pieces.Add(new CameraInfo()
                    {
                        entity = entity,
                        camera = camera,
                        transform = transform
                    });
                });

                pieces.Sort((x, y) => x.camera.depth.CompareTo(y.camera.depth));

                return pieces.ToArray();
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
                    if (info.Create(out var component) == false)
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

                if (componentIndex >= 0)
                {
                    entities[entity.ID].components.Remove(componentIndex);
                }
            }
        }

        public IComponent GetComponent(Entity entity, Type t)
        {
            if (typeof(IComponent).IsAssignableFrom(t) == false)
            {
                return default;
            }

            lock (lockObject)
            {
                if (entity.ID < 0 || entity.ID >= entities.Count || entities[entity.ID].alive == false || entities[entity.ID].generation != entity.generation)
                {
                    return default;
                }

                var componentIndex = ComponentIndex(t);

                if (entities[entity.ID].components.Contains(componentIndex) == false ||
                    componentsRepository.TryGetValue(componentIndex, out var info) == false)
                {
                    return default;
                }

                return info.components[entity.ID];
            }
        }

        public T GetComponent<T>(Entity entity) where T : IComponent
        {
            return (T)GetComponent(entity, typeof(T));
        }

        public void UpdateComponent(Entity entity, IComponent component)
        {
            var componentIndex = ComponentIndex(component.GetType());

            if (componentIndex < 0)
            {
                return;
            }

            foreach (var e in entities)
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
