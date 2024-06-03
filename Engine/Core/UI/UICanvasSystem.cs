using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal;

public class UICanvasSystem : IRenderSystem
{
    public const ushort UIViewID = 200;

    private class RenderInfo
    {
        public UICanvas canvas;
        public Transform canvasTransform;
        public Matrix4x4 projection;
    }

    private List<RenderInfo> renders = new();

    public void Destroy()
    {
    }

    public void Prepare()
    {
        renders.Clear();
    }

    public void Preprocess(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Entity entity, Transform transform, IComponent relatedComponent, Camera activeCamera, Transform activeCameraTransform, ushort viewId)
    {
        if(relatedComponent is not UICanvas canvas ||
            canvas.resolution.X <= 0 ||
            canvas.resolution.Y <= 0 ||
            renders.Any(x => x.canvas == canvas))
        {
            return;
        }

        renders.Add(new()
        {
            canvas = canvas,
            canvasTransform = transform,
            projection = Matrix4x4.CreateOrthographicOffCenter(0, Screen.Width, Screen.Height, 0, -1, 1),
        });
    }

    public Type RelatedComponent() => typeof(UICanvas);

    public void Submit()
    {
        foreach(var render in renders)
        {
            var view = Matrix4x4.Identity;
            var projection = render.projection;

            unsafe
            {
                Matrix4x4.Invert(view, out view);

                bgfx.set_view_transform(UIViewID, &view, &projection);
                bgfx.set_view_clear(UIViewID, (ushort)bgfx.ClearFlags.None, 0, 1, 0);
                bgfx.set_view_rect(UIViewID, 0, 0, (ushort)Screen.Width, (ushort)Screen.Height);
            }

            void Recursive(Transform parent, Vector2Int position)
            {
                foreach(var child in parent)
                {
                    var p = position;
                    var localPosition = child.LocalPosition;

                    p.X += (int)localPosition.X;
                    p.Y += (int)localPosition.Y;

                    if(child.entity.TryGetComponent<IUIElement>(out var element))
                    {
                        element.Render(p, UIViewID);
                    }

                    Recursive(child, p);
                }
            }

            Recursive(render.canvasTransform, Vector2Int.Zero);
        }
    }
}
