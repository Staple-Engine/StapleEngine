using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Rendering subsystem, handles all rendering
/// </summary>
public sealed partial class RenderSystem : ISubsystem, IWorldChangeReceiver
{
    /// <summary>
    /// Contains information on a render system and its capabilities
    /// </summary>
    public readonly struct RenderSystemInfo(IRenderSystem system, bool isRenderable)
    {
        public readonly IRenderSystem system = system;
        public readonly bool isRenderable = isRenderable;

        public override int GetHashCode()
        {
            return HashCode.Combine(system, isRenderable);
        }

        public override bool Equals([NotNullWhen(true)] object obj)
        {
            return obj is RenderSystemInfo info && info.system == system && info.isRenderable == isRenderable;
        }

        public static bool operator ==(RenderSystemInfo left, RenderSystemInfo right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(RenderSystemInfo left, RenderSystemInfo right)
        {
            return !(left == right);
        }
    }

    public SubsystemType type { get; } = SubsystemType.Update;

    /// <summary>
    /// Whether to use a drawcall interpolator. Can allow for small ticks of updates causing smooth rendering.
    /// </summary>
    public static bool UseDrawcallInterpolator = false;

    /// <summary>
    /// Rendering statistics
    /// </summary>
    public static readonly RenderStats RenderStats = new();

    /// <summary>
    /// The current frame being rendered
    /// </summary>
    public static uint CurrentFrame { get; private set; }

    /// <summary>
    /// The current camera
    /// </summary>
    public static (Camera, Transform) CurrentCamera { get; internal set; }

    /// <summary>
    /// The instance of this render system
    /// </summary>
    public static readonly RenderSystem Instance = new();

    /// <summary>
    /// Registers a render system into this subsystem
    /// </summary>
    /// <param name="system">The system to add</param>
    public void RegisterSystem(IRenderSystem system)
    {
        lock (lockObject)
        {
            foreach (var s in renderSystems)
            {
                if (s.system == system || s.system.GetType() == system.GetType())
                {
                    return;
                }
            }

            try
            {
                system.Startup();
            }
            catch (Exception e)
            {
                Log.Error($"[RenderSystem] Failed to initialize {system.GetType().FullName}: {e}");

                return;
            }

            if (system is IWorldChangeReceiver receiver)
            {
                World.AddChangeReceiver(receiver);
            }

            renderSystems.Add(new(system, system.RelatedComponent != null &&
                (system.RelatedComponent.IsSubclassOf(typeof(Renderable)) ||
                system.RelatedComponent == typeof(Renderable))));
        }
    }

    /// <summary>
    /// Gets a registered render system. This render system must have been registered previously.
    /// </summary>
    /// <typeparam name="T">The render system type</typeparam>
    /// <returns>The system, or default</returns>
    public T Get<T>() where T: IRenderSystem
    {
        lock(lockObject)
        {
            foreach(var s in renderSystems)
            {
                if(s.system is T instance)
                {
                    return instance;
                }
            }
        }

        return default;
    }

    /// <summary>
    /// Renders with specific camera info
    /// </summary>
    /// <param name="target">The render target, if any</param>
    /// <param name="clearMode">How to clear the target</param>
    /// <param name="clearColor">The color to clear if clearMode is <see cref="CameraClearMode.SolidColor"/></param>
    /// <param name="viewport">The viewport area to render to (normalized coordinates for x, y, width, height)</param>
    /// <param name="cameraTransform">The transform of the camera</param>
    /// <param name="projection">The projection matrix</param>
    /// <param name="callback">A callback to render the content</param>
    public static void Render(RenderTarget target, CameraClearMode clearMode, Color clearColor, Vector4 viewport,
        Matrix4x4 cameraTransform, Matrix4x4 projection, Action callback)
    {
        PrepareRender(target, clearMode, clearColor, viewport, cameraTransform, projection);

        callback?.Invoke();
    }

    /// <summary>
    /// Renders in the standard mode (no interpolator)
    /// </summary>
    /// <param name="cameraEntity">The camera's entity</param>
    /// <param name="camera">The camera</param>
    /// <param name="cameraTransform">The camera's transform</param>
    /// <param name="queue">The render queue for this camera</param>
    /// <param name="cull">Whether to cull invisible elements</param>
    public static void RenderStandard(Entity cameraEntity, Camera camera, Transform cameraTransform,
        List<(RenderSystemInfo, List<RenderEntry>)> queue, bool cull)
    {
        CurrentCamera = (camera, cameraTransform);

        PrepareCamera(cameraEntity, camera, cameraTransform);

        var queueLength = queue.Count;

        for (var i = 0; i < queueLength; i++)
        {
            var (systemInfo, content) = queue[i];

            if (content.Count == 0)
            {
                continue;
            }

            systemInfo.system.Prepare();

            systemInfo.system.Preprocess(CollectionsMarshal.AsSpan(content), camera, cameraTransform);

            if(systemInfo.isRenderable)
            {
                var contentLength = content.Count;

                for (var j = 0; j < contentLength; j++)
                {
                    var renderable = (Renderable)content[j].component;

                    renderable.isVisible = renderable.enabled &&
                        !renderable.forceRenderingOff &&
                        renderable.cullingState != CullingState.Invisible;

                    if (renderable.isVisible && cull)
                    {
                        if (renderable.cullingState == CullingState.None)
                        {
                            renderable.isVisible = camera.IsVisible(renderable.bounds);

                            renderable.cullingState = renderable.isVisible ? CullingState.Visible : CullingState.Invisible;
                        }
                    }

                    if (!renderable.isVisible)
                    {
                        RenderStats.culledDrawCalls++;
                    }
                }
            }

            systemInfo.system.Process(CollectionsMarshal.AsSpan(content), camera, cameraTransform);
        }

        for (var i = 0; i < queueLength; i++)
        {
            var (systemInfo, content) = queue[i];

            if (content.Count == 0)
            {
                continue;
            }

            systemInfo.system.Submit();
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
    public void RenderEntity(Entity cameraEntity, Camera camera, Transform cameraTransform,
        Entity entity, Transform entityTransform, bool cull)
    {
        using var p1 = new PerformanceProfiler(PerformanceProfilerType.Rendering);

        var c = (CurrentCamera.Item1, CurrentCamera.Item2);

        CurrentCamera = (camera, cameraTransform);

        var systems = new List<RenderSystemInfo>();

        lock (lockObject)
        {
            systems.AddRange(renderSystems);
        }

        foreach (var systemInfo in systems)
        {
            if (systemInfo.system.UsesOwnRenderProcess)
            {
                continue;
            }

            systemInfo.system.Prepare();
        }

        var systemQueues = new Dictionary<RenderSystemInfo, List<RenderEntry>>();

        void Handle(Entity e, Transform t)
        {
            foreach (var systemInfo in systems)
            {
                if (systemInfo.system.UsesOwnRenderProcess)
                {
                    continue;
                }

                if (!systemQueues.TryGetValue(systemInfo, out var queue))
                {
                    queue = [];

                    systemQueues.Add(systemInfo, queue);
                }

                if (systemInfo.system.RelatedComponent == null ||
                    !e.TryGetComponent(systemInfo.system.RelatedComponent, out var related))
                {
                    continue;
                }
                
                systemInfo.system.Preprocess([new(e, t, related)], camera, cameraTransform);

                if (systemInfo.isRenderable)
                {
                    var renderable = (Renderable)related;

                    renderable.isVisible = renderable.enabled && !renderable.forceRenderingOff;

                    if (renderable.isVisible && cull)
                    {
                        renderable.isVisible = renderable.isVisible && camera.IsVisible(renderable.bounds);

                        if (!renderable.isVisible)
                        {
                            RenderStats.culledDrawCalls++;
                        }
                    }
                }

                queue.Add(new(e, t, related));
            }

            foreach (var child in t.Children)
            {
                Handle(child.Entity, child);
            }
        }

        Handle(entity, entityTransform);

        PrepareCamera(cameraEntity, camera, cameraTransform);

        foreach (var pair in systemQueues)
        {
            pair.Key.system.Process(CollectionsMarshal.AsSpan(pair.Value), camera, cameraTransform);

            pair.Key.system.Submit();
        }

        CurrentCamera = (c.Item1, c.Item2);
    }

    /// <summary>
    /// Render with the drawcall accumulator (interpolator)
    /// </summary>
    /// <param name="cameraEntity">The camera's entity</param>
    /// <param name="camera">The camera</param>
    /// <param name="cameraTransform">The camera's transform</param>
    public void RenderAccumulator(Entity cameraEntity, Camera camera, Transform cameraTransform)
    {
        CurrentCamera = (camera, cameraTransform);

        var systems = new List<RenderSystemInfo>();

        lock (lockObject)
        {
            systems.AddRange(renderSystems);
        }

        foreach (var systemInfo in systems)
        {
            if (systemInfo.system.UsesOwnRenderProcess)
            {
                continue;
            }

            systemInfo.system.Prepare();
        }

        var alpha = accumulator / Time.fixedDeltaTime;

        lock (lockObject)
        {
            foreach (var call in currentDrawBucket.drawCalls)
            {
                var previous = previousDrawBucket.drawCalls.Find(x => x.entity.Identifier == call.entity.Identifier);

                if (!call.renderable.isVisible)
                {
                    continue;
                }
                
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

                foreach (var systemInfo in systems)
                {
                    if (call.relatedComponent.GetType() == systemInfo.system.RelatedComponent)
                    {
                        systemInfo.system.Process([new(call.entity, stagingTransform, call.relatedComponent)],
                            camera, cameraTransform);
                    }
                }
            }
        }

        PrepareCamera(cameraEntity, camera, cameraTransform);

        foreach (var systemInfo in systems)
        {
            systemInfo.system.Submit();
        }
    }
}
