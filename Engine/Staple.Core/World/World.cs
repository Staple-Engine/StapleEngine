using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Staple;

/// <summary>
/// Contains and manages entities
/// </summary>
public partial class World
{
    internal static readonly int TransformComponentID = typeof(Transform).FullName.GetHashCode();

    internal static readonly string LogTag = "World";

    public delegate void IterateComponentCallback(Component component);

    public delegate void OnComponentChangedCallback(World world, Component component);

    public delegate void LifecycleComponentCallback(Span<LifecycleComponent> content);

    public delegate void SingleLifecycleComponentCallback(LifecycleComponent component);

    /// <summary>
    /// Contains data about a component
    /// </summary>
    internal class ComponentHolder(int hash, Component component)
    {
        /// <summary>
        /// The hash of the component
        /// </summary>
        public int hash = hash;
        /// <summary>
        /// The component instance
        /// </summary>
        public Component component = component;
        /// <summary>
        /// The <see cref="VersionableComponent"/> of this component, if any
        /// </summary>
        public VersionableComponent versionable = component as VersionableComponent;
    }

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
        public Dictionary<int, ComponentHolder> components = [];

        /// <summary>
        /// List of components as an array
        /// </summary>
        public ExpandableContainer<ComponentHolder> componentsArray = new();

        /// <summary>
        /// List of versionable components as an array
        /// </summary>
        public ExpandableContainer<ComponentHolder> versionableComponentsArray = new();

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
        /// The entity's transform
        /// </summary>
        public Transform transform;

        /// <summary>
        /// List of emitted component add events
        /// </summary>
        public readonly HashSet<int> emittedAddComponents = [];

        /// <summary>
        /// List of emitted component change events
        /// </summary>
        public readonly HashSet<int> emittedChangeComponents = [];

        /// <summary>
        /// Cached entity value
        /// </summary>
        public Entity entityValue;

        /// <summary>
        /// Updates the <see cref="EntityInfo"/>'s <see cref="entityValue"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UpdateEntityValue()
        {
            entityValue.Identifier.ID = ID;
            entityValue.Identifier.generation = generation;
        }

