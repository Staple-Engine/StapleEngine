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
    public readonly struct RenderSystemInfo(RenderSystemBase system, bool isRenderable)
    {
        public readonly RenderSystemBase system = system;
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
    public static (Camera camera, Transform transform) CurrentCamera { get; internal set; }

    /// <summary>
    /// The instance of this render system
    /// </summary>
    public static readonly RenderSystem Instance = new();

    /// <summary>
    /// Gets the render queue from a render index
    /// </summary>
    /// <param name="renderIndex">The render index</param>
    /// <returns>The render queue</returns>
    public static MaterialRenderQueue GetRenderQueue(int renderIndex)
    {
        if(renderIndex < (int)MaterialRenderQueue.AlphaTest)
        {
            return MaterialRenderQueue.Opaque;
        }
        else if (renderIndex < (int)MaterialRenderQueue.Transparent)
        {
            return MaterialRenderQueue.AlphaTest;
        }
        else if(renderIndex < (int)MaterialRenderQueue.Overlay)
        {
            return MaterialRenderQueue.Transparent;
        }

        return MaterialRenderQueue.Overlay;
    }

    /// <summary>
    /// Gets the sort mode for a specific render queue
    /// </summary>
    /// <param name="renderQueue">The render queue</param>
    /// <returns>The sort mode</returns>
    public static RenderableSortMode RenderQueueSortMode(MaterialRenderQueue renderQueue)
    {
        return renderQueue switch
        {
            MaterialRenderQueue.Opaque => RenderableSortMode.FrontToBack,
            _ => RenderableSortMode.BackToFront,
        };
    }

    /// <summary>
    /// Registers a render system into this subsystem
    /// </summary>
    /// <param name="system">The system to add</param>
    public void RegisterSystem(RenderSystemBase system)
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
                Log.Error($"Failed to initialize {system.GetType().FullName}: {e}", LogTag);

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
    public T Get<T>() where T: RenderSystemBase
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
        var previous = RenderTarget.Current;

        RenderTarget.Current = target;

        PrepareRender(target, clearMode, clearColor, viewport, cameraTransform, projection);

        callback?.Invoke();

        RenderTarget.Current = previous;
    }

    /// <summary>
    /// Renders in the standard mode (no interpolator)
    /// </summary>
    /// <param name="set">The camera's entity</param>
    /// <param name="cull">Whether to cull invisible elements</param>
    public void RenderStandard(RenderSystemCameraSet set, bool cull)
    {
        CurrentCamera = (set.camera, set.transform);

        PrepareCamera(set.transform.Entity, set.camera, set.transform);

        if (visibilityCheckCounter > 0)
        {
            visibilityCheckCounter--;
        }

        var shouldCheckVisibility = visibilityCheckCounter == 0;

        if (shouldCheckVisibility)
        {
            visibilityCheckCounter = MaxFramesBetweenVisibilityChecks;

            ClearCullingStates();

            var renderables = this.renderables.Contents;

            foreach (var pair in spatialEntities)
            {
                var result = set.camera.IsSpatialNodeVisible(pair.Key, false);

                var span = CollectionsMarshal.AsSpan(pair.Value);

                switch (result)
                {
                    //Since we can process nodes in any way, it's possible for an entity to be visible then invisible and vice versa.
                    //Ensure it's marked as visible if it's visible at any point.
                    case CullingState.Visible:

                        foreach (var transform in span)
                        {
                            if (visibleEntities.Contains(transform.Entity))
                            {
                                continue;
                            }

                            var renderable = renderables[transform.Entity.Identifier.ID - 1];

                            if (renderable == null || renderable.cullingState != CullingState.Invisible) //No work, not invisible yet!
                            {
                                continue;
                            }

                            //Ensure marked as visible
                            renderable.cullingState = CullingState.Visible;

                            visibleEntities.Add(transform.Entity);
                        }

                        break;

                    case CullingState.Invisible:

                        foreach (var transform in span)
                        {
                            if (visibleEntities.Contains(transform.Entity))
                            {
                                continue;
                            }

                            var renderable = renderables[transform.Entity.Identifier.ID - 1];

                            if (renderable == null || renderable.cullingState == CullingState.Visible) //Already passed a visible test!
                            {
                                continue;
                            }

                            renderable.isVisible = false;

                            //Ensure marked as invisible
                            renderable.cullingState = CullingState.Invisible;
                        }

                        break;
                }
            }
        }

        foreach (var renderIndex in set.renderIndices)
        {
            foreach (var system in set.renderSystems.Contents)
            {
                if (!system.queue.TryGetValue(renderIndex, out var container) ||
                    container.queue.Empty)
                {
                    continue;
                }

                var queue = container.queue;

                if(container.transformTracker.ShouldUpdateComponent(CurrentCamera.transform.Entity, CurrentCamera.transform))
                {
                    var renderQueue = GetRenderQueue(renderIndex);

                    var sortMode = RenderQueueSortMode(renderQueue);

                    queue.Sort(CurrentCamera.transform.Position, sortMode);
                }

                system.renderSystem.system.Prepare();

                system.renderSystem.system.Preprocess(queue);

                if (system.renderSystem.isRenderable)
                {
                    queue.IterateRenderables((entity, transform, renderable) =>
                    {
                        if (renderable.cullingState == CullingState.Invisible)
                        {
                            RenderStats.culledDrawCalls++;

                            return;
                        }

                        if (shouldCheckVisibility)
                        {
                            renderable.isVisible = renderable.enabled &&
                                !renderable.forceRenderingOff;

                            if (renderable.isVisible && cull)
                            {
                                if (renderable.cullingState == CullingState.None)
                                {
                                    renderable.isVisible = set.camera.IsVisible(renderable.bounds);

                                    renderable.cullingState = renderable.isVisible ? CullingState.Visible : CullingState.Invisible;
                                }
                            }
                        }

                        if (!renderable.isVisible)
                        {
                            RenderStats.culledDrawCalls++;
                        }
                    });
                }

                system.renderSystem.system.Process(queue, set.camera, set.transform, renderIndex);

                system.renderSystem.system.Submit();
            }
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

        var c = CurrentCamera;

        CurrentCamera = (camera, cameraTransform);

        ClearCullingStates();

        var systems = new List<RenderSystemRenderQueue>();
        var renderIndices = new HashSet<int>();

        lock (lockObject)
        {
            foreach(var system in renderSystems)
            {
                systems.Add(new()
                {
                    renderSystem = system,
                });
            }
        }

        foreach (var systemInfo in systems)
        {
            if (systemInfo.renderSystem.system.UsesOwnRenderProcess)
            {
                continue;
            }
        }

        void Handle(Entity e, Transform t)
        {
            if(!camera.cullingLayers.HasLayer(e.Layer))
            {
                foreach (var child in t.Children)
                {
                    Handle(child.Entity, child);
                }

                return;
            }

            foreach (var systemInfo in systems)
            {
                if (systemInfo.renderSystem.system.UsesOwnRenderProcess ||
                    !systemInfo.renderSystem.isRenderable ||
                    !e.TryGetComponent(systemInfo.renderSystem.system.RelatedComponent, out var component))
                {
                    continue;
                }

                var renderable = (Renderable)component;

                foreach (var material in renderable.materials)
                {
                    if (!(material?.IsValid ?? false))
                    {
                        continue;
                    }

                    var priority = material.RenderQueueIndex;

                    if (!systemInfo.queue.TryGetValue(priority, out var container) ||
                        container.queue.GetType() != systemInfo.renderSystem.system.QueueType)
                    {
                        container ??= new();

                        container.queue = systemInfo.renderSystem.system.CreateRenderQueue();

                        systemInfo.queue.AddOrSetKey(priority, container);
                    }

                    renderIndices.Add(priority);

                    container.queue.Add(e, t, renderable);
                }
            }

            foreach (var child in t.Children)
            {
                Handle(child.Entity, child);
            }
        }

        Handle(entity, entityTransform);

        foreach(var renderIndex in renderIndices)
        {
            foreach (var system in systems)
            {
                if (system.renderSystem.system.UsesOwnRenderProcess ||
                    !system.queue.TryGetValue(renderIndex, out var container) ||
                    container.queue.Empty)
                {
                    continue;
                }

                var queue = container.queue;

                //Assume RenderEntity is never gonna be able to use cached render orders
                var renderQueue = GetRenderQueue(renderIndex);

                var sortMode = RenderQueueSortMode(renderQueue);

                queue.Sort(CurrentCamera.transform.Position, sortMode);

                system.renderSystem.system.Prepare();

                system.renderSystem.system.Preprocess(queue);

                if (system.renderSystem.isRenderable)
                {
                    queue.IterateRenderables((entity, transform, renderable) =>
                    {
                        if (renderable.cullingState == CullingState.Invisible)
                        {
                            RenderStats.culledDrawCalls++;

                            return;
                        }

                        renderable.isVisible = true;
                        renderable.cullingState = CullingState.Visible;

                        if (!renderable.isVisible)
                        {
                            RenderStats.culledDrawCalls++;
                        }
                    });
                }

                system.renderSystem.system.Process(queue, camera, cameraTransform, renderIndex);

                system.renderSystem.system.Submit();
            }
        }

        CurrentCamera = c;
    }
}
