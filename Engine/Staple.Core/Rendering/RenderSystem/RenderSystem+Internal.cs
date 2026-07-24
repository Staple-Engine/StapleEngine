using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Staple.Internal;

public sealed partial class RenderSystem
{
    #region Fields and Classes
    internal static readonly string LogTag = "RenderSystem";

    /// <summary>
    /// Size of spatial partitioning cells
    /// </summary>
    internal const int StartingSpatialPartitionSize = 50;

    /// <summary>
    /// Size of spatial partitioning cells
    /// </summary>
    internal static int SpatialPartitionSize = 50;

    /// <summary>
    /// Offset when balancing the spatial partition size
    /// </summary>
    internal const int SpatialPartitionBalanceOffset = 50;

    /// <summary>
    /// The target amount of spatial nodes to check
    /// </summary>
    internal const int TargetSpatialPartitionNodeCount = 100;

    /// <summary>
    /// Maximum amount of frames before a spatial node's visibility is recalculated
    /// </summary>
    internal const int MaxFramesBetwenSpatialRecalculation = 6;

    /// <summary>
    /// How many frames to wait before doing visibility checks
    /// </summary>
    internal const int MaxFramesBetweenVisibilityChecks = 3;

    /// <summary>
    /// Vector with the size of spatial partitioning cells
    /// </summary>
    internal static Vector3 SpatialPartitionSizeVector = new(SpatialPartitionSize, SpatialPartitionSize, SpatialPartitionSize);

    /// <summary>
    /// Vector with half the size of spatial partitioning cells
    /// </summary>
    internal static Vector3 SpatialPartitionSizeHalfVector = new(SpatialPartitionSize / 2.0f, SpatialPartitionSize / 2.0f,
        SpatialPartitionSize / 2.0f);

    public class RenderSystemRenderQueue
    {
        public RenderSystemInfo renderSystem;

        public readonly Dictionary<int, IRenderQueue> queue = [];
    }

    public class RenderSystemCameraSet
    {
        public Camera camera;

        public Transform transform;

        public readonly ExpandableContainer<RenderSystemRenderQueue> renderSystems = new(false);
    }

    internal static byte Priority = 1;

    /// <summary>
    /// Render thread lock
    /// </summary>
    private readonly Lock lockObject = new();

    /// <summary>
    /// All registered render systems
    /// </summary>
    internal readonly List<RenderSystemInfo> renderSystems = [];

    /// <summary>
    /// Temporary transform for rendering with the interpolator
    /// </summary>
    private readonly Transform stagingTransform = new();

    /// <summary>
    /// Queued list of callbacks for frames
    /// </summary>
    private readonly Dictionary<uint, List<Action>> queuedFrameCallbacks = [];

    /// <summary>
    /// The render queue
    /// </summary>
    private readonly ExpandableContainer<RenderSystemCameraSet> renderQueue = new(false);

    /// <summary>
    /// The entity query for every entity with a transform
    /// </summary>
    internal readonly SceneQuery<Transform> entityQuery = new();

    /// <summary>
    /// All transforms of each entity
    /// </summary>
    internal readonly ExpandableContainer<Matrix4x4> entityTransforms = new(1024);

    /// <summary>
    /// Spatial location of each entity
    /// </summary>
    internal Dictionary<Vector3Int, List<Transform>> spatialEntities = [];

    /// <summary>
    /// Keeps track of where an entity was located the previous frame
    /// </summary>
    private readonly ExpandableContainer<HashSet<Vector3Int>> lastSpatialEntities = new(1024);

    /// <summary>
    /// Keeps track of which entities we already processed
    /// </summary>
    private readonly ExpandableContainer<bool> processedSpatialEntities = new(1024);

    /// <summary>
    /// Tracker for each entity's transform
    /// </summary>
    private readonly ComponentVersionTracker<Transform> entityTransformTracker = new();

    /// <summary>
    /// Tracker for each entity's renderable
    /// </summary>
    private readonly ComponentVersionTracker<Renderable> entityRenderableTracker = new();

    /// <summary>
    /// Tracks all changed entity transforms in ranges (key is start index, value is length)
    /// </summary>
    internal readonly Dictionary<int, int> changedEntityTransformRanges = [];

