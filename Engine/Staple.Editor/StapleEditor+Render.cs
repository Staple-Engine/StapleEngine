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
        ImGuizmo.SetDrawlist();
        ImGuizmo.SetOrthographic(false);
        ImGuizmo.SetRect(0, 0, window.width, window.height);

        var hasGizmos = cachedGizmoEditors.Count > 0;

        var projection = Camera.Projection(default, camera);
        var view = cameraTransform.Matrix;

        Matrix4x4.Invert(view, out view);

        camera.UpdateFrustum(view, projection);

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

                var matrix = Matrix4x4.TRS(selectedTransform.Position, selectedTransform.Scale, selectedTransform.Rotation);
                var delta = Matrix4x4.Identity;

                if (ImGuizmo.Manipulate(ref view.M11, ref projection.M11, transformOperation, transformMode, ref matrix.M11,
                    ref delta.M11, snap, localBound, snap))
                {
                    Matrix4x4.Invert(selectedTransform.Parent?.Matrix ?? Matrix4x4.Identity, out var invParent);

                    var local = matrix * invParent;

                    if (Matrix4x4.Decompose(local, out var scale, out var rotation, out var position))
                    {
                        switch (transformOperation)
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

        var renderSystem = RenderSystem.Instance;

        RenderSystem.RenderStats.Clear();

        foreach (var systemInfo in renderSystem.renderSystems)
        {
            systemInfo.system.Prepare();
        }

        RenderSystem.CurrentCamera = (camera, cameraTransform);

        if (World.Current != null)
        {
            foreach (var entity in renderQueue.disabledEntities)
            {
                ClearEntityBody(entity);
            }

            renderQueue.disabledEntities.Clear();

            RenderSystem.Instance.ClearCullingStates();

            foreach (var pair in renderQueue.renderQueue)
            {
                var (components, renderables) = pair.Value;

                try
                {
                    pair.Key.Preprocess(components.ToArray(), camera, cameraTransform);

                    if(renderables.Count > 0)
                    {
                        foreach (var (entity, transform, renderable) in renderables)
                        {
                            if (renderable.enabled)
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

                                if (transform.ChangedThisFrame)
                                {
                                    ReplaceEntityBodyIfNeeded(entity, renderable.bounds);
                                }
                            }
                            else
                            {
                                ClearEntityBody(entity);
                            }
                        }
                    }

                    pair.Key.Process(components.ToArray(), camera, cameraTransform);
                }
                catch (Exception e)
                {
                    Log.Error($"[{pair.Key.GetType()}] {e}");
                }
            }
        }

        RenderSystem.Render(null, CameraClearMode.SolidColor, ClearColor, new(0, 0, 1, 1),
            cameraTransform.Matrix, projection, () =>
            {
                wireframeMaterial?.SetVector4("cameraPosition", new Vector4(cameraTransform.Position, 1));

                foreach (var pair in renderQueue.renderQueue)
                {
                    var (components, renderables) = pair.Value;

                    if (renderables.Count == 0)
                    {
                        continue;
                    }

                    try
                    {
                        pair.Key.Submit();
                    }
                    catch (Exception e)
                    {
                        Log.Error($"[{pair.Key.GetType()}] {e}");
                    }
                }

                foreach (var (_, transform) in renderQueue.transforms.Contents)
                {
                    transform.changedThisFrame = false;
                }

                if (hasGizmos)
                {
                    var counter = 0;

                    selectedEntity.IterateComponents((ref IComponent component) =>
                    {
                        if (cachedGizmoEditors.TryGetValue(counter++, out var editor))
                        {
                            try
                            {
                                editor.OnGizmo(selectedEntity, selectedEntity.GetComponent<Transform>(), component);
                            }
                            catch (Exception e)
                            {
                                Log.Debug($"[{editor.GetType().FullName}]: {e}");
                            }
                        }
                    });
                }
            });
    }
}
