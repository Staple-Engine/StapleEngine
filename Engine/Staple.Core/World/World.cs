using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple;

/// <summary>
/// Contains and manages entities
/// </summary>
public partial class World
{
    public delegate void IterateComponentCallback(ref IComponent component);

    public delegate void OnComponentChangedCallback(World world, Entity entity, ref IComponent component);

    public delegate void CallableComponentCallback(Span<(Entity, CallbackComponent)> content);

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

        /// <summary>
        /// Whether the entity is hidden
        /// </summary>
        public EntityHierarchyVisibility hierarchyVisibility;

        /// <summary>
        /// List of emitted component add events
        /// </summary>
        public readonly HashSet<int> emittedAddComponents = [];

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

        public override string ToString()
        {
            return alive ? $"{name} ({ID}:{generation})" : "Invalid Entity (Dead)";
        }
    }

    /// <summary>
    /// Contains camera entity information
    /// Used by the SortedCameras property
    /// </summary>
    public struct CameraInfo
    {
        public Entity entity;
        public Camera camera;
        public Transform transform;
    }

    private class WorldChangeBox : ObservableBox
    {
        public override void EmitAction(object observer)
        {
            if(observer is not IWorldChangeReceiver receiver)
            {
                return;
            }

            receiver.WorldChanged();
        }
    }

    private class SceneQueryBox : ObservableBox
    {
        public override void EmitAction(object observer)
        {
            if(observer is not ISceneQuery query)
            {
                return;
            }

            query.WorldChanged();
        }
    }

    public static World Current { get; internal set; } = new();

    private int entityCount;

    public int EntityCount
    {
        get
        {
            lock (lockObject)
            {
                return entityCount;
            }
        }
    }

    /// <summary>
    /// Gets all available cameras sorted by depth
    /// </summary>
    public CameraInfo[] SortedCameras => sortedCameras;

    /// <summary>
    /// Gets all entities with a valid transform that don't have a parent
    /// </summary>
    public (Entity, Transform)[] RootEntities => rootEntities;

    private readonly Lock lockObject = new();
    private static readonly Lock globalLockObject = new();

    private EntityInfo[] entities = [];
    private readonly Dictionary<int, HashSet<int>> componentCompatibilityCache = [];
    private readonly Dictionary<int, string> componentNameHashes = [];
    private readonly HashSet<int> callableComponentTypes = [];
    private readonly List<Entity> destroyedEntities = [];
    private readonly HashSet<(Entity, int)> removedComponents = [];
    private SceneQuery<CallbackComponent> callableComponents;
    private SceneQuery<Camera, Transform> cameras;

    private readonly List<CameraInfo> sortedCamerasBacking = [];
    private readonly List<(Entity, Transform)> rootEntitiesBacking = [];

    internal CameraInfo[] sortedCameras = [];
    internal (Entity, Transform)[] rootEntities = [];

    private EntityInfo[] cachedEntityList = [];
    private bool needsEmitWorldChange = false;
    private readonly SortedSet<int> deadEntities = [];

    private static readonly WorldChangeBox worldChangeReceivers = new();
    private static readonly SceneQueryBox sceneQueries = new();
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

            receiver.WorldChanged();
        }
    }

    internal static void RemoveSceneQuery(ISceneQuery receiver)
    {
        if (receiver == null)
        {
            return;
        }

        lock (globalLockObject)
        {
            sceneQueries.RemoveObserver(receiver);
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

            receiver.WorldChanged();
        }
    }

    internal static void RemoveChangeReceiver(IWorldChangeReceiver receiver)
    {
        if (receiver == null)
        {
            return;
        }

        lock (globalLockObject)
        {
            worldChangeReceivers.RemoveObserver(receiver);
        }
    }

    internal static void EmitWorldChangedEvent()
    {
        if(Current != null)
        {
            if(Current.cachedEntityList.Length != Current.entities.Length)
            {
                Array.Resize(ref Current.cachedEntityList, Current.entities.Length);

                for(var i = 0; i < Current.entities.Length; i++)
                {
                    Current.cachedEntityList[i] = Current.entities[i];
                }
            }

            {
                Current.sortedCamerasBacking.Clear();
                var cameras = Scene.Query<Camera, Transform>(false);

                foreach ((Entity e, Camera c, Transform t) in cameras)
                {
                    Current.sortedCamerasBacking.Add(new()
                    {
                        camera = c,
                        entity = e,
                        transform = t,
                    });
                }

                Current.sortedCamerasBacking.Sort((x, y) => x.camera.depth.CompareTo(y.camera.depth));

                if(Current.sortedCameras.Length != Current.sortedCamerasBacking.Count)
                {
                    Array.Resize(ref Current.sortedCameras, Current.sortedCamerasBacking.Count);
                }

                for(var i = 0; i < Current.sortedCameras.Length; i++)
                {
                    Current.sortedCameras[i] = Current.sortedCamerasBacking[i];
                }
            }

            {
                var transforms = Scene.Query<Transform>(true);

                Current.rootEntitiesBacking.Clear();

                foreach(var (e, t) in transforms)
                {
                    if(t.Parent == null)
                    {
                        Current.rootEntitiesBacking.Add((e, t));
                    }
                }

                if(Current.rootEntities.Length != Current.rootEntitiesBacking.Count)
                {
                    Array.Resize(ref Current.rootEntities, Current.rootEntitiesBacking.Count);
                }

                for(var i = 0; i < Current.rootEntities.Length; i++)
                {
                    Current.rootEntities[i] = Current.rootEntitiesBacking[i];
                }
            }
        }

        sceneQueries.Emit();

        worldChangeReceivers.Emit();
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
            cameras ??= new();

            if (removedComponents.Count > 0)
            {
                foreach (var item in removedComponents)
                {
                    if(TryGetEntity(item.Item1, out var entityInfo))
                    {
                        entityInfo.components.Remove(item.Item2);

                        needsEmitWorldChange = true;
                    }
                }

                removedComponents.Clear();
            }

            void Destroy(Entity e)
            {
                needsEmitWorldChange = true;

                if (!TryGetEntity(e, out var info))
                {
                    return;
                }

                lock (lockObject)
                {
                    var transform = GetComponent<Transform>(e);

                    transform?.SetParent(null);

                    while ((transform?.ChildCount ?? 0) > 0)
                    {
                        var child = transform.GetChild(0);

                        child.SetParent(null);

                        Destroy(child.Entity);
                    }

                    foreach(var pair in info.components)
                    {
                        RemoveComponent(e, pair.Value.GetType());
                    }

                    info.components.Clear();

                    removedComponents.RemoveWhere(x => x.Item1 == e);

                    info.alive = false;
                    info.prefabGUID = null;
                    info.prefabLocalID = 0;

                    entityCount--;

                    deadEntities.Add(e.Identifier.ID - 1);
                }
            }

            foreach (var e in destroyedEntities)
            {
                Destroy(e);
            }

            destroyedEntities.Clear();

            if(needsEmitWorldChange)
            {
                needsEmitWorldChange = false;

                EmitWorldChangedEvent();
            }
        }
    }

    internal void Dispose()
    {
        lock(lockObject)
        {
            needsEmitWorldChange = true;

            foreach (var entity in entities)
            {
                var transform = GetComponent<Transform>(entity.ToEntity());

                transform?.Entity = default;

                foreach(var pair in entity.components)
                {
                    RemoveComponent(entity.ToEntity(), pair.Value.GetType());
                }

                entity.components.Clear();
            }

            entities = [];
            destroyedEntities.Clear();
            removedComponents.Clear();

            cachedEntityList = [];

            if (needsEmitWorldChange)
            {
                needsEmitWorldChange = false;

                EmitWorldChangedEvent();
            }
        }
    }
}