    /// <summary>
    /// All renderables
    /// </summary>
    private readonly ExpandableContainer<Renderable> renderables = new(1024);

    /// <summary>
    /// The material hashes of all renderables
    /// </summary>
    private readonly ExpandableContainer<int> renderableMaterialHashes = new(1024);

    /// <summary>
    /// Frame counter for how many frames to wait before checking visibility
    /// </summary>
    private int visibilityCheckCounter = 0;

    /// <summary>
    /// The renderer backend
    /// </summary>
    internal static readonly IRendererBackend Backend = new SDLGPURendererBackend();

    /// <summary>
    /// The already verified to be visible entities.
    /// So we can ignore the ones that were already validated as visible when we find them again to be tested
    /// </summary>
    internal static readonly HashSet<Entity> visibleEntities = [];

    #endregion

    #region Helpers
    /// <summary>
    /// Gets the reset flags for specific video flags
    /// </summary>
    /// <param name="videoFlags">The video flags to use</param>
    /// <returns>The reset flags</returns>
    internal static RenderModeFlags RenderFlags(VideoFlags videoFlags)
    {
        var resetFlags = RenderModeFlags.None;

        if (videoFlags.HasFlag(VideoFlags.Vsync))
        {
            resetFlags |= RenderModeFlags.Vsync;
        }

        if(videoFlags.HasFlag(VideoFlags.TripleBuffering))
        {
            resetFlags |= RenderModeFlags.TripleBuffering;
        }

        if (videoFlags.HasFlag(VideoFlags.HDR10))
        {
            resetFlags |= RenderModeFlags.HDR10;
        }

        return resetFlags;
    }

    /// <summary>
    /// Returns whether a combination of blend modes results in opaque geometry
    /// </summary>
    /// <param name="sourceBlend">The source blend mode</param>
    /// <param name="destinationBlend">The destination blend mode</param>
    /// <returns>Whether it's opaque</returns>
    internal static bool IsOpaque(BlendMode sourceBlend, BlendMode destinationBlend)
    {
        return (sourceBlend == BlendMode.Off && destinationBlend == BlendMode.Off) ||
                (sourceBlend == BlendMode.One && destinationBlend == BlendMode.Zero) ||
                (sourceBlend == BlendMode.Zero && destinationBlend == BlendMode.One);
    }
    #endregion

    #region Frame Callbacks
    /// <summary>
    /// Queues a callback to run at a specific frame
    /// </summary>
    /// <param name="frame">The frame to run the callback at</param>
    /// <param name="callback">The callback</param>
    internal void QueueFrameCallback(uint frame, Action callback)
    {
        lock (lockObject)
        {
            if (!queuedFrameCallbacks.TryGetValue(frame, out var list))
            {
                list = [];

                queuedFrameCallbacks.Add(frame, list);
            }

            list.Add(callback);
        }
    }

    /// <summary>
    /// Called at the start of a frame
    /// </summary>
    internal void OnStartFrame()
    {
        LightSystem.Instance.StartFrame();

        var world = World.Current;

        if(world != null)
        {
            foreach(var info in world.sortedCameras.Contents)
            {
                info.camera.OnStartFrame();
            }
        }
    }

    /// <summary>
    /// Called at the end of each frame
    /// </summary>
    /// <param name="frame">The current frame</param>
    internal void OnEndFrame(uint frame)
    {
        CurrentFrame = frame;

        List<Action> callbacks = null;

        lock (lockObject)
        {
            if (!queuedFrameCallbacks.TryGetValue(frame, out callbacks))
            {
                return;
            }
        }

        foreach (var item in callbacks)
        {
            try
            {
                item?.Invoke();
            }
            catch (Exception e)
            {
                Log.Debug($"[RenderSystem] While executing a frame callback at frame {frame}: {e}");
            }
        }

        lock (lockObject)
        {
            queuedFrameCallbacks.Remove(frame);
        }
    }
    #endregion

