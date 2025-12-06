using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Rendering subsystem, handles all rendering
/// </summary>
public sealed partial class RenderSystem : ISubsystem, IWorldChangeReceiver
{
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
                if (s == system || s.GetType() == system.GetType())
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

            renderSystems.Add(system);
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
                if(s is T instance)
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
        List<(IRenderSystem, List<(Entity, Transform, IComponent)>)> queue, bool cull)
    {
        CurrentCamera = (camera, cameraTransform);

        PrepareCamera(cameraEntity, camera, cameraTransform);

        var queueLength = queue.Count;

        for (var i = 0; i < queueLength; i++)
        {
            var (system, content) = queue[i];

            if (content.Count == 0)
            {
                continue;
            }

            system.Prepare();

            system.Preprocess(CollectionsMarshal.AsSpan(content), camera, cameraTransform);

            var contentLength = content.Count;

            for (var j = 0; j < contentLength; j++)
            {
                if (content[j].Item3 is Renderable renderable)
                {
                    renderable.isVisible = renderable.enabled &&
                        renderable.forceRenderingOff == false &&
                        renderable.cullingState != CullingState.Invisible;

                    if (renderable.isVisible && cull)
                    {
                        if (renderable.cullingState == CullingState.None)
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

            system.Process(CollectionsMarshal.AsSpan(content), camera, cameraTransform);
        }

        for (var i = 0; i < queueLength; i++)
        {
            var (system, content) = queue[i];

            if (content.Count == 0)
            {
                continue;
            }

            system.Submit();
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

        var systemQueues = new Dictionary<IRenderSystem, List<(Entity, Transform, IComponent)>>();

        void Handle(Entity e, Transform t)
        {
            foreach (var system in systems)
            {
                if (system.UsesOwnRenderProcess)
                {
                    continue;
                }

                if (systemQueues.TryGetValue(system, out var queue) == false)
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

            foreach (var child in t.Children)
            {
                Handle(child.Entity, child);
            }
        }

        Handle(entity, entityTransform);

        PrepareCamera(cameraEntity, camera, cameraTransform);

        foreach (var pair in systemQueues)
        {
            pair.Key.Process(CollectionsMarshal.AsSpan(pair.Value), camera, cameraTransform);

            pair.Key.Submit();
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

        var alpha = accumulator / Time.fixedDeltaTime;

        lock (lockObject)
        {
            foreach (var call in currentDrawBucket.drawCalls)
            {
                var previous = previousDrawBucket.drawCalls.Find(x => x.entity.Identifier == call.entity.Identifier);

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
                                camera, cameraTransform);
                        }
                    }
                }
            }
        }

        PrepareCamera(cameraEntity, camera, cameraTransform);

        foreach (var system in systems)
        {
            system.Submit();
        }
    }
}
