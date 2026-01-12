using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
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
    /// Contains lists of drawcalls for the drawcall interpolator
    /// </summary>
    internal class DrawBucket
    {
        public readonly List<DrawCall> drawCalls = [];
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
    private bool needsDrawCalls;

    /// <summary>
    /// Time accumulator (interpolator only)
    /// </summary>
    private float accumulator;

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
    private readonly List<((Camera, Transform), List<(RenderSystemInfo, List<RenderEntry>)>)> renderQueue = [];

    /// <summary>
    /// The entity query for every entity with a transform
    /// </summary>
    internal readonly SceneQuery<Transform> entityQuery = new();

    /// <summary>
    /// All transforms of each entity
    /// </summary>
    internal Matrix4x4[] entityTransforms = new Matrix4x4[1024];

    /// <summary>
    /// Tracker for each entity's transform
    /// </summary>
    private readonly ComponentVersionTracker<Transform> entityTransformTracker = new();

    /// <summary>
    /// Tracks all changed entity transforms in ranges (key is start index, value is length)
    /// </summary>
    internal readonly Dictionary<int, int> changedEntityTransformRanges = [];

    /// <summary>
    /// All renderables
    /// </summary>
    private Renderable[] renderables = new Renderable[1024];

    /// <summary>
    /// Amount of renderables used
    /// </summary>
    private int renderableCount;

    /// <summary>
    /// The renderer backend
    /// </summary>
    internal static readonly IRendererBackend Backend = new SDLGPURendererBackend();
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
    /// Called at the end of each frame
    /// </summary>
    /// <param name="frame">The current frame</param>
    internal void OnFrame(uint frame)
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
        RegisterSystem(new UICanvasSystem());

        LightSystem.Enabled = AppSettings.Current?.enableLighting ?? true;

        Time.onAccumulatorFinished += () =>
        {
            needsDrawCalls = true;
        };
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
        if (World.Current == null)
        {
            return;
        }

        RenderStats.Clear();

        ClearCullingStates();

        UpdateEntityTransforms();

        foreach (var systemInfo in renderSystems)
        {
            if(!systemInfo.system.UsesOwnRenderProcess)
            {
                continue;
            }

            systemInfo.system.Prepare();
        }

        if (UseDrawcallInterpolator)
        {
            UpdateAccumulator();
        }
        else
        {
            UpdateStandard();
        }

        foreach (var systemInfo in renderSystems)
        {
            if (!systemInfo.system.UsesOwnRenderProcess)
            {
                continue;
            }

            systemInfo.system.Submit();
        }
    }

    /// <summary>
    /// Clears the culling states of the entire render queue
    /// </summary>
    internal void ClearCullingStates()
    {
        for (var i = 0; i < renderableCount; i++)
        {
            renderables[i].cullingState = CullingState.None;
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
                if (renderSystems[i].system.GetType().Assembly != assembly)
                {
                    continue;
                }
                
                renderSystems[i].system.Shutdown();

                renderSystems.RemoveAt(i);
            }
        }
    }

    private void UpdateEntityTransforms()
    {
        var startIndex = -1;
        var length = 0;

        changedEntityTransformRanges.Clear();

        for (var i = 0; i < entityQuery.Length; i++)
        {
            var (entity, transform) = entityQuery[i];

            if (!entityTransformTracker.ShouldUpdateComponent(entity, in transform))
            {
                if (startIndex < 0)
                {
                    continue;
                }

                changedEntityTransformRanges.Add(startIndex, length);

                startIndex = -1;

                continue;
            }

            entityTransforms[i] = transform.Matrix;

            if (startIndex < 0)
            {
                startIndex = i;
                length = 1;
            }
            else
            {
                length++;
            }
        }

        if (startIndex >= 0)
        {
            changedEntityTransformRanges.Add(startIndex, length);
        }
    }

    public void WorldChanged()
    {
        lock (lockObject)
        {
            renderQueue.Clear();

            renderableCount = 0;

            if (entityQuery.Length > entityTransforms.Length)
            {
                var newSize = entityTransforms.Length * 2;

                while (newSize < entityQuery.Length)
                {
                    newSize *= 2;
                }
                
                Array.Resize(ref entityTransforms, newSize);
            }

            foreach (var systemInfo in renderSystems)
            {
                if(!systemInfo.isRenderable)
                {
                    continue;
                }

                foreach (var entityInfo in entityQuery.Contents)
                {
                    if (!entityInfo.Item1.TryGetComponent(systemInfo.system.RelatedComponent, out var component))
                    {
                        continue;
                    }
                    
                    renderableCount++;

                    if(renderableCount > renderables.Length)
                    {
                        Array.Resize(ref renderables, renderables.Length * 2);
                    }

                    renderables[renderableCount - 1] = (Renderable)component;
                }
            }

            var cameras = World.Current.SortedCameras;

            if (cameras.Length <= 0)
            {
                return;
            }
        
            foreach (var cameraInfo in cameras)
            {
                var collected = new Dictionary<RenderSystemInfo, List<RenderEntry>>();

                foreach (var entityInfo in entityQuery.Contents)
                {
                    var layer = entityInfo.Item1.Layer;

                    if (!cameraInfo.camera.cullingLayers.HasLayer(layer))
                    {
                        continue;
                    }

                    foreach (var systemInfo in renderSystems)
                    {
                        if(systemInfo.system.UsesOwnRenderProcess)
                        {
                            continue;
                        }

                        if (!collected.TryGetValue(systemInfo, out var content))
                        {
                            content = [];

                            collected.Add(systemInfo, content);
                        }

                        if (entityInfo.Item1.TryGetComponent(systemInfo.system.RelatedComponent, out var component))
                        {
                            content.Add(new(entityInfo.Item1, entityInfo.Item2, component));
                        }
                    }
                }

                var final = new List<(RenderSystemInfo, List<RenderEntry>)>();

                foreach(var pair in collected)
                {
                    final.Add((pair.Key, pair.Value));
                }

                renderQueue.Add(((cameraInfo.camera, cameraInfo.transform), final));
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

        foreach (var pair in renderQueue)
        {
            RenderStandard(pair.Item1.Item2.Entity, pair.Item1.Item1, pair.Item1.Item2, pair.Item2, true);
        }
    }

    /// <summary>
    /// Update process for interpolator rendering
    /// </summary>
    private void UpdateAccumulator()
    {
        using var profiler = new PerformanceProfiler(PerformanceProfilerType.Rendering);

        foreach (var pair in renderQueue)
        {
            RenderAccumulator(pair.Item1.Item2.Entity, pair.Item1.Item1, pair.Item1.Item2);
        }

        if (needsDrawCalls)
        {
            lock (lockObject)
            {
                (currentDrawBucket, previousDrawBucket) = (previousDrawBucket, currentDrawBucket);

                currentDrawBucket.drawCalls.Clear();
            }

            foreach (var pair in renderQueue)
            {
                var camera = pair.Item1.Item1;
                var cameraTransform = pair.Item1.Item2;

                var projection = Camera.Projection(cameraTransform.Entity, camera);
                var view = cameraTransform.Matrix;

                Matrix4x4.Invert(view, out view);

                camera.UpdateFrustum(view, projection);

                foreach (var (systemInfo, contents) in pair.Item2)
                {
                    if(contents.Count == 0)
                    {
                        continue;
                    }

                    systemInfo.system.Preprocess(CollectionsMarshal.AsSpan(contents), camera, cameraTransform);

                    if (!systemInfo.isRenderable)
                    {
                        continue;
                    }
                    
                    var contentLength = contents.Count;

                    for (var j = 0; j < contentLength; j++)
                    {
                        var renderable = (Renderable)contents[j].component;

                        renderable.isVisible = renderable.enabled &&
                            !renderable.forceRenderingOff &&
                            renderable.cullingState != CullingState.Invisible;

                        if (!renderable.isVisible)
                        {
                            continue;
                        }
                        
                        if (renderable.cullingState == CullingState.None)
                        {
                            renderable.isVisible = camera.IsVisible(renderable.bounds);

                            renderable.cullingState = renderable.isVisible ? CullingState.Visible : CullingState.Invisible;
                        }

                        if (renderable.isVisible)
                        {
                            AddDrawCall(contents[j].entity, contents[j].transform, contents[j].component, renderable);
                        }
                        else
                        {
                            RenderStats.culledDrawCalls++;
                        }
                    }
                }
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
    /// Adds a drawcall to the drawcall list
    /// </summary>
    /// <param name="entity">The entity to draw</param>
    /// <param name="transform">The entity's transform</param>
    /// <param name="relatedComponent">The entity's related component</param>
    /// <param name="renderable">The entity's Renderable</param>
    private void AddDrawCall(Entity entity, Transform transform, IComponent relatedComponent, Renderable renderable)
    {
        lock (lockObject)
        {
            currentDrawBucket.drawCalls.Add(new()
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

    internal static void Submit(RenderState state, int triangles, int instances)
    {
        Backend.Render(state);

        RenderStats.drawCalls++;
        RenderStats.triangleCount += triangles * instances;

        if (instances > 1)
        {
            RenderStats.savedDrawCalls += (instances - 1);
        }
    }
    #endregion
}
