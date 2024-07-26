using System;
using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Contains and manages entities
/// </summary>
public partial class World
{
    public delegate void IterateComponentCallback(ref IComponent component);

    public delegate void OnComponentChangedCallback(World world, Entity entity, ref IComponent component);

    public delegate void CallableComponentCallback(Entity entity, CallbackComponent component);

    /// <summary>
    /// Contains data on an entity
    /// </summary>
    internal class EntityInfo
    {
        /// <summary>
        /// The entity's ID
        /// </summary>
        public int ID;

        /// <summary>
        /// The entity's generation
        /// </summary>
        public int generation;

        /// <summary>
        /// Whether this entity is alive. If it's not, queries on it will fail.
        /// </summary>
        public bool alive;

        /// <summary>
        /// Whether this entity is enabled. If it's not, it will not be updated or rendered.
        /// </summary>
        public bool enabled;

        /// <summary>
        /// List of components for the entity
        /// </summary>
        public List<IComponent> components = new();

        /// <summary>
        /// List of component indices for the entity
        /// </summary>
        public List<int> componentIndices = new();

        /// <summary>
        /// The components that were just removed for the entity
        /// </summary>
        public HashSet<int> removedComponents = new();

        /// <summary>
        /// The entity's name
        /// </summary>
        public string name;

        /// <summary>
        /// The entity's layer. Defaults to the first layer.
        /// </summary>
        public uint layer;

        /// <summary>
        /// The entity's prefab (if any)
        /// </summary>
        public string prefabGUID;

        /// <summary>
        /// The entity's local ID in the prefab (if any)
        /// </summary>
        public int prefabLocalID;

        public bool TryGetComponentIndex(int index, out int value)
        {
            value = componentIndices.IndexOf(index);

            return value >= 0;
        }

        public Entity ToEntity()
        {
            return new()
            {
                Identifier = new()
                {
                    generation = generation,
                    ID = ID,
                }
            };
        }
    }

    /// <summary>
    /// Contains camera entity information
    /// Used by the SortedCameras property
    /// </summary>
    public class CameraInfo
    {
        public Entity entity;
        public Camera camera;
        public Transform transform;
    }

    public static World Current { get; internal set; } = new();

    private readonly object lockObject = new();
    private static readonly object globalLockObject = new();
    private readonly List<EntityInfo> entities = new();
    private readonly Dictionary<int, Type> componentsRepository = new();
    private readonly HashSet<int> callableComponentIndices = new();
    private readonly List<Entity> destroyedEntities = new();

    internal void StartFrame()
    {
        lock(lockObject)
        {
            foreach(var info in entities)
            {
                if(info.removedComponents.Count > 0)
                {
                    foreach (var index in info.removedComponents)
                    {
                        if(info.TryGetComponentIndex(index, out var i))
                        {
                            info.components.RemoveAt(i);
                            info.componentIndices.RemoveAt(i);
                        }
                    }

                    info.removedComponents.Clear();
                }
            }

            void Destroy(Entity e)
            {
                if (TryGetEntity(e, out var info) == false)
                {
                    return;
                }

                lock (lockObject)
                {
                    var transform = GetComponent<Transform>(e);

                    transform?.SetParent(null);

                    info.components.Clear();
                    info.componentIndices.Clear();
                    info.alive = false;

                    while (transform.ChildCount > 0)
                    {
                        var child = transform.GetChild(0);

                        Destroy(child.entity);
                    }
                }
            }

            foreach (var e in destroyedEntities)
            {
                Destroy(e);
            }

            destroyedEntities.Clear();
        }
    }

    private static readonly Dictionary<Type, List<OnComponentChangedCallback>> componentAddedCallbacks = new();
    private static readonly Dictionary<Type, List<OnComponentChangedCallback>> componentRemovedCallbacks = new();
}
