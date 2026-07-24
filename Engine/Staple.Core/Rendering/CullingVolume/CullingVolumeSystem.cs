namespace Staple.Internal;

/// <summary>
/// Culling Volume system that culls large amounts of objects by checking if its bounds are invisible
/// </summary>
public sealed class CullingVolumeSystem : RenderSystemBase
{
    public CullingVolumeSystem() : base(false, typeof(CullingVolume), typeof(GenericRenderQueue<CullingVolume>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<CullingVolume>();

    #region Lifecycle
    public override void Prepare()
    {
    }

    public override void Preprocess(IRenderQueue renderQueue)
    {
    }

    public override void Shutdown()
    {
    }

    public override void Startup()
    {
    }

    public override void Submit()
    {
    }
    #endregion

    public override void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
    {
        if (renderQueue is not GenericRenderQueue<CullingVolume> queue)
        {
            return;
        }

        var items = queue.Items;

        foreach (var entry in items)
        {
            var volume = entry.component;

            volume.renderers ??= new(entry.entity, EntityQueryMode.SelfAndChildren, false);
            volume.children ??= new(entry.entity, EntityQueryMode.Children, false);

            volume.needsUpdate = true;
        }

        foreach (var entry in items)
        {
            var volume = entry.component;

            if(!volume.needsUpdate)
            {
                continue;
            }

            volume.needsUpdate = false;

            AABB bounds = default;

            switch (volume.type)
            {
                case CullingVolume.CullingType.Renderers:

                    {
                        var validRenderers = 0;

                        foreach (var renderer in volume.renderers.Contents)
                        {
                            if (!renderer.Item2.enabled || renderer.Item2.forceRenderingOff)
                            {
                                continue;
                            }

                            validRenderers++;
                        }

                        volume.boundsCoordinates.Clear();

                        for (int i = 0, index = 0; i < volume.renderers.Length; i++)
                        {
                            var renderer = volume.renderers[i];

                            if (renderer.Item2.enabled && !renderer.Item2.forceRenderingOff)
                            {
                                volume.boundsCoordinates.Add(renderer.Item2.bounds.min);
                                volume.boundsCoordinates.Add(renderer.Item2.bounds.max);

                                index += 2;
                            }
                        }

                        bounds = AABB.CreateFromPoints(volume.boundsCoordinates.Contents);
                    }

                    break;

                case CullingVolume.CullingType.Bounds:

                    bounds = new AABB(entry.transform.Position, volume.bounds * entry.transform.Scale);

                    break;
            }

            var isVisible = activeCamera.IsVisible(bounds);

            foreach (var (_, renderer) in volume.renderers.Contents)
            {
                renderer.cullingState = isVisible ? CullingState.Visible : CullingState.Invisible;
            }

            //We want to avoid over-processing, if we've already processed all
            //renderers then we don't need to process the child volumes, thus saving performance.
            //However, for bounds volumes we don't want to do that as we bounds volumes are only meant to be used
            //for checking a large area to save on performance
            if (volume.type != CullingVolume.CullingType.Bounds)
            {
                foreach (var (_, child) in volume.children.Contents)
                {
                    child.needsUpdate = false;
                }
            }
        }
    }
}