    #region Lifecycle
    public void Startup()
    {
        RegisterSystem(new CullingVolumeSystem());
        RegisterSystem(new MeshCombineSystem());
        RegisterSystem(new SkinnedMeshAnimatorSystem());
        RegisterSystem(new SkinnedMeshAttachmentSystem());
        RegisterSystem(new SkinnedMeshRenderSystem());
        RegisterSystem(new MeshRenderSystem());
        RegisterSystem(new TextRenderSystem());

        LightSystem.Enabled = AppSettings.Active.enableLighting;
    }

    public void Shutdown()
    {
        foreach (var systemInfo in renderSystems)
        {
            systemInfo.system.Shutdown();
        }
    }

    public void Update()
    {
        if (World.Current is not World world)
        {
            return;
        }

        RenderStats.Clear();

        UpdateEntityTransforms(world);

        foreach (var systemInfo in renderSystems)
        {
            if(!systemInfo.system.UsesOwnRenderProcess)
            {
                continue;
            }

            systemInfo.system.Prepare();
        }

        UpdateStandard();

        foreach (var systemInfo in renderSystems)
        {
            if (!systemInfo.system.UsesOwnRenderProcess)
            {
                continue;
            }

            systemInfo.system.Submit();
        }

        if(CheckMaterialChanges())
        {
            World.Current?.RequestWorldUpdate();
        }
    }

