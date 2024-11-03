using Staple.Internal;
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

    /// <summary>
    /// Listener to calculate the sorted cameras in a scene
    /// </summary>
    public class SortedCamerasHolder : IWorldChangeReceiver
    {
        public CameraInfo[] sortedCameras = [];

        public void WorldChanged()
        {
            var pieces = new List<CameraInfo>();
            var cameras = Scene.Query<Camera, Transform>(false);

            foreach ((Entity e, Camera c, Transform t) in cameras)
            {
                pieces.Add(new()
                {
                    camera = c,
                    entity = e,
                    transform = t,
                });
            }

            pieces.Sort((x, y) => x.camera.depth.CompareTo(y.camera.depth));

            sortedCameras = pieces.ToArray();
        }
    }

    public static World Current { get; internal set; } = new();

    private readonly object lockObject = new();
    private static readonly object globalLockObject = new();

    private readonly List<EntityInfo> entities = [];
    private readonly Dictionary<int, HashSet<int>> componentCompatibilityCache = [];
    private readonly Dictionary<int, string> componentNameHashes = [];
    private readonly HashSet<int> callableComponentTypes = [];
    private readonly List<Entity> destroyedEntities = [];
    private readonly HashSet<(Entity, int)> removedComponents = [];
    private SceneQuery<CallbackComponent> callableComponents;
    private SceneQuery<Camera, Transform> cameras;

    internal SortedCamerasHolder sortedCamerasHolder;

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

            receiver.WorldChanged();
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
            if (cameras == null)
            {
                cameras = new();
            }

            if (sortedCamerasHolder == null)
            {
                sortedCamerasHolder = new();

                AddChangeReceiver(sortedCamerasHolder);
            }

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

                if (TryGetEntity(e, out var info) == false)
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

                        Destroy(child.entity);
                    }

                    info.components.Clear();

                    removedComponents.RemoveWhere(x => x.Item1 == e);

                    info.alive = false;
                    info.prefabGUID = null;
                    info.prefabLocalID = 0;
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
