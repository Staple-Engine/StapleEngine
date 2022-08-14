
using Bgfx;
using Staple.Internal;
using System;
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
        }

        internal class DrawBucket
        {
            public Dictionary<ushort, List<DrawCall>> drawCalls = new Dictionary<ushort, List<DrawCall>>();
        }

        public SubsystemType type { get; } = SubsystemType.Render;

        private DrawBucket previousDrawBucket = new DrawBucket(), currentDrawBucket = new DrawBucket();

        private object lockObject = new object();

        private SpriteRenderSystem spriteRenderSystem = new SpriteRenderSystem();

        private FrustumCuller frustumCuller = new FrustumCuller();

        private bool needsDrawCalls = false;

        private float accumulator = 0.0f;

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
            Time.OnAccumulatorFinished += () =>
            {
                needsDrawCalls = true;
            };
        }

        public void Shutdown()
        {
            spriteRenderSystem.Destroy();
        }

        public void AddDrawCall(Entity entity, ushort viewID)
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
                    position = entity.Transform.Position,
                    rotation = entity.Transform.Rotation,
                    scale = entity.Transform.Scale,
                });
            }
        }

        public void Update()
        {
            ushort viewID = 1;

            var cameras = Scene.current.GetComponents<Camera>().OrderBy(x => x.depth);

            foreach (var camera in cameras)
            {
                unsafe
                {
                    Matrix4x4 projection;

                    switch (camera.cameraType)
                    {
                        case CameraType.Perspective:

                            projection = Matrix4x4.CreatePerspectiveFieldOfView(Math.Deg2Rad(camera.fov), camera.Width / camera.Height,
                                camera.nearPlane, camera.farPlane);

                            break;

                        case CameraType.Orthographic:

                            projection = Matrix4x4.CreateOrthographicOffCenter(0, camera.Width, camera.Height, 0, camera.nearPlane, camera.farPlane);

                            break;

                        default:
                            continue;
                    }

                    var view = camera.Transform.Matrix;

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
                    if(currentDrawBucket.drawCalls.TryGetValue(viewID, out var drawCalls) && previousDrawBucket.drawCalls.TryGetValue(viewID, out var previousDrawCalls))
                    {
                        foreach(var call in drawCalls)
                        {
                            var previous = previousDrawCalls.Find(x => x.entity == call.entity);

                            if(call.entity.TryGetComponent(out Renderer renderer) && renderer.enabled)
                            {
                                var previousPosition = previous.position;
                                var previousRotation = previous.rotation;
                                var previousScale = previous.scale;

                                var currentPosition = call.position;
                                var currentRotation = call.rotation;
                                var currentScale = call.scale;

                                var transform = new Transform(null);

                                transform.LocalPosition = Vector3.Lerp(previousPosition, currentPosition, alpha);
                                transform.LocalRotation = Quaternion.Lerp(previousRotation, currentRotation, alpha);
                                transform.LocalScale = Vector3.Lerp(previousScale, currentScale, alpha);

                                if(renderer is SpriteRenderer)
                                {
                                    spriteRenderSystem.Process(call.entity, transform, (SpriteRenderer)renderer, viewID);
                                }
                            }
                        }
                    }
                }

                if (needsDrawCalls)
                {
                    lock (lockObject)
                    {
                        var previous = previousDrawBucket;

                        previousDrawBucket = currentDrawBucket;
                        currentDrawBucket = previous;

                        currentDrawBucket.drawCalls.Clear();
                    }

                    foreach (var entity in Scene.current.entities)
                    {
                        if (camera.cullingLayers.HasLayer(entity.layer) &&
                            entity.TryGetComponent(out Renderer renderer) &&
                            renderer.enabled &&
                            frustumCuller.AABBTest(renderer.bounds) != FrustumAABBResult.Invisible)
                        {
                            AddDrawCall(entity, viewID);
                        }
                    }
                }

                viewID++;
            }

            if(needsDrawCalls)
            {
                needsDrawCalls = false;
            }

            accumulator = Time.Accumulator;
        }
    }
}
