using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Staple.UI;

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

    public delegate void ObserverCallback(Vector2Int position, Vector2Int size, UIElement element);

    public ObserverCallback observer;

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

            void Recursive(Transform parent, Vector2Int position, Vector2Int containerSize)
            {
                foreach(var child in parent)
                {
                    var p = position;
                    Vector2Int localPosition;
                    Vector2Int localSize = containerSize;

                    if (child.entity.TryGetComponent<UIElement>(out var element))
                    {
                        if (element.adjustToIntrinsicSize)
                        {
                            element.size = element.IntrinsicSize();
                        }

                        localPosition = element.position;
                        localSize = element.size;

                        switch (element.alignment)
                        {
                            case UIElementAlignment.TopLeft:

                                //Do nothing

                                break;

                            case UIElementAlignment.TopRight:

                                localPosition = new Vector2Int(containerSize.X - localSize.X, 0) + localPosition;

                                break;

                            case UIElementAlignment.Top:

                                localPosition = new Vector2Int((containerSize.X - localSize.X) / 2, 0) + localPosition;

                                break;

                            case UIElementAlignment.Bottom:

                                localPosition = new Vector2Int((containerSize.X - localSize.X) / 2, containerSize.Y - localSize.Y) + localPosition;

                                break;

                            case UIElementAlignment.Left:

                                localPosition = new Vector2Int(0, (containerSize.Y - localSize.Y) / 2) + localPosition;

                                break;

                            case UIElementAlignment.Right:

                                localPosition = new Vector2Int(containerSize.X - localSize.X, (containerSize.Y - localSize.Y) / 2) + localPosition;

                                break;

                            case UIElementAlignment.Center:

                                localPosition = new Vector2Int((containerSize.X - localSize.X) / 2, (containerSize.Y - localSize.Y) / 2) + localPosition;

                                break;

                            case UIElementAlignment.BottomLeft:

                                localPosition = new Vector2Int(0, containerSize.Y - localSize.Y) + localPosition;

                                break;

                            case UIElementAlignment.BottomRight:

                                localPosition = new Vector2Int(containerSize.X - localSize.X, containerSize.Y - localSize.Y) + localPosition;

                                break;
                        }
                    }
                    else
                    {
                        localPosition = new Vector2Int((int)child.LocalPosition.X, (int)child.LocalPosition.Y);
                        localSize = containerSize;
                    }

                    p.X += localPosition.X;
                    p.Y += localPosition.Y;

                    element?.Render(p, UIViewID);

                    observer?.Invoke(p, localSize, element);

                    Recursive(child, p, localSize);
                }
            }

            Recursive(render.canvasTransform, Vector2Int.Zero, new Vector2Int(Screen.Width, Screen.Height));
        }
    }
}
