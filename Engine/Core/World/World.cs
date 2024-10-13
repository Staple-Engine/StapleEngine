using Staple.Internal;
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
        /// Whether this entity is enabled in the scene hierarchy. If a parent is disabled, this will be false, even if it's normally enabled.
        /// </summary>
        public bool enabledInHierarchy;

        /// <summary>
        /// List of components for the entity
        /// </summary>
        public Dictionary<int, IComponent> components = [];

        /// <summary>
        /// The components that were just removed for the entity
        /// </summary>
        public HashSet<int> removedComponents = [];

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
    private readonly List<EntityInfo> entities = [];
    private readonly Dictionary<int, HashSet<int>> componentCompatibilityCache = [];
    private readonly Dictionary<int, string> componentNameHashes = [];
    private readonly HashSet<int> callableComponentTypes = [];
    private readonly List<Entity> destroyedEntities = [];
    private SceneQuery<CallbackComponent> callableComponents;
    private EntityInfo[] cachedEntityList = [];
    private bool needsEmitWorldChange = false;

    private static readonly ObservableBox worldChangeReceivers = new();
    private static readonly ObservableBox sceneQueries = new();
    private static readonly Dictionary<int, List<OnComponentChangedCallback>> componentAddedCallbacks = [];
    private static readonly Dictionary<int, List<OnComponentChangedCallback>> componentRemovedCallbacks = [];

    internal static void AddSceneQuery(ISceneQuery receiver)
    {
        if (receiver == null)
        {
            return;
        }

        lock (globalLockObject)
        {
            sceneQueries.AddObserver(receiver);
        }
    }

    internal static void AddChangeReceiver(IWorldChangeReceiver receiver)
    {
        if(receiver == null)
        {
            return;
        }

        lock(globalLockObject)
        {
            worldChangeReceivers.AddObserver(receiver);
        }
    }

    internal static void EmitWorldChangedEvent()
    {
        sceneQueries.Emit((t) => ((ISceneQuery)t).WorldChanged());

        worldChangeReceivers.Emit((t) => ((IWorldChangeReceiver)t).WorldChanged());
    }

    internal void RequestWorldUpdate()
    {
        lock (lockObject)
        {
            needsEmitWorldChange = true;
        }
    }

    internal void StartFrame()
    {
        lock(lockObject)
        {
            foreach(var info in entities)
            {
                if(info.removedComponents.Count > 0)
                {
                    foreach (var componentTypeName in info.removedComponents)
                    {
                        info.components.Remove(componentTypeName.GetHashCode());
                    }

                    info.removedComponents.Clear();

                    needsEmitWorldChange = true;
                }
            }

            void Destroy(Entity e)
            {
                needsEmitWorldChange = true;

                if (TryGetEntity(e, out var info) == false)
                {
                    return;
                }

                lock (lockObject)
                {
                    var transform = GetComponent<Transform>(e);

                    transform?.SetParent(null);

                    info.components.Clear();
                    info.removedComponents.Clear();

                    info.alive = false;
                    info.prefabGUID = null;
                    info.prefabLocalID = 0;

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

            if(needsEmitWorldChange)
            {
                cachedEntityList = entities.ToArray();

                needsEmitWorldChange = false;

                EmitWorldChangedEvent();
            }
        }
    }
}