    /// <summary>
    /// Verifies for any changed materials and requests a world update if that happens
    /// </summary>
    internal bool CheckMaterialChanges()
    {
        var renderablesContents = renderables.Contents;
        var renderablesHashContents = renderableMaterialHashes.Contents;

        for(var i = 0; i < renderablesContents.Length; i++)
        {
            if (renderablesContents[i] == null ||
                renderablesContents[i].MaterialState == renderablesHashContents[i])
            {
                continue;
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Clears the culling states of the entire render queue
    /// </summary>
    internal void ClearCullingStates()
    {
        var renderableSpan = renderables.Contents;
        var l = renderableSpan.Length;

        for (var i = 0; i < l; i++)
        {
            ref var renderable = ref renderableSpan[i];

            if(renderable == null)
            {
                continue;
            }

            renderable.cullingState = CullingState.None;
        }

        visibleEntities.Clear();
    }

    /// <summary>
    /// Removes all subsystems belonging to an assembly
    /// </summary>
    /// <param name="assembly">The assembly to check</param>
    internal void RemoveAllSubsystems(Assembly assembly)
    {
        lock (lockObject)
        {
            for (var i = renderSystems.Count - 1; i >= 0; i--)
            {
                if (renderSystems[i].system.GetType().Assembly != assembly)
                {
                    continue;
                }
                
                renderSystems[i].system.Shutdown();

                renderSystems.RemoveAt(i);
            }
        }
    }

    internal static void SetSpatialPartitionSize(int size)
    {
        SpatialPartitionSize = size;

        SpatialPartitionSizeVector = new(SpatialPartitionSize, SpatialPartitionSize, SpatialPartitionSize);

        SpatialPartitionSizeHalfVector = new(SpatialPartitionSize / 2.0f, SpatialPartitionSize / 2.0f,
            SpatialPartitionSize / 2.0f);

        Instance.processedSpatialEntities.ClearValues();

        Instance.spatialEntities.Clear();
        Instance.entityRenderableTracker.Clear();
        Instance.entityTransformTracker.Clear();
        Instance.changedEntityTransformRanges.Clear();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static Vector3Int GetEntitySpatialLocation(Vector3 position)
    {
        return new Vector3Int(Math.FloorToInt(position.X / SpatialPartitionSize),
            Math.FloorToInt(position.Y / SpatialPartitionSize),
            Math.FloorToInt(position.Z / SpatialPartitionSize));
    }

    /// <summary>
    /// Iterates all spatial locations that some bounds belong to
    /// </summary>
    /// <param name="bounds">The bounds</param>
    /// <param name="callback">The callback. You should return true if you want the process to stop (such as confirming the object is visible)</param>
    /// <remarks>Should only ever be used by a single thread. Do not use this across threads!</remarks>
    internal static void IterateEntitySpatialLocations(AABB bounds, Func<Vector3Int, bool> callback)
    {
        var coordinateBoundsMin = new Vector3Int(9999, 9999, 9999);
        var coordinateBoundsMax = new Vector3Int(-9999, -9999, -9999);

        var corners = bounds.Corners;

        foreach(var corner in corners)
        {
            var coordinate = GetEntitySpatialLocation(corner);

            if(coordinateBoundsMin.X > coordinate.X)
            {
                coordinateBoundsMin.X = coordinate.X;
            }

            if (coordinateBoundsMax.X < coordinate.X)
            {
                coordinateBoundsMax.X = coordinate.X;
            }

            if (coordinateBoundsMin.Y > coordinate.Y)
            {
                coordinateBoundsMin.Y = coordinate.Y;
            }

            if (coordinateBoundsMax.Y < coordinate.Y)
            {
                coordinateBoundsMax.Y = coordinate.Y;
            }

            if (coordinateBoundsMin.Z > coordinate.Z)
            {
                coordinateBoundsMin.Z = coordinate.Z;
            }

            if (coordinateBoundsMax.Z < coordinate.Z)
            {
                coordinateBoundsMax.Z = coordinate.Z;
            }
        }

        for (var x = coordinateBoundsMin.X; x <= coordinateBoundsMax.X; x++)
        {
            for (var y = coordinateBoundsMin.Y; y <= coordinateBoundsMax.Y; y++)
            {
                for (var z = coordinateBoundsMin.Z; z <= coordinateBoundsMax.Z; z++)
                {
                    if(callback(new(x, y, z)))
                    {
                        break;
                    }
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static AABB MakeSpatialAABB(Vector3Int coordinate)
    {
        return new AABB((Vector3)coordinate * SpatialPartitionSize + SpatialPartitionSizeHalfVector, SpatialPartitionSizeVector);
    }

    internal void UpdateEntitySpatialData(int index, Transform transform, Renderable renderable, Span<bool> processed, Span<HashSet<Vector3Int>> spatials)
    {
        if(renderable == null)
        {
            return;
        }

        if(!entityRenderableTracker.ShouldUpdateComponent(transform.Entity, in renderable))
        {
            return;
        }

        ref var container = ref spatials[index];

        container ??= [];

        var entitySpatialSet = container;

        foreach (var lastSpatial in entitySpatialSet)
        {
            if (!spatialEntities.TryGetValue(lastSpatial, out var transforms))
            {
                continue;
            }

            transforms.Remove(transform);
        }

        entitySpatialSet.Clear();

        ref var processedResult = ref processed[index];

        var shouldAddAnyway = !processedResult;

        processedResult = true;

        IterateEntitySpatialLocations(renderable.bounds, (coordinate) =>
        {
            entitySpatialSet.Add(coordinate);

            if (!spatialEntities.TryGetValue(coordinate, out var newSpatialStorage))
            {
                newSpatialStorage = [];

                spatialEntities.Add(coordinate, newSpatialStorage);
            }

            newSpatialStorage.Add(transform);

            return false;
        });
    }

    internal void BalanceSpatialNodes(World world)
    {
        if(spatialEntities.Count <= TargetSpatialPartitionNodeCount)
        {
            return;
        }

        SetSpatialPartitionSize(SpatialPartitionSize + SpatialPartitionBalanceOffset);

        UpdateEntityTransforms(world);
    }

    /// <summary>
    /// Updates all the entity transform data, should be called once per frame
    /// </summary>
    /// <param name="world">The current world</param>
    internal void UpdateEntityTransforms(World world)
    {
        changedEntityTransformRanges.Clear();

        var startIndex = -1;
        var length = 0;

        var entities = world.entities.Contents;

        if (world.entities.Length > entityTransforms.Length)
        {
            var newSize = entityTransforms.Length * 2;

            while (newSize < world.entities.Length)
            {
                newSize *= 2;
            }

            entityTransforms.Resize(newSize, true);
            lastSpatialEntities.Resize(newSize, true);
            processedSpatialEntities.Resize(newSize, true);
            renderables.Resize(newSize, false);
            renderableMaterialHashes.Resize(newSize, false);

            var renderableSpan = renderables.Contents;
            var renderableMaterialHashesContents = renderableMaterialHashes.Contents;

            for(var i = 0; i < entities.Length; i++)
            {
                renderableSpan[i] = entities[i].ToEntity().GetComponent<Renderable>();
                renderableMaterialHashesContents[i] = renderableSpan[i]?.MaterialState ?? 0;
            }
        }

        var renderablesContents = renderables.Contents;
        var transforms = entityTransforms.Contents;

        for (var i = 0; i < entities.Length; i++)
        {
            ref var entity = ref entities[i];

            if(entity.alive == false || entity.transform == null)
            {
                if (startIndex < 0)
                {
                    continue;
                }

                changedEntityTransformRanges.Add(startIndex, length);

                startIndex = -1;

                continue;
            }

            var renderable = renderablesContents[i];

            if (!entityTransformTracker.ShouldUpdateComponent(entity.ToEntity(), in entity.transform))
            {
                if (startIndex < 0)
                {
                    UpdateEntitySpatialData(i, entity.transform, renderable, processedSpatialEntities.Contents, lastSpatialEntities.Contents);

                    continue;
                }

                changedEntityTransformRanges.Add(startIndex, length);

                startIndex = -1;

                UpdateEntitySpatialData(i, entity.transform, renderable, processedSpatialEntities.Contents, lastSpatialEntities.Contents);

                continue;
            }

            transforms[i] = entity.transform.Matrix;

            if (startIndex < 0)
            {
                startIndex = i;
                length = 1;
            }
            else
            {
                length++;
            }

            UpdateEntitySpatialData(i, entity.transform, renderable, processedSpatialEntities.Contents, lastSpatialEntities.Contents);
        }

        if (startIndex >= 0)
        {
            changedEntityTransformRanges.Add(startIndex, length);
        }

        if(spatialEntities.Count > TargetSpatialPartitionNodeCount)
        {
            BalanceSpatialNodes(world);
        }
    }

    public void WorldReplaced(World world)
    {
        lock(lockObject)
        {
            renderQueue.Clear();
            entityTransformTracker.Clear();
            entityRenderableTracker.Clear();

            if (entityQuery.Contents.Length > renderables.Length)
            {
                var newSize = renderables.Length * 2;

                while (newSize < entityQuery.Contents.Length)
                {
                    newSize *= 2;
                }

                renderables.Resize(newSize, false);
                renderableMaterialHashes.Resize(newSize, false);
            }

            processedSpatialEntities.ClearValues();

            foreach (var set in lastSpatialEntities.Contents)
            {
                set?.Clear();
            }

            foreach(var pair in spatialEntities)
            {
                pair.Value.Clear();
            }

            SpatialPartitionSize = StartingSpatialPartitionSize;

            {
                var renderableContents = renderables.Contents;
                var renderableMaterialHashesContents = renderableMaterialHashes.Contents;

                foreach (var entityInfo in entityQuery.Contents)
                {
                    var renderable = entityInfo.Item1.GetComponent<Renderable>();

                    renderableContents[entityInfo.Item1.Identifier.ID - 1] = renderable;
                    renderableMaterialHashesContents[entityInfo.Item1.Identifier.ID - 1] = renderable?.MaterialState ?? 0;
                }
            }

            var cameras = world.SortedCameras;

            if (cameras.Length <= 0)
            {
                return;
            }

            var renderSystemContent = CollectionsMarshal.AsSpan(renderSystems);

            foreach (var cameraInfo in cameras)
            {
                renderQueue.AddDefault();

                ref var collected = ref renderQueue.Contents[renderQueue.Length - 1];

                collected ??= new();

                collected.camera = cameraInfo.camera;
                collected.transform = cameraInfo.transform;
                collected.renderSystems.Resize(renderSystems.Count, true);

                for(var i = 0; i < renderSystemContent.Length; i++)
                {
                    ref var system = ref collected.renderSystems.Contents[i];

                    system ??= new();

                    system.renderSystem = renderSystemContent[i];

                    foreach (var pair in system.queue)
                    {
                        pair.Value.Clear();
                    }
                }

                foreach (var entityInfo in entityQuery.Contents)
                {
                    var layer = entityInfo.Item1.Layer;

                    if (!cameraInfo.camera.cullingLayers.HasLayer(layer))
                    {
                        continue;
                    }

                    for(var i = 0; i < renderSystemContent.Length; i++)
                    {
                        var systemInfo = renderSystemContent[i];

                        if (systemInfo.system.UsesOwnRenderProcess ||
                            !entityInfo.Item1.TryGetComponent(systemInfo.system.RelatedComponent, out var component))
                        {
                            continue;
                        }

                        ref var content = ref collected.renderSystems.Contents[i];

                        if (systemInfo.isRenderable)
                        {
                            var renderable = (Renderable)component;

                            foreach (var material in renderable.materials)
                            {
                                if (!(material?.IsValid ?? false))
                                {
                                    continue;
                                }

                                var priority = material.RenderQueueIndex;

                                if (!content.queue.TryGetValue(priority, out var queue) ||
                                    queue == null ||
                                    queue.GetType() != systemInfo.system.QueueType)
                                {
                                    queue = systemInfo.system.CreateRenderQueue();

                                    content.queue.AddOrSetKey(priority, queue);
                                }

                                queue.Add(entityInfo.Item1, entityInfo.Item2, renderable);
                            }
                        }
                        else
                        {
                            if (!content.queue.TryGetValue(0, out var queue) ||
                                queue == null ||
                                queue.GetType() != systemInfo.system.QueueType)
                            {
                                queue = systemInfo.system.CreateRenderQueue();

                                content.queue.AddOrSetKey(0, queue);
                            }

                            queue.Add(entityInfo.Item1, entityInfo.Item2, component);
                        }
                    }
                }
            }
        }
    }

    public void WorldChanged(World world)
    {
        lock (lockObject)
        {
            renderQueue.Clear();

            if(entityQuery.Contents.Length > renderables.Length)
            {
                var newSize = renderables.Length * 2;

                while (newSize < entityQuery.Contents.Length)
                {
                    newSize *= 2;
                }

                renderables.Resize(newSize, false);
                renderableMaterialHashes.Resize(newSize, false);
            }

            processedSpatialEntities.ClearValues();

            {
                var renderableContents = renderables.Contents;
                var renderableMaterialHashesContents = renderableMaterialHashes.Contents;

                foreach (var entityInfo in entityQuery.Contents)
                {
                    var renderable = entityInfo.Item1.GetComponent<Renderable>();

                    renderableContents[entityInfo.Item1.Identifier.ID - 1] = renderable;
                    renderableMaterialHashesContents[entityInfo.Item1.Identifier.ID - 1] = renderable?.MaterialState ?? 0;
                }
            }

            if(world == null)
            {
                return;
            }

            var cameras = world.SortedCameras;

            if (cameras.Length <= 0)
            {
                return;
            }

            var renderSystemContent = CollectionsMarshal.AsSpan(renderSystems);

            foreach (var cameraInfo in cameras)
            {
                renderQueue.AddDefault();

                ref var collected = ref renderQueue.Contents[renderQueue.Length - 1];

                collected ??= new();

                collected.camera = cameraInfo.camera;
                collected.transform = cameraInfo.transform;
                collected.renderSystems.Resize(renderSystems.Count, true);

                for (var i = 0; i < renderSystemContent.Length; i++)
                {
                    ref var system = ref collected.renderSystems.Contents[i];

                    system ??= new();

                    system.renderSystem = renderSystemContent[i];

                    system.queue.Clear();

                    foreach (var pair in system.queue)
                    {
                        pair.Value.Clear();
                    }
                }

                foreach (var entityInfo in entityQuery.Contents)
                {
                    var layer = entityInfo.Item1.Layer;

                    if (!cameraInfo.camera.cullingLayers.HasLayer(layer))
                    {
                        continue;
                    }

                    for (var i = 0; i < renderSystemContent.Length; i++)
                    {
                        var systemInfo = renderSystemContent[i];

                        if (systemInfo.system.UsesOwnRenderProcess ||
                            !entityInfo.Item1.TryGetComponent(systemInfo.system.RelatedComponent, out var component))
                        {
                            continue;
                        }

                        ref var content = ref collected.renderSystems.Contents[i];

                        if (systemInfo.isRenderable)
                        {
                            var renderable = (Renderable)component;

                            foreach (var material in renderable.materials)
                            {
                                if (!(material?.IsValid ?? false))
                                {
                                    continue;
                                }

                                var priority = material.RenderQueueIndex;

                                if (!content.queue.TryGetValue(priority, out var queue) ||
                                    queue == null ||
                                    queue.GetType() != systemInfo.system.QueueType)
                                {
                                    queue = systemInfo.system.CreateRenderQueue();

                                    content.queue.AddOrSetKey(priority, queue);
                                }

                                queue.Add(entityInfo.Item1, entityInfo.Item2, renderable);
                            }
                        }
                        else
                        {
                            if (!content.queue.TryGetValue(0, out var queue) ||
                                queue == null ||
                                queue.GetType() != systemInfo.system.QueueType)
                            {
                                queue = systemInfo.system.CreateRenderQueue();

                                content.queue.AddOrSetKey(0, queue);
                            }

                            queue.Add(entityInfo.Item1, entityInfo.Item2, component);
                        }
                    }
                }
            }
        }
    }
    #endregion

    #region Render Modes
    /// <summary>
    /// Update process for standard rendering
    /// </summary>
    private void UpdateStandard()
    {
        using var profiler = new PerformanceProfiler(PerformanceProfilerType.Rendering);

        foreach (var set in renderQueue.Contents)
        {
            RenderStandard(set, true);
        }
    }
    #endregion

    #region Render Helpers
    /// <summary>
    /// Prepares a camera for rendering
    /// </summary>
    /// <param name="entity">The camera's entity</param>
    /// <param name="camera">The camera</param>
    /// <param name="cameraTransform">The camera's transform</param>
    private static void PrepareCamera(Entity entity, Camera camera, Transform cameraTransform)
    {
        var projection = Camera.Projection(entity, camera);
        var view = cameraTransform.Matrix;

        Matrix4x4.Invert(view, out view);

        camera.UpdateFrustum(view, projection);

        Backend.BeginRenderPass(RenderTarget.Current, camera.clearMode, camera.clearColor, camera.viewport,
            in view, in projection);
    }

    /// <summary>
    /// Prepares a render pass
    /// </summary>
    /// <param name="target">The render target, if any</param>
    /// <param name="clearMode">How to clear the target</param>
    /// <param name="clearColor">The color to clear if clearMode is <see cref="CameraClearMode.SolidColor"/></param>
    /// <param name="viewport">The viewport area to render to (normalized coordinates for x, y, width, height)</param>
    /// <param name="cameraTransform">The transform of the camera</param>
    /// <param name="projection">The projection matrix</param>
    private static void PrepareRender(RenderTarget target, CameraClearMode clearMode,
        Color clearColor, Vector4 viewport, Matrix4x4 cameraTransform, Matrix4x4 projection)
    {
        Matrix4x4.Invert(cameraTransform, out var view);

        Backend.BeginRenderPass(target ?? RenderTarget.Current, clearMode, clearColor, viewport, in view, in projection);
    }

    /// <summary>
    /// Submits something to render
    /// </summary>
    /// <param name="state">The render state</param>
    /// <param name="triangles">How many triangles are being rendered</param>
    /// <param name="instances">How many instances are being rendered</param>
    internal static void Submit(RenderState state, int triangles, int instances)
    {
        Backend.Render(state);

        RenderStats.drawCalls++;
        RenderStats.triangleCount += triangles * instances;
        RenderStats.instanceCount += instances;

        if (instances > 1)
        {
            RenderStats.savedDrawCalls += (instances - 1);
        }
    }

    /// <summary>
    /// Submits static meshes for rendering with multidraw
    /// </summary>
    /// <param name="state">The render state</param>
    /// <param name="entries">The entries for multidraw</param>
    /// <param name="triangles">How many triangles are being rendered</param>
    internal static void SubmitStatic(RenderState state, Span<MultidrawEntry> entries, int triangles)
    {
        Backend.RenderStatic(state, entries);

        RenderStats.drawCalls++;
        RenderStats.triangleCount += triangles;

        foreach(var entry in entries)
        {
            RenderStats.savedDrawCalls += entry.transforms.Count;
        }

        RenderStats.savedDrawCalls--;
    }
    #endregion
}
