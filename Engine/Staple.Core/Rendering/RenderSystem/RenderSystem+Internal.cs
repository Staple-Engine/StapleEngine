using Bgfx;
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
    /// <summary>
    /// Contains information on a draw call
    /// </summary>
    internal class DrawCall
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Entity entity;
        public Renderable renderable;
        public IComponent relatedComponent;
    }

    /// <summary>
    /// Contains lists of drawcalls per view ID
    /// </summary>
    internal class DrawBucket
    {
        public Dictionary<ushort, List<DrawCall>> drawCalls = [];
    }

    internal static byte Priority = 1;

    /// <summary>
    /// Keep the current and previous draw buckets to interpolate around
    /// </summary>
    private DrawBucket previousDrawBucket = new(), currentDrawBucket = new();

    /// <summary>
    /// Render thread lock
    /// </summary>
    private readonly Lock lockObject = new();

    /// <summary>
    /// Whether we need to generate draw calls (interpolator only)
    /// </summary>
    private bool needsDrawCalls = false;

    /// <summary>
    /// Time accumulator (interpolator only)
    /// </summary>
    private float accumulator = 0.0f;

    /// <summary>
    /// All registered render systems
    /// </summary>
    internal readonly List<IRenderSystem> renderSystems = [];

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
    private readonly List<((Camera, Transform), List<(IRenderSystem, List<(Entity, Transform, IComponent)>)>)> renderQueue = [];

    /// <summary>
    /// The entity query for every entity with a transform
    /// </summary>
    private readonly SceneQuery<Transform> entityQuery = new();

    /// <summary>
    /// Cached per-frame used view IDs
    /// </summary>
    private HashSet<ushort> usedViewIDs = [];

    /// <summary>
    /// Cached per-frame used view IDs (previous frame)
    /// </summary>
    private HashSet<ushort> previousUsedViewIDs = [];
    #endregion

    #region Helpers
    /// <summary>
    /// Calculates the blending function for blending flags
    /// </summary>
    /// <param name="source">The blending source flag</param>
    /// <param name="destination">The blending destination flag</param>
    /// <returns>The combined function</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong BlendFunction(bgfx.StateFlags source, bgfx.StateFlags destination)
    {
        return BlendFunction(source, destination, source, destination);
    }

    /// <summary>
    /// Calculates the blending function for blending flags
    /// </summary>
    /// <param name="sourceColor">The source color flag</param>
    /// <param name="destinationColor">The destination color flag</param>
    /// <param name="sourceAlpha">The source alpha blending flag</param>
    /// <param name="destinationAlpha">The destination alpha blending flag</param>
    /// <returns>The combined function</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ulong BlendFunction(bgfx.StateFlags sourceColor, bgfx.StateFlags destinationColor, bgfx.StateFlags sourceAlpha, bgfx.StateFlags destinationAlpha)
    {
        return (ulong)sourceColor | (ulong)destinationColor << 4 | ((ulong)sourceAlpha | (ulong)destinationAlpha << 4) << 8;
    }

    /// <summary>
    /// Checks if we have an available transient buffer
    /// </summary>
    /// <param name="numVertices">The amount of vertices to check</param>
    /// <param name="layout">The vertex layout to use</param>
    /// <param name="numIndices">The amount of indices to check</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool CheckAvailableTransientBuffers(uint numVertices, bgfx.VertexLayout layout, uint numIndices)
    {
        unsafe
        {
            return numVertices == bgfx.get_avail_transient_vertex_buffer(numVertices, &layout)
                && (0 == numIndices || numIndices == bgfx.get_avail_transient_index_buffer(numIndices, false));
        }
    }

    /// <summary>
    /// Gets the reset flags for specific video flags
    /// </summary>
    /// <param name="videoFlags">The video flags to use</param>
    /// <returns>The BGFX Reset Flags</returns>
    internal static bgfx.ResetFlags ResetFlags(VideoFlags videoFlags)
    {
        var resetFlags = bgfx.ResetFlags.FlushAfterRender | bgfx.ResetFlags.FlipAfterRender; //bgfx.ResetFlags.SrgbBackbuffer;

        if (videoFlags.HasFlag(VideoFlags.Vsync))
        {
            resetFlags |= bgfx.ResetFlags.Vsync;
        }

        if (videoFlags.HasFlag(VideoFlags.MSAAX2))
        {
            resetFlags |= bgfx.ResetFlags.MsaaX2;
        }
        else if (videoFlags.HasFlag(VideoFlags.MSAAX4))
        {
            resetFlags |= bgfx.ResetFlags.MsaaX4;
        }
        else if (videoFlags.HasFlag(VideoFlags.MSAAX8))
        {
            resetFlags |= bgfx.ResetFlags.MsaaX8;
        }
        else if (videoFlags.HasFlag(VideoFlags.MSAAX16))
        {
            resetFlags |= bgfx.ResetFlags.MsaaX16;
        }

        if (videoFlags.HasFlag(VideoFlags.HDR10))
        {
            resetFlags |= bgfx.ResetFlags.Hdr10;
        }

        if (videoFlags.HasFlag(VideoFlags.HiDPI))
        {
            resetFlags |= bgfx.ResetFlags.Hidpi;
        }

        return resetFlags;
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
            if (queuedFrameCallbacks.TryGetValue(frame, out var list) == false)
            {
                list = [];

                queuedFrameCallbacks.Add(frame, list);
            }

            list.Add(callback);
        }
    }

    /// <summary>
    /// Called at the end of each frame
    /// </summary>
    /// <param name="frame">The current frame</param>
    internal void OnFrame(uint frame)
    {
        CurrentFrame = frame;

        List<Action> callbacks = null;

        lock (lockObject)
        {
            if (queuedFrameCallbacks.TryGetValue(frame, out callbacks) == false)
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
        RegisterSystem(new LightSystem());
        RegisterSystem(new SpriteRenderSystem());
        RegisterSystem(new SkinnedMeshAnimatorSystem());
        RegisterSystem(new SkinnedMeshAttachmentSystem());
        RegisterSystem(new SkinnedMeshRenderSystem());
        RegisterSystem(new MeshRenderSystem());
        RegisterSystem(new TextRenderSystem());
        RegisterSystem(new UICanvasSystem());

        LightSystem.Enabled = AppSettings.Current?.enableLighting ?? true;

        Time.onAccumulatorFinished += () =>
        {
            needsDrawCalls = true;
        };
    }

    public void Shutdown()
    {
        foreach (var system in renderSystems)
        {
            system.Shutdown();
        }
    }

    public void Update()
    {
        if (World.Current == null)
        {
            return;
        }

        RenderStats.Clear();

        (previousUsedViewIDs, usedViewIDs) = (usedViewIDs, previousUsedViewIDs);

        usedViewIDs.Clear();

        ClearCullingStates();

        foreach(var system in renderSystems)
        {
            if(system.UsesOwnRenderProcess == false)
            {
                continue;
            }

            system.Prepare();
        }

        if (UseDrawcallInterpolator)
        {
            UpdateAccumulator();
        }
        else
        {
            UpdateStandard();
        }

        foreach (var system in renderSystems)
        {
            if (system.UsesOwnRenderProcess == false)
            {
                continue;
            }

            system.Submit(0);
        }

        previousUsedViewIDs.ExceptWith(usedViewIDs);

        if (previousUsedViewIDs.Count > 0)
        {
            foreach (var viewID in previousUsedViewIDs)
            {
                foreach (var system in renderSystems)
                {
                    if(system.UsesOwnRenderProcess)
                    {
                        continue;
                    }

                    system.ClearRenderData(viewID);
                }
            }
        }
    }

    /// <summary>
    /// Clears the culling states of the entire render queue
    /// </summary>
    internal void ClearCullingStates()
    {
        foreach (var pair in renderQueue)
        {
            foreach (var item in pair.Item2)
            {
                foreach (var (_, _, renderable) in item.Item2)
                {
                    if (renderable is not Renderable r)
                    {
                        continue;
                    }

                    r.cullingState = CullingState.None;
                }
            }
        }
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
                if (renderSystems[i].GetType().Assembly == assembly)
                {
                    renderSystems[i].Shutdown();

                    renderSystems.RemoveAt(i);
                }
            }
        }
    }

    public void WorldChanged()
    {
        lock (lockObject)
        {
            renderQueue.Clear();

            var cameras = World.Current.SortedCameras;

            if (cameras.Length > 0)
            {
                foreach (var cameraInfo in cameras)
                {
                    var collected = new Dictionary<IRenderSystem, List<(Entity, Transform, IComponent)>>();

                    foreach (var entityInfo in entityQuery.Contents)
                    {
                        var layer = entityInfo.Item1.Layer;

                        if (cameraInfo.camera.cullingLayers.HasLayer(layer) == false)
                        {
                            continue;
                        }

                        foreach (var system in renderSystems)
                        {
                            if(system.UsesOwnRenderProcess)
                            {
                                continue;
                            }

                            if (collected.TryGetValue(system, out var content) == false)
                            {
                                content = [];

                                collected.Add(system, content);
                            }

                            if (entityInfo.Item1.TryGetComponent(system.RelatedComponent, out var component))
                            {
                                content.Add((entityInfo.Item1, entityInfo.Item2, component));
                            }
                        }
                    }

                    var final = new List<(IRenderSystem, List<(Entity, Transform, IComponent)>)>();

                    foreach(var pair in collected)
                    {
                        final.Add((pair.Key, pair.Value));
                    }

                    renderQueue.Add(((cameraInfo.camera, cameraInfo.transform), final));
                }
            }
        }
    }
    #endregion

    #region Render Modes

    /// <summary>
    /// Renders in the standard mode (no interpolator)
    /// </summary>
    /// <param name="cameraEntity">The camera's entity</param>
    /// <param name="camera">The camera</param>
    /// <param name="cameraTransform">The camera's transform</param>
    /// <param name="queue">The render queue for this camera</param>
    /// <param name="cull">Whether to cull invisible elements</param>
    /// <param name="viewID">The view ID</param>
    public void RenderStandard(Entity cameraEntity, Camera camera, Transform cameraTransform,
        List<(IRenderSystem, List<(Entity, Transform, IComponent)>)> queue, bool cull, ushort viewID)
    {
        usedViewIDs.Add(viewID);

        CurrentCamera = (camera, cameraTransform);

        PrepareCamera(cameraEntity, camera, cameraTransform, viewID);

        var queueLength = queue.Count;

        for (var i = 0; i < queueLength; i++)
        {
            var (system, content) = queue[i];

            if(content.Count == 0)
            {
                continue;
            }

            system.Prepare();

            system.Preprocess(CollectionsMarshal.AsSpan(content), camera, cameraTransform);

            var contentLength = content.Count;

            for(var j = 0; j < contentLength; j++)
            {
                if (content[j].Item3 is Renderable renderable)
                {
                    renderable.isVisible = renderable.enabled &&
                        renderable.forceRenderingOff == false &&
                        renderable.cullingState != CullingState.Invisible;

                    if(renderable.isVisible && cull)
                    {
                        if(renderable.cullingState == CullingState.None)
                        {
                            renderable.isVisible = camera.IsVisible(renderable.bounds);

                            renderable.cullingState = renderable.isVisible ? CullingState.Visible : CullingState.Invisible;
                        }
                    }

                    if (renderable.isVisible == false)
                    {
                        RenderStats.culledDrawCalls++;
                    }
                }
            }

            system.Process(CollectionsMarshal.AsSpan(content), camera, cameraTransform, viewID);

            system.Submit(viewID);
        }
    }

    /// <summary>
    /// Renders a single entity
    /// </summary>
    /// <param name="cameraEntity">The camera's entity</param>
    /// <param name="camera">The camera</param>
    /// <param name="cameraTransform">The camera's transform</param>
    /// <param name="entity">The entity to render</param>
    /// <param name="entityTransform">The transform of the entity to render</param>
    /// <param name="cull">Whether to cull invisible elements</param>
    /// <param name="viewID">The view ID</param>
    public void RenderEntity(Entity cameraEntity, Camera camera, Transform cameraTransform,
        Entity entity, Transform entityTransform, bool cull, ushort viewID)
    {
        usedViewIDs.Add(viewID);

        using var p1 = new PerformanceProfiler(PerformanceProfilerType.Rendering);

        var c = (CurrentCamera.Item1, CurrentCamera.Item2);

        CurrentCamera = (camera, cameraTransform);

        var systems = new List<IRenderSystem>();

        lock (lockObject)
        {
            systems.AddRange(renderSystems);
        }

        foreach (var system in systems)
        {
            if (system.UsesOwnRenderProcess)
            {
                continue;
            }

            system.Prepare();
        }

        PrepareCamera(cameraEntity, camera, cameraTransform, viewID);

        var systemQueues = new Dictionary<IRenderSystem, List<(Entity, Transform, IComponent)>>();

        void Handle(Entity e, Transform t)
        {
            foreach (var system in systems)
            {
                if (system.UsesOwnRenderProcess)
                {
                    continue;
                }

                if(systemQueues.TryGetValue(system, out var queue) == false)
                {
                    queue = [];

                    systemQueues.Add(system, queue);
                }

                if (system.RelatedComponent != null &&
                    e.TryGetComponent(system.RelatedComponent, out var related))
                {
                    system.Preprocess([(e, t, related)], camera, cameraTransform);

                    if (related is Renderable renderable)
                    {
                        renderable.isVisible = renderable.enabled && renderable.forceRenderingOff == false;

                        if (renderable.isVisible && cull)
                        {
                            renderable.isVisible = renderable.isVisible && camera.IsVisible(renderable.bounds);

                            if (renderable.isVisible == false)
                            {
                                RenderStats.culledDrawCalls++;
                            }
                        }
                    }

                    queue.Add((e, t, related));
                }
            }

            foreach(var child in t.Children)
            {
                Handle(child.Entity, child);
            }
        }

        Handle(entity, entityTransform);

        foreach(var pair in systemQueues)
        {
            pair.Key.Process(CollectionsMarshal.AsSpan(pair.Value), camera, cameraTransform, viewID);

            pair.Key.Submit(viewID);
        }

        CurrentCamera = (c.Item1, c.Item2);
    }

    /// <summary>
    /// Render with the drawcall accumulator (interpolator)
    /// </summary>
    /// <param name="cameraEntity">The camera's entity</param>
    /// <param name="camera">The camera</param>
    /// <param name="cameraTransform">The camera's transform</param>
    /// <param name="viewID">The view ID</param>
    public void RenderAccumulator(Entity cameraEntity, Camera camera, Transform cameraTransform, ushort viewID)
    {
        usedViewIDs.Add(viewID);

        CurrentCamera = (camera, cameraTransform);

        var systems = new List<IRenderSystem>();

        lock(lockObject)
        {
            systems.AddRange(renderSystems);
        }

        foreach (var system in systems)
        {
            if (system.UsesOwnRenderProcess)
            {
                continue;
            }

            system.Prepare();
        }

        PrepareCamera(cameraEntity, camera, cameraTransform, viewID);

        var alpha = accumulator / Time.fixedDeltaTime;

        lock (lockObject)
        {
            if (currentDrawBucket.drawCalls.TryGetValue(viewID, out var drawCalls) && previousDrawBucket.drawCalls.TryGetValue(viewID, out var previousDrawCalls))
            {
                foreach (var call in drawCalls)
                {
                    var previous = previousDrawCalls.Find(x => x.entity.Identifier == call.entity.Identifier);

                    if (call.renderable.isVisible)
                    {
                        var currentPosition = call.position;
                        var currentRotation = call.rotation;
                        var currentScale = call.scale;

                        if (previous == null)
                        {
                            stagingTransform.LocalPosition = currentPosition;
                            stagingTransform.LocalRotation = currentRotation;
                            stagingTransform.LocalScale = currentScale;
                        }
                        else
                        {
                            var previousPosition = previous.position;
                            var previousRotation = previous.rotation;
                            var previousScale = previous.scale;

                            stagingTransform.LocalPosition = Vector3.Lerp(previousPosition, currentPosition, alpha);
                            stagingTransform.LocalRotation = Quaternion.Lerp(previousRotation, currentRotation, alpha);
                            stagingTransform.LocalScale = Vector3.Lerp(previousScale, currentScale, alpha);
                        }

                        foreach (var system in systems)
                        {
                            if (call.relatedComponent.GetType() == system.RelatedComponent)
                            {
                                system.Process([(call.entity, stagingTransform, call.relatedComponent)],
                                    camera, cameraTransform, viewID);
                            }
                        }
                    }
                }
            }
        }

        foreach (var system in systems)
        {
            system.Submit(viewID);
        }
    }

    /// <summary>
    /// Update process for standard rendering
    /// </summary>
    private void UpdateStandard()
    {
        CurrentViewID = 1;

        using var profiler = new PerformanceProfiler(PerformanceProfilerType.Rendering);

        foreach (var pair in renderQueue)
        {
            RenderStandard(pair.Item1.Item2.Entity, pair.Item1.Item1, pair.Item1.Item2, pair.Item2, true, CurrentViewID++);
        }

        foreach(var (_, transform) in entityQuery.Contents)
        {
            transform.changedThisFrame = false;
        }
    }

    /// <summary>
    /// Update process for interpolator rendering
    /// </summary>
    private void UpdateAccumulator()
    {
        CurrentViewID = 1;

        using var profiler = new PerformanceProfiler(PerformanceProfilerType.Rendering);

        foreach (var pair in renderQueue)
        {
            RenderAccumulator(pair.Item1.Item2.Entity, pair.Item1.Item1, pair.Item1.Item2, CurrentViewID++);
        }

        foreach (var (_, transform) in entityQuery.Contents)
        {
            transform.changedThisFrame = false;
        }

        if (needsDrawCalls)
        {
            CurrentViewID = 1;

            lock (lockObject)
            {
                (currentDrawBucket, previousDrawBucket) = (previousDrawBucket, currentDrawBucket);

                currentDrawBucket.drawCalls.Clear();
            }

            foreach (var pair in renderQueue)
            {
                var camera = pair.Item1.Item1;
                var cameraTransform = pair.Item1.Item2;

                unsafe
                {
                    var projection = Camera.Projection(cameraTransform.Entity, camera);
                    var view = cameraTransform.Matrix;

                    Matrix4x4.Invert(view, out view);

                    camera.UpdateFrustum(view, projection);
                }

                foreach (var systemInfo in pair.Item2)
                {
                    var system = systemInfo.Item1;
                    var contents = systemInfo.Item2;

                    if(contents.Count == 0)
                    {
                        continue;
                    }

                    system.Preprocess(CollectionsMarshal.AsSpan(contents), camera, cameraTransform);

                    var contentLength = contents.Count;

                    for (var j = 0; j < contentLength; j++)
                    {
                        if (contents[j].Item3 is Renderable renderable)
                        {
                            renderable.isVisible = renderable.enabled &&
                                renderable.forceRenderingOff == false &&
                                renderable.cullingState != CullingState.Invisible;

                            if(renderable.isVisible)
                            {
                                if (renderable.cullingState == CullingState.None)
                                {
                                    renderable.isVisible = camera.IsVisible(renderable.bounds);

                                    renderable.cullingState = renderable.isVisible ? CullingState.Visible : CullingState.Invisible;
                                }

                                if (renderable.isVisible)
                                {
                                    AddDrawCall(contents[j].Item1, contents[j].Item2, contents[j].Item3, renderable, CurrentViewID);
                                }
                                else
                                {
                                    RenderStats.culledDrawCalls++;
                                }
                            }
                        }
                    }
                }

                CurrentViewID++;
            }
        }

        if (needsDrawCalls)
        {
            needsDrawCalls = false;
        }

        accumulator = Time.accumulator;
    }
    #endregion

    #region Render Helpers
    /// <summary>
    /// Prepares a camera for rendering
    /// </summary>
    /// <param name="entity">The camera's entity</param>
    /// <param name="camera">The camera</param>
    /// <param name="cameraTransform">The camera's transform</param>
    /// <param name="viewID">The view ID</param>
    private static void PrepareCamera(Entity entity, Camera camera, Transform cameraTransform, ushort viewID)
    {
        unsafe
        {
            var projection = Camera.Projection(entity, camera);
            var view = cameraTransform.Matrix;

            Matrix4x4.Invert(view, out view);

            bgfx.set_view_transform(viewID, &view, &projection);

            camera.UpdateFrustum(view, projection);
        }

        switch (camera.clearMode)
        {
            case CameraClearMode.Depth:
                bgfx.set_view_clear(viewID, (ushort)bgfx.ClearFlags.Depth, 0, 1, 0);

                break;

            case CameraClearMode.None:
                bgfx.set_view_clear(viewID, (ushort)bgfx.ClearFlags.None, 0, 1, 0);

                break;

            case CameraClearMode.SolidColor:
                bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), camera.clearColor.UIntValue, 1, 0);

                break;
        }

        var viewMode = camera.viewMode switch
        {
            CameraViewMode.Default => bgfx.ViewMode.Default,
            CameraViewMode.Sequential => bgfx.ViewMode.Sequential,
            CameraViewMode.DepthAscending => bgfx.ViewMode.DepthAscending,
            CameraViewMode.DepthDescending => bgfx.ViewMode.DepthDescending,
            _ => bgfx.ViewMode.Default,
        };

        bgfx.set_view_mode(viewID, viewMode);

        bgfx.set_view_rect(viewID, (ushort)camera.viewport.X, (ushort)camera.viewport.Y,
            (ushort)(camera.viewport.Z * Screen.Width), (ushort)(camera.viewport.W * Screen.Height));

        bgfx.touch(viewID);
    }

    /// <summary>
    /// Adds a drawcall to the drawcall list
    /// </summary>
    /// <param name="entity">The entity to draw</param>
    /// <param name="transform">The entity's transform</param>
    /// <param name="relatedComponent">The entity's related component</param>
    /// <param name="renderable">The entity's Renderable</param>
    /// <param name="viewID">The current view ID</param>
    private void AddDrawCall(Entity entity, Transform transform, IComponent relatedComponent, Renderable renderable, ushort viewID)
    {
        lock (lockObject)
        {
            if (currentDrawBucket.drawCalls.TryGetValue(viewID, out var drawCalls) == false)
            {
                drawCalls = [];

                currentDrawBucket.drawCalls.Add(viewID, drawCalls);
            }

            drawCalls.Add(new()
            {
                entity = entity,
                renderable = renderable,
                position = transform.Position,
                rotation = transform.Rotation,
                scale = transform.Scale,
                relatedComponent = relatedComponent,
            });
        }
    }

    internal static void Submit(ushort viewID, bgfx.ProgramHandle program, bgfx.DiscardFlags flags, int triangles, int instances)
    {
        bgfx.submit(viewID, program, 0, (byte)flags);

        RenderStats.drawCalls++;
        RenderStats.triangleCount += triangles * instances;

        if (instances > 1)
        {
            RenderStats.savedDrawCalls += instances;
        }

    }

    #endregion
}
