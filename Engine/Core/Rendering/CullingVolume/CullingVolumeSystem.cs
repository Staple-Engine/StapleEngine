using System;
using System.Numerics;

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

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
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

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
    {
        foreach (var (entity, _, component) in entities)
        {
            if(component is not CullingVolume volume)
            {
                continue;
            }

            volume.renderers ??= new(entity, EntityQueryMode.SelfAndChildren, false);

            if(volume.boundsCoordinates.Length < volume.renderers.Length * 2)
            {
                Array.Resize(ref volume.boundsCoordinates, volume.renderers.Length * 2);
            }

            for (int i = 0, index = 0; i < volume.renderers.Length; i++, index += 2)
            {
                var renderer = volume.renderers[i];

                if(renderer.Item2.enabled)
                {
                    volume.boundsCoordinates[index] = renderer.Item2.bounds.min;
                    volume.boundsCoordinates[index + 1] = renderer.Item2.bounds.max;
                }
                else
                {
                    volume.boundsCoordinates[index] = volume.boundsCoordinates[index + 1] = Vector3.Zero;
                }
            }

            var bounds = AABB.CreateFromPoints(volume.boundsCoordinates);

            var isVisible = activeCamera.IsVisible(bounds);

            foreach (var renderer in volume.renderers.Contents)
            {
                renderer.Item2.cullingState = isVisible ? CullingState.Visible : CullingState.Invisible;
            }
        }
    }
}
