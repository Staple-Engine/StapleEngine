using System;

namespace Staple.Internal;

public sealed class CullingVolumeSystem : IRenderSystem
{
    public bool UsesOwnRenderProcess => false;

    public Type RelatedComponent => typeof(CullingVolume);

    #region Lifecycle
    public void ClearRenderData(ushort viewID)
    {
    }

    public void Prepare()
    {
    }

    public void Process(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
    }

    public void Shutdown()
    {
    }

    public void Startup()
    {
    }

    public void Submit(ushort viewID)
    {
    }
    #endregion

    public void Preprocess(Span<(Entity, Transform, IComponent)> entities, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach(var (entity, _, component) in entities)
        {
            if(component is not CullingVolume volume)
            {
                continue;
            }

            volume.renderers ??= new(entity, EntityQueryMode.SelfAndChildren, false);
            volume.children ??= new(entity, EntityQueryMode.Children, false);

            volume.needsUpdate = true;
        }

        foreach (var (_, transform, component) in entities)
        {
            if(component is not CullingVolume volume ||
                volume.needsUpdate == false)
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
                            if (renderer.Item2.enabled == false || renderer.Item2.forceRenderingOff)
                            {
                                continue;
                            }

                            validRenderers++;
                        }

                        if (volume.boundsCoordinates.Length < validRenderers * 2)
                        {
                            Array.Resize(ref volume.boundsCoordinates, validRenderers * 2);
                        }

                        for (int i = 0, index = 0; i < volume.renderers.Length; i++)
                        {
                            var renderer = volume.renderers[i];

                            if (renderer.Item2.enabled && renderer.Item2.forceRenderingOff == false)
                            {
                                volume.boundsCoordinates[index] = renderer.Item2.bounds.min;
                                volume.boundsCoordinates[index + 1] = renderer.Item2.bounds.max;

                                index += 2;
                            }
                        }

                        bounds = AABB.CreateFromPoints(volume.boundsCoordinates);
                    }

                    break;

                case CullingVolume.CullingType.Bounds:

                    bounds = new AABB(transform.Position, volume.bounds * transform.Scale);

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