        public override string ToString()
        {
            return alive ? $"{name} ({ID}:{generation})" : "Invalid Entity (Dead)";
        }
    }

    private class WorldChangeBox : ObservableBox
    {
        protected override void EmitAction(object observer)
        {
            if(observer is not IWorldChangeReceiver receiver)
            {
                return;
            }

            receiver.WorldChanged(Current);
        }

        public void EmitWorldReplacedEvent()
        {
            foreach(var observer in Observers)
            {
                if (observer is not IWorldChangeReceiver receiver)
                {
                    return;
                }

                receiver.WorldReplaced(Current);
            }
        }
    }

    private class SceneQueryBox : ObservableBox
    {
        protected override void EmitAction(object observer)
        {
            if(observer is not ISceneQuery query)
            {
                return;
            }

            query.WorldChanged(Current);
        }
    }

    private class ComponentChangedBox : ObservableBox
    {
        protected override void EmitAction(object observer)
        {
        }

        public void EmitChangedComponent(VersionableComponent c)
        {
            foreach (var observer in Observers)
            {
                if (observer is not IComponentChangeReceiver receiver)
                {
                    continue;
                }

                receiver.MarkComponentChanged(c);
            }
        }
    }

    /// <summary>
    /// The current world instance, if any
    /// </summary>
    public static World Current { get; internal set; } = new();

    /// <summary>
    /// The total amount of live entities in the world
    /// </summary>
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
    public Span<Camera> SortedCameras => sortedCameras.Contents;

    /// <summary>
    /// Gets all entities with a valid transform that don't have a parent
    /// </summary>
    public Span<Transform> RootEntities => rootEntities.Contents;

    /// <summary>
    /// World-specific thread lock
    /// </summary>
    private readonly Lock lockObject = new();

    /// <summary>
    /// Global thread lock
    /// </summary>
    private static readonly Lock globalLockObject = new();

    /// <summary>
    /// Keeps the inheritance info for a component (type fullname hash to set of type fullname hashes)
    /// </summary>
    private readonly Dictionary<int, HashSet<int>> componentCompatibilityCache = [];

    /// <summary>
    /// Maps component type fullname hashes to component names
    /// </summary>
    private readonly Dictionary<int, string> componentNameHashes = [];

    /// <summary>
    /// <see cref="LifecycleComponent"/> hashes
    /// </summary>
    private readonly HashSet<int> lifecycleComponentTypes = [];

    /// <summary>
    /// List of pending destroyed entities
    /// </summary>
    private readonly List<Entity> destroyedEntities = [];

    /// <summary>
    /// List of removed components per entity
    /// </summary>
    private readonly HashSet<(Entity, int)> removedComponents = [];

    /// <summary>
    /// All lifecycle components
    /// </summary>
    private SceneQuery<LifecycleComponent> lifecycleComponents;

    /// <summary>
    /// All cameras in the scene
    /// </summary>
    private SceneQuery<Camera> cameras;

    /// <summary>
    /// All entities
    /// </summary>
    internal readonly ExpandableContainer<EntityInfo> entities = new(false);

    /// <summary>
    /// All sorted cameras
    /// </summary>
    internal readonly ExpandableContainer<Camera> sortedCameras = new(false);

    /// <summary>
    /// All root entities
    /// </summary>
    internal readonly ExpandableContainer<Transform> rootEntities = new(false);

    /// <summary>
    /// A cached list of all entities
    /// </summary>
    private readonly ExpandableContainer<EntityInfo> cachedEntityList = new();

    /// <summary>
    /// Whether we need to emit a world change
    /// </summary>
    private bool needsEmitWorldChange = false;

    /// <summary>
    /// List of all dead entities (by index)
    /// </summary>
    private readonly SortedSet<int> deadEntities = [];

    /// <summary>
    /// List of all updated components
    /// </summary>
    private readonly HashSet<Component> updatedComponents = [];

    /// <summary>
    /// Total entities
    /// </summary>
    private int entityCount;

    /// <summary>
    /// Receivers for the world changing
    /// </summary>
    private static readonly WorldChangeBox worldChangeReceivers = new();

    /// <summary>
    /// All scene queries
    /// </summary>
    private static readonly SceneQueryBox sceneQueries = new();

    /// <summary>
    /// All component changes (component type hash to observer box)
    /// </summary>
    private static readonly Dictionary<int, ComponentChangedBox> componentChanges = [];

    /// <summary>
    /// All callbacks for added components (component type hash to observer box)
    /// </summary>
    private static readonly Dictionary<int, List<OnComponentChangedCallback>> componentAddedCallbacks = [];

    /// <summary>
    /// All callbacks for changed components (component type hash to observer box)
    /// </summary>
    private static readonly Dictionary<int, List<OnComponentChangedCallback>> componentChangedCallbacks = [];

    /// <summary>
    /// All callbacks for removed components (component type hash to observer box)
    /// </summary>
    private static readonly Dictionary<int, List<OnComponentChangedCallback>> componentRemovedCallbacks = [];

    /// <summary>
    /// Adds a scene query receiver to the observer list
    /// </summary>
    /// <param name="receiver">The receiver</param>
    internal static void AddSceneQuery(ISceneQuery receiver)
    {
        if (receiver == null)
        {
            return;
        }

        lock (globalLockObject)
        {
            sceneQueries.AddObserver(receiver);

            receiver.WorldChanged(Current);
        }
    }

    /// <summary>
    /// Removes a scene query receiver from the observer list
    /// </summary>
    /// <param name="receiver">The receiver</param>
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

    /// <summary>
    /// Adds a world change receiver to the observer list
    /// </summary>
    /// <param name="receiver">The receiver</param>
    internal static void AddWorldChangeReceiver(IWorldChangeReceiver receiver)
    {
        if(receiver == null)
        {
            return;
        }

        lock(globalLockObject)
        {
            worldChangeReceivers.AddObserver(receiver);

            receiver.WorldChanged(Current);
        }
    }

    /// <summary>
    /// Removes a world change receiver from the observer list
    /// </summary>
    /// <param name="receiver">The receiver</param>
    internal static void RemoveWorldChangeReceiver(IWorldChangeReceiver receiver)
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

    /// <summary>
    /// Adds a component change receiver to the observer list
    /// </summary>
    /// <param name="receiver">The receiver</param>
    internal static void AddComponentChangeReceiver(IComponentChangeReceiver receiver)
    {
        if(receiver == null)
        {
            return;
        }

        lock(globalLockObject)
        {
            var world = Current;

            var hash = receiver.ComponentHash;

            if(!componentChanges.TryGetValue(hash, out var box))
            {
                box = new();

                componentChanges.Add(hash, box);
            }

            box.AddObserver(receiver);

            if (world == null || !world.componentCompatibilityCache.TryGetValue(receiver.ComponentHash, out var compatibleHashes))
            {
                return;
            }

            foreach (var entity in world.entities.Contents)
            {
                foreach (var container in entity.versionableComponentsArray.Contents)
                {
                    if(!compatibleHashes.Contains(container.hash))
                    {
                        continue;
                    }

                    receiver.MarkComponentChanged(container.versionable);
                }
            }
        }
    }

    /// <summary>
    /// Emits a world change event
    /// </summary>
    /// <param name="isWorldReplaced">Whether it's a world replaced event</param>
    internal static void EmitWorldChangedEvent(bool isWorldReplaced)
    {
        var world = Current;

        if(world == null)
        {
            sceneQueries.Emit();

            if(isWorldReplaced)
            {
                worldChangeReceivers.EmitWorldReplacedEvent();
            }
            else
            {
                worldChangeReceivers.Emit();
            }

            return;
        }

        if(world.cachedEntityList.Length != world.entities.Length)
        {
            world.cachedEntityList.Resize(world.entities.Length, false);

            world.entities.Contents.CopyTo(world.cachedEntityList.Contents);
        }

        {
            world.sortedCameras.Clear();

            var cameras = Scene.Query<Camera>(false);

            foreach (Camera camera in cameras)
            {
                world.sortedCameras.Add(camera);
            }

            world.sortedCameras.Contents.Sort((x, y) => x.depth.CompareTo(y.depth));

            {
                var transforms = Scene.Query<Transform>(true);

                world.rootEntities.Clear();

                foreach(var t in transforms)
                {
                    if (t.Parent != null)
                    {
                        continue;
                    }

                    world.rootEntities.Add(t);
                }
            }
        }

        sceneQueries.Emit();

        if (isWorldReplaced)
        {
            worldChangeReceivers.EmitWorldReplacedEvent();

            foreach(var entity in world.entities.Contents)
            {
                foreach(var container in entity.versionableComponentsArray.Contents)
                {
                    world.PropagateChangedComponentObservers(container.hash, container.versionable);
                }
            }
        }
        else
        {
            worldChangeReceivers.Emit();
        }
    }

    /// <summary>
    /// Requests a world update next frame
    /// </summary>
    internal void RequestWorldUpdate()
    {
        lock (lockObject)
        {
            needsEmitWorldChange = true;
        }
    }

    /// <summary>
    /// Called at the start of the frame
    /// </summary>
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
                        if(entityInfo.components.TryGetValue(item.Item2, out var container))
                        {
                            entityInfo.componentsArray.Remove(container);
                            entityInfo.versionableComponentsArray.Remove(container);

                            entityInfo.components.Remove(item.Item2);

                            needsEmitWorldChange = true;
                        }
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

                    var components = info.componentsArray.Contents;

                    foreach(var container in components)
                    {
                        RemoveComponent(e, container.component.GetType());
                    }

                    info.components.Clear();
                    info.componentsArray.Clear();
                    info.versionableComponentsArray.Clear();

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

                EmitWorldChangedEvent(false);
            }
            else
            {
                foreach(var c in updatedComponents)
                {
                    if(!(c?.Entity.IsValid ?? false))
                    {
                        continue;
                    }

                    EmitChangedComponentEvent(c);
                }
            }

            updatedComponents.Clear();
        }
    }

    /// <summary>
    /// Disposes this <see cref="World"/>'s entities and resources
    /// </summary>
    internal void Dispose()
    {
        lock(lockObject)
        {
            needsEmitWorldChange = true;

            foreach (var entity in entities.Contents)
            {
                entity.transform?.Entity = default;

                var components = entity.componentsArray.Contents;

                foreach(var container in components)
                {
                    RemoveComponent(entity.entityValue, container.component.GetType());
                }

                entity.components.Clear();
                entity.componentsArray.Clear();
                entity.versionableComponentsArray.Clear();
            }

            entities.Clear();
            cachedEntityList.Clear();
            destroyedEntities.Clear();
            removedComponents.Clear();
            updatedComponents.Clear();

            if (needsEmitWorldChange)
            {
                needsEmitWorldChange = false;

                EmitWorldChangedEvent(false);
            }
        }
    }

    /// <summary>
    /// Reports that a component was changed and queues it for an <see cref="EmitChangedComponentEvent(Component)"/>
    /// </summary>
    /// <param name="component">The component</param>
    public void ReportChangedComponent(Component component)
    {
        updatedComponents.Add(component);
    }
}
