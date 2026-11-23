using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

/// <summary>
/// Mesh Combine system that merges child mesh renderers inside it
/// </summary>
public sealed class MeshCombineSystem : RenderSystemBase
{
    private readonly SceneQuery<MeshCombine> combines = new(false);

    public MeshCombineSystem() : base(true, typeof(MeshCombine), typeof(GenericRenderQueue<MeshCombine>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<MeshCombine>();

    public override IRenderData CreateRenderData() => null;

    #region Lifecycle
    public override void Startup()
    {
    }

    public override void Shutdown()
    {
    }

    public override void PrepareRenderData(IRenderData renderData, IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform,
        int renderIndex)
    {
    }

    public override void UpdateRenderDataVisibility(IRenderData renderData, Camera activeCamera, Transform activeCameraTransform)
    {
    }
    #endregion

    public override void Prepare()
    {
        if(!Platform.IsPlaying)
        {
            return;
        }

        foreach(var combine in combines.Contents)
        {
            var sourceContainer = (combine.sourceContainer?.Entity.IsValid ?? false) ? combine.sourceContainer : combine.Transform;
            var targetContainer = (combine.targetContainer?.Entity.IsValid ?? false) ? combine.targetContainer : combine.Transform;

            if(!(sourceContainer?.Entity.IsValid ?? false) ||
                !(targetContainer?.Entity.IsValid ?? false))
            {
                continue;
            }

            if (!combine.processed)
            {
                combine.processed = true;

                var renderers = sourceContainer.Entity.GetComponentsInChildren<MeshRenderer>(true);

                var combined = MeshUtilities.CombineMeshes(sourceContainer);

                if(combined == null ||
                    !MeshUtilities.InstanceCombinedMesh(combined, targetContainer))
                {
                    continue;
                }

                switch (combine.childMode)
                {
                    case MeshCombine.MeshCombineChildMode.DestroyRenderers:

                        foreach (var renderer in renderers)
                        {
                            renderer.Entity.RemoveComponent<MeshRenderer>();
                        }

                        break;

                    case MeshCombine.MeshCombineChildMode.DisableRendering:

                        foreach (var renderer in renderers)
                        {
                            renderer.Enabled = false;
                        }

                        break;
                }
            }
        }
    }

    public override void Preprocess(IRenderQueue renderQueue)
    {
    }

    public override void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
    {
    }

    public override void Submit()
    {
    }
}
