using Bgfx;
using Hexa.NET.ImGuizmo;
using Staple.Internal;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple.Editor;

internal partial class StapleEditor
{
    private const float MinComponentIconDistance = 2;

    /// <summary>
    /// Renders the scene
    /// </summary>
    public void RenderScene()
    {
        bgfx.touch(SceneView);
        bgfx.touch(WireframeView);

        ImGuizmo.SetDrawlist();
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.SetRect(0, 0, window.width, window.height);

        var hasGizmos = cachedGizmoEditors.Count > 0;

        unsafe
        {
            var projection = Camera.Projection(default, camera);
            var view = cameraTransform.Matrix;

            Matrix4x4.Invert(view, out view);

            camera.UpdateFrustum(view, projection);

            bgfx.set_view_transform(SceneView, &view, &projection);
            bgfx.set_view_transform(WireframeView, &view, &projection);

            if (selectedEntity.IsValid &&
                selectedEntity.TryGetComponent<Transform>(out var selectedTransform))
            {
                ImGuizmo.Enable(true);

                unsafe
                {
                    float* snap = null;
                    float* localBound = null;
                    float[] snaps = [1, 1, 1];

                    if (Input.GetKey(KeyCode.LeftShift))
                    {
                        snap = (float*)Unsafe.AsPointer(ref snaps[0]);
                    }

                    var matrix = Math.TRS(selectedTransform.Position, selectedTransform.Scale, selectedTransform.Rotation);
                    var delta = Matrix4x4.Identity;

                    if (ImGuizmo.Manipulate(ref view, ref projection, transformOperation, transformMode, ref matrix, ref delta, snap, localBound, snap))
                    {
                        Matrix4x4.Invert(selectedTransform.parent?.Matrix ?? Matrix4x4.Identity, out var invParent);

                        var local = matrix * invParent;

                        if (Matrix4x4.Decompose(local, out var scale, out var rotation, out var position))
                        {
                            switch(transformOperation)
                            {
                                case ImGuizmoOperation.Rotate:

                                    selectedTransform.LocalRotation = rotation;

                                    break;

                                case ImGuizmoOperation.Translate:

                                    selectedTransform.LocalPosition = position;

                                    break;

                                case ImGuizmoOperation.Scale:

                                    selectedTransform.LocalScale = scale;

                                    break;
                            }
                        }
                    }
                }

                if (ImGuizmo.IsUsing())
                {
                    if (transforming == false)
                    {
                        transforming = true;

                        transformPosition = selectedTransform.LocalPosition;
                        transformRotation = selectedTransform.LocalRotation;
                        transformScale = selectedTransform.LocalScale;
                    }
                }
                else if (transforming)
                {
                    transforming = false;

                    var t = selectedTransform;

                    var currentPosition = selectedTransform.LocalPosition;
                    var currentRotation = selectedTransform.LocalRotation;
                    var currentScale = selectedTransform.LocalScale;

                    var oldPosition = transformPosition;
                    var oldRotation = transformRotation;
                    var oldScale = transformScale;

                    undoStack.AddItem("Transform",
                        () =>
                        {
                            t.LocalPosition = currentPosition;
                            t.LocalRotation = currentRotation;
                            t.LocalScale = currentScale;
                        },
                        () =>
                        {
                            t.LocalPosition = oldPosition;
                            t.LocalRotation = oldRotation;
                            t.LocalScale = oldScale;
                        });
                }
            }
            else
            {
                ImGuizmo.Enable(false);
            }
        }

        wireframeMaterial?.SetVector4("cameraPosition", new Vector4(cameraTransform.Position, 1));

        var renderSystem = RenderSystem.Instance;

        RenderSystem.RenderStats.Clear();

        foreach (var system in renderSystem.renderSystems)
        {
            system.Prepare();
        }

        RenderSystem.CurrentCamera = (camera, cameraTransform);

        if (World.Current != null)
        {
            foreach(var entity in renderQueue.disabledEntities)
            {
                ClearEntityBody(entity);
            }

            renderQueue.disabledEntities.Clear();

            RenderSystem.Instance.ClearCullingStates();

            foreach (var pair in renderQueue.renderQueue)
            {
                if(pair.Value.Count == 0)
                {
                    continue;
                }

                foreach(var item in pair.Value)
                {
                    if(item.Item3 is Renderable renderable &&
                        renderable.enabled)
                    {
                        renderable.isVisible = renderable.enabled &&
                            renderable.forceRenderingOff == false &&
                            renderable.cullingState != CullingState.Invisible;

                        if (renderable.isVisible)
                        {
                            if (renderable.cullingState == CullingState.None)
                            {
                                renderable.isVisible = camera.IsVisible(renderable.bounds);

                                renderable.cullingState = renderable.isVisible ? CullingState.Visible : CullingState.Invisible;
                            }
                        }

                        if (renderable.isVisible == false)
                        {
                            RenderSystem.RenderStats.culledDrawCalls++;
                        }
                    }
                }

                try
                {
                    pair.Key.Preprocess(pair.Value.ToArray(), camera, cameraTransform);

                    foreach (var item in pair.Value)
                    {
                        if (item.Item3 is Renderable renderable)
                        {
                            if (renderable.enabled)
                            {
                                if(item.Item2.ChangedThisFrame)
                                {
                                    ReplaceEntityBodyIfNeeded(item.Item1, renderable.bounds);
                                }
                            }
                            else
                            {
                                ClearEntityBody(item.Item1);
                            }
                        }
                    }

                    pair.Key.Process(pair.Value.ToArray(), camera, cameraTransform, SceneView);

                    pair.Key.Submit(SceneView);
                }
                catch(Exception e)
                {
                    Log.Error($"[{pair.Key.GetType()}] {e}");
                }
            }

            if (hasGizmos)
            {
                var counter = 0;

                selectedEntity.IterateComponents((ref IComponent component) =>
                {
                    if(cachedGizmoEditors.TryGetValue(counter++, out var editor))
                    {
                        try
                        {
                            editor.OnGizmo(selectedEntity, selectedEntity.GetComponent<Transform>(), component);
                        }
                        catch(Exception e)
                        {
                            Log.Debug($"[{editor.GetType().FullName}]: {e}");
                        }
                    }
                });
            }

            foreach(var (entity, transform) in renderQueue.transforms.Contents)
            {
                transform.changedThisFrame = false;

                if(Vector3.Distance(transform.Position, cameraTransform.Position) < MinComponentIconDistance)
                {
                    continue;
                }

                if(componentIcons.TryGetValue(entity, out var icon) == false)
                {
                    continue;
                }

                componentIconMaterial ??= new Material(SpriteRenderSystem.DefaultMaterial.Value);

                componentIconMaterial.MainColor = Color.Lerp(Color.Clear, Color.White,
                    Math.Clamp01(Vector3.Distance(transform.Position, cameraTransform.Position) - MinComponentIconDistance));
                componentIconMaterial.MainTexture = icon;

                var rotation = Math.LookAt(Vector3.Normalize(cameraTransform.Position - transform.Position), Vector3.UnitY) *
                    Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), 180 * Math.Deg2Rad);

                MeshRenderSystem.RenderMesh(Mesh.Quad, transform.Position, rotation, Vector3.One, componentIconMaterial, MaterialLighting.Unlit, WireframeView);
            }
        }
    }
}
