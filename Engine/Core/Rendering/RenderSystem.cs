using Bgfx;
using Staple.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple
{
    internal class RenderSystem : ISubsystem
    {
        internal class DrawCall
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 scale;
            public Entity entity;
            public Renderable renderable;
            public IComponent relatedComponent;
        }

        internal class DrawBucket
        {
            public Dictionary<ushort, List<DrawCall>> drawCalls = new Dictionary<ushort, List<DrawCall>>();
        }

        public SubsystemType type { get; } = SubsystemType.Render;

        private DrawBucket previousDrawBucket = new DrawBucket(), currentDrawBucket = new DrawBucket();

        private object lockObject = new object();

        private FrustumCuller frustumCuller = new FrustumCuller();

        private bool needsDrawCalls = false;

        private float accumulator = 0.0f;

        private List<IRenderSystem> renderSystems = new List<IRenderSystem>();

        private Transform stagingTransform = new Transform();

        internal static byte Priority = 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BlendFunction(bgfx.StateFlags source, bgfx.StateFlags destination)
        {
            return BlendFunction(source, destination, source, destination);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong BlendFunction(bgfx.StateFlags sourceColor, bgfx.StateFlags destinationColor, bgfx.StateFlags sourceAlpha, bgfx.StateFlags destinationAlpha)
        {
            return ((ulong)sourceColor | ((ulong)destinationColor << 4)) | (((ulong)sourceAlpha | ((ulong)destinationAlpha << 4)) << 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool CheckAvailableTransientBuffers(uint _numVertices, bgfx.VertexLayout layout, uint _numIndices)
        {
            unsafe
            {
                return _numVertices == bgfx.get_avail_transient_vertex_buffer(_numVertices, &layout)
                    && (0 == _numIndices || _numIndices == bgfx.get_avail_transient_index_buffer(_numIndices, false));
            }
        }

        public void Startup()
        {
            renderSystems.Add(new SpriteRenderSystem());

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

        public void AddDrawCall(Entity entity, Transform transform, IComponent relatedComponent, Renderable renderable, ushort viewID)
        {
            lock(lockObject)
            {
                if(currentDrawBucket.drawCalls.TryGetValue(viewID, out var drawCalls) == false)
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

        public void Update()
        {
            ushort viewID = 1;

            var cameras = Scene.current.world.SortedCameras;

            if(cameras.Length > 0)
            {
                foreach(var c in cameras)
                {
                    var camera = c.camera;
                    var cameraTransform = c.transform;

                    foreach (var system in renderSystems)
                    {
                        system.Prepare();
                    }

                    unsafe
                    {
                        var projection = Camera.Projection(Scene.current.world, c.entity, c.camera, c.transform);
                        var view = cameraTransform.Matrix;

                        Matrix4x4.Invert(view, out view);

                        bgfx.set_view_transform(viewID, &view, &projection);

                        frustumCuller.Update(view, projection);
                    }

                    switch (camera.clearMode)
                    {
                        case CameraClearMode.Depth:
                            bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Depth), 0, 24, 0);

                            break;

                        case CameraClearMode.None:
                            bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.None), 0, 24, 0);

                            break;

                        case CameraClearMode.SolidColor:
                            bgfx.set_view_clear(viewID, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), camera.clearColor.uintValue, 0, 0);

                            break;
                    }

                    bgfx.set_view_rect(viewID, (ushort)camera.viewport.X, (ushort)camera.viewport.Y,
                        (ushort)(camera.viewport.Z * AppPlayer.ScreenWidth), (ushort)(camera.viewport.W * AppPlayer.ScreenHeight));

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
                                            system.Process(call.entity, stagingTransform, call.relatedComponent, viewID);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    foreach(var system in renderSystems)
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
                    var previous = previousDrawBucket;

                    previousDrawBucket = currentDrawBucket;
                    currentDrawBucket = previous;

                    currentDrawBucket.drawCalls.Clear();
                }

                foreach(var c in cameras)
                {
                    var camera = c.camera;
                    var cameraTransform = c.transform;

                    unsafe
                    {
                        var projection = Camera.Projection(Scene.current.world, c.entity, c.camera, c.transform);
                        var view = cameraTransform.Matrix;

                        frustumCuller.Update(view, projection);
                    }

                    Scene.current.world.ForEach((Entity entity, ref Transform t) =>
                    {
                        foreach (var system in renderSystems)
                        {
                            var related = Scene.current.world.GetComponent(entity, system.RelatedComponent());

                            if (related != null)
                            {
                                system.Preprocess(entity, t, related);

                                if (related is Renderable renderable &&
                                    renderable.enabled &&
                                    frustumCuller.AABBTest(renderable.bounds) != FrustumAABBResult.Invisible)
                                {
                                    AddDrawCall(entity, t, related, renderable, viewID);
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
    }
}
