using Bgfx;
using Staple.Internal;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple;

/// <summary>
/// Base Rendering subsystem
/// </summary>
internal class RenderSystem : ISubsystem
{
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
        public Dictionary<ushort, List<DrawCall>> drawCalls = new();
    }

    public SubsystemType type { get; } = SubsystemType.Update;

    public static bool useDrawcallInterpolator = false;

    /// <summary>
    /// Keep the current and previous draw buckets to interpolate around
    /// </summary>
    private DrawBucket previousDrawBucket = new(), currentDrawBucket = new();

    private readonly object lockObject = new();

    private readonly FrustumCuller frustumCuller = new();

    private bool needsDrawCalls = false;

    private float accumulator = 0.0f;

    internal readonly List<IRenderSystem> renderSystems = new();

    private readonly Transform stagingTransform = new();

    internal static byte Priority = 1;

    /// <summary>
    /// Calculates the blending function for blending flags
    /// </summary>
    /// <param name="source">The blending source flag</param>
    /// <param name="destination">The blending destination flag</param>
    /// <returns>The combined function</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ulong BlendFunction(bgfx.StateFlags source, bgfx.StateFlags destination)
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
    public static ulong BlendFunction(bgfx.StateFlags sourceColor, bgfx.StateFlags destinationColor, bgfx.StateFlags sourceAlpha, bgfx.StateFlags destinationAlpha)
    {
        return ((ulong)sourceColor | ((ulong)destinationColor << 4)) | (((ulong)sourceAlpha | ((ulong)destinationAlpha << 4)) << 8);
    }

    /// <summary>
    /// Checks if we have an available transient buffer
    /// </summary>
    /// <param name="numVertices">The amount of vertices to check</param>
    /// <param name="layout">The vertex layout to use</param>
    /// <param name="numIndices">The amount of indices to check</param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool CheckAvailableTransientBuffers(uint numVertices, bgfx.VertexLayout layout, uint numIndices)
    {
        unsafe
        {
            return numVertices == bgfx.get_avail_transient_vertex_buffer(numVertices, &layout)
                && (0 == numIndices || numIndices == bgfx.get_avail_transient_index_buffer(numIndices, false));
        }
    }

    public static bgfx.ResetFlags ResetFlags(VideoFlags videoFlags)
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

    public void Startup()
    {
        renderSystems.Add(new SpriteRenderSystem());
        renderSystems.Add(new MeshRenderSystem());
        renderSystems.Add(new TextRenderSystem());

        Time.OnAccumulatorFinished += () =>
        {
            needsDrawCalls = true;
        };
    }

    public void Shutdown()
    {
        foreach(var system in renderSystems)
        {
            system.Destroy();
        }
    }

    public void Update()
    {
        if(Scene.current?.world == null)
        {
            return;
        }

        if(useDrawcallInterpolator)
        {
            UpdateAccumulator();
        }
        else
        {
            UpdateStandard();
        }
    }

    private void UpdateStandard()
    {
        ushort viewID = 1;

        var cameras = Scene.current.world.SortedCameras;

        if (cameras.Length > 0)
        {
            foreach (var c in cameras)
            {
                var camera = c.camera;
                var cameraTransform = c.transform;

                foreach (var system in renderSystems)
                {
                    system.Prepare();
                }

                unsafe
                {
                    var projection = Camera.Projection(Scene.current.world, c.entity, c.camera);
                    var view = cameraTransform.Matrix;

                    Matrix4x4.Invert(view, out view);

                    bgfx.set_view_transform(viewID, &view, &projection);

                    frustumCuller.Update(view, projection);
                }

                switch (camera.clearMode)
                {
                    case CameraClearMode.Depth:
                        bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Depth), 0, 1, 0);

                        break;

                    case CameraClearMode.None:
                        bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.None), 0, 1, 0);

                        break;

                    case CameraClearMode.SolidColor:
                        bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), camera.clearColor.UIntValue, 1, 0);

                        break;
                }

                bgfx.set_view_rect(viewID, (ushort)camera.viewport.X, (ushort)camera.viewport.Y,
                    (ushort)(camera.viewport.Z * Screen.Width), (ushort)(camera.viewport.W * Screen.Height));

                bgfx.touch(viewID);

                Scene.current.world.ForEach((Entity entity, bool enabled, ref Transform t) =>
                {
                    if (enabled == false)
                    {
                        return;
                    }

                    var layer = Scene.current.world.GetEntityLayer(entity);

                    if (camera.cullingLayers.HasLayer(layer) == false)
                    {
                        return;
                    }

                    foreach (var system in renderSystems)
                    {
                        var related = Scene.current.world.GetComponent(entity, system.RelatedComponent());

                        if (related != null)
                        {
                            system.Preprocess(Scene.current.world, entity, t, related, camera, cameraTransform);

                            if (related is Renderable renderable &&
                                renderable.enabled)
                            {
                                renderable.isVisible = frustumCuller.AABBTest(renderable.bounds) != FrustumAABBResult.Invisible;

                                if (renderable.isVisible && renderable.forceRenderingOff == false)
                                {
                                    system.Process(Scene.current.world, entity, t, related, camera, cameraTransform, viewID);
                                }
                            }
                        }
                    }
                });

                foreach (var system in renderSystems)
                {
                    system.Submit();
                }

                viewID++;
            }
        }

        if (needsDrawCalls)
        {
            viewID = 1;

            lock (lockObject)
            {
                (currentDrawBucket, previousDrawBucket) = (previousDrawBucket, currentDrawBucket);

                currentDrawBucket.drawCalls.Clear();
            }

            foreach (var c in cameras)
            {
                var camera = c.camera;
                var cameraTransform = c.transform;

                unsafe
                {
                    var projection = Camera.Projection(Scene.current.world, c.entity, c.camera);
                    var view = cameraTransform.Matrix;

                    Matrix4x4.Invert(view, out view);

                    frustumCuller.Update(view, projection);
                }


                viewID++;
            }
        }
    }

    private void UpdateAccumulator()
    {
        ushort viewID = 1;

        var cameras = Scene.current.world.SortedCameras;

        if (cameras.Length > 0)
        {
            foreach (var c in cameras)
            {
                var camera = c.camera;
                var cameraTransform = c.transform;

                foreach (var system in renderSystems)
                {
                    system.Prepare();
                }

                unsafe
                {
                    var projection = Camera.Projection(Scene.current.world, c.entity, c.camera);
                    var view = cameraTransform.Matrix;

                    Matrix4x4.Invert(view, out view);

                    bgfx.set_view_transform(viewID, &view, &projection);

                    frustumCuller.Update(view, projection);
                }

                switch (camera.clearMode)
                {
                    case CameraClearMode.Depth:
                        bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Depth), 0, 1, 0);

                        break;

                    case CameraClearMode.None:
                        bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.None), 0, 1, 0);

                        break;

                    case CameraClearMode.SolidColor:
                        bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), camera.clearColor.UIntValue, 1, 0);

                        break;
                }

                bgfx.set_view_rect(viewID, (ushort)camera.viewport.X, (ushort)camera.viewport.Y,
                    (ushort)(camera.viewport.Z * Screen.Width), (ushort)(camera.viewport.W * Screen.Height));

                bgfx.touch(viewID);

                var alpha = accumulator / Time.fixedDeltaTime;

                lock (lockObject)
                {
                    if (currentDrawBucket.drawCalls.TryGetValue(viewID, out var drawCalls) && previousDrawBucket.drawCalls.TryGetValue(viewID, out var previousDrawCalls))
                    {
                        foreach (var call in drawCalls)
                        {
                            var previous = previousDrawCalls.Find(x => x.entity.ID == call.entity.ID);

                            if (call.renderable.enabled)
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

                                foreach (var system in renderSystems)
                                {
                                    if (call.relatedComponent.GetType() == system.RelatedComponent())
                                    {
                                        system.Process(Scene.current.world, call.entity, stagingTransform, call.relatedComponent,
                                            camera, cameraTransform, viewID);
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (var system in renderSystems)
                {
                    system.Submit();
                }

                viewID++;
            }
        }

        if (needsDrawCalls)
        {
            viewID = 1;

            lock (lockObject)
            {
                (currentDrawBucket, previousDrawBucket) = (previousDrawBucket, currentDrawBucket);

                currentDrawBucket.drawCalls.Clear();
            }

            foreach (var c in cameras)
            {
                var camera = c.camera;
                var cameraTransform = c.transform;

                unsafe
                {
                    var projection = Camera.Projection(Scene.current.world, c.entity, c.camera);
                    var view = cameraTransform.Matrix;

                    Matrix4x4.Invert(view, out view);

                    frustumCuller.Update(view, projection);
                }

                Scene.current.world.ForEach((Entity entity, bool enabled, ref Transform t) =>
                {
                    if (enabled == false)
                    {
                        return;
                    }

                    var layer = Scene.current.world.GetEntityLayer(entity);

                    if (camera.cullingLayers.HasLayer(layer) == false)
                    {
                        return;
                    }

                    foreach (var system in renderSystems)
                    {
                        var related = Scene.current.world.GetComponent(entity, system.RelatedComponent());

                        if (related != null)
                        {
                            system.Preprocess(Scene.current.world, entity, t, related, camera, cameraTransform);

                            if (related is Renderable renderable &&
                                renderable.enabled)
                            {
                                renderable.isVisible = frustumCuller.AABBTest(renderable.bounds) != FrustumAABBResult.Invisible;

                                if (renderable.isVisible && renderable.forceRenderingOff == false)
                                {
                                    AddDrawCall(entity, t, related, renderable, viewID);
                                }
                            }
                        }
                    }
                });

                viewID++;
            }
        }

        if (needsDrawCalls)
        {
            needsDrawCalls = false;
        }

        accumulator = Time.Accumulator;
    }

    /// <summary>
    /// Adds a drawcall to the drawcall list
    /// </summary>
    /// <param name="entity">The entity to draw</param>
    /// <param name="transform">The entity's transform</param>
    /// <param name="relatedComponent">The entity's related component</param>
    /// <param name="renderable">The entity's Renderable</param>
    /// <param name="viewID">The current view ID</param>
    public void AddDrawCall(Entity entity, Transform transform, IComponent relatedComponent, Renderable renderable, ushort viewID)
    {
        lock (lockObject)
        {
            if (currentDrawBucket.drawCalls.TryGetValue(viewID, out var drawCalls) == false)
            {
                drawCalls = new List<DrawCall>();

                currentDrawBucket.drawCalls.Add(viewID, drawCalls);
            }

            drawCalls.Add(new DrawCall()
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
}
