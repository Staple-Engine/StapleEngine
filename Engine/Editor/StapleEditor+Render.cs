﻿using Bgfx;
using Hexa.NET.ImGuizmo;
using Staple.Internal;
using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple.Editor;

internal partial class StapleEditor
{
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

        static void ExecuteBlock(object source, Action execute)
        {
            try
            {
                execute();
            }
            catch(Exception e)
            {
                Log.Debug($"[{source.GetType().FullName}]: {e}");
            }
        }

        unsafe
        {
            var projection = Camera.Projection(default, camera);
            var view = cameraTransform.Matrix;

            Matrix4x4.Invert(view, out view);

            frustumCuller.Update(view, projection);

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

                    var matrix = Math.TransformationMatrix(selectedTransform.Position, selectedTransform.Scale, selectedTransform.Rotation);
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

        wireframeMaterial.SetVector4("cameraPosition", new Vector4(cameraTransform.Position, 1));

        var renderSystem = RenderSystem.Instance;

        foreach (var system in renderSystem.renderSystems)
        {
            system.Prepare();
        }

        RenderSystem.CurrentCamera = (camera, cameraTransform);

        if (World.Current != null)
        {
            var renderCamera = Scene.SortedCameras.FirstOrDefault()?.camera ?? camera;

            var transforms = Scene.Query<Transform>();

            foreach ((Entity entity, Transform transform) in transforms)
            {
                if(entity.Layer == LayerMask.NameToLayer(RenderTargetLayerName))
                {
                    continue;
                }

                foreach(var system in renderSystem.renderSystems)
                {
                    var related = entity.GetComponent(system.RelatedComponent());

                    if (related != null)
                    {
                        ExecuteBlock(system, () =>
                        {
                            system.Preprocess(entity, transform, related, renderCamera, cameraTransform);
                        });

                        if (related is Renderable renderable &&
                            renderable.enabled)
                        {
                            renderable.isVisible = frustumCuller.AABBTest(renderable.bounds) != FrustumAABBResult.Invisible || true; //TEMP: Figure out what's wrong with the frustum culler

                            if (renderable.isVisible && renderable.forceRenderingOff == false)
                            {
                                ExecuteBlock(system, () =>
                                {
                                    system.Process(entity, transform, related, renderCamera, cameraTransform, SceneView);
                                });
                            }

                            ReplaceEntityBodyIfNeeded(entity, transform, renderable.localBounds);
                        }
                        else if(related is not Renderable)
                        {
                            ExecuteBlock(system, () =>
                            {
                                system.Process(entity, transform, related, renderCamera, cameraTransform, SceneView);
                            });
                        }
                    }
                }
            }

            if (cachedGizmoEditors.Count > 0)
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

            //Temporarily disabled because obtrusive
            /*
            Scene.IterateEntities((entity) =>
            {
                var transform = entity.GetComponent<Transform>();

                if(transform == null)
                {
                    return;
                }

                if(componentIcons.TryGetValue(entity, out var icon) == false)
                {
                    return;
                }

                componentIconMaterial ??= new Material(ResourceManager.instance.LoadMaterial("Materials/Sprite.mat"));

                componentIconMaterial.MainTexture = icon;

                MeshRenderSystem.DrawMesh(Mesh.Quad, transform.Position, Quaternion.Identity, Vector3.One, componentIconMaterial, WireframeView);
            });
            */
        }

        foreach (var system in renderSystem.renderSystems)
        {
            ExecuteBlock(system, () =>
            {
                system.Submit();
            });
        }
    }
}
