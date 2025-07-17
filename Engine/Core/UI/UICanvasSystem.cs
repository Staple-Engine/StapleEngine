using Bgfx;
using System;
using System.Numerics;
using Staple.UI;

namespace Staple.Internal;

/// <summary>
/// Canvas Render System. Used to render UI.
/// </summary>
public class UICanvasSystem : IRenderSystem
{
    public const ushort UIViewID = 200;

    private static readonly MouseButton[] MouseButtons = Enum.GetValues<MouseButton>();

    private readonly SceneQuery<Transform, UICanvas> canvases = new();

    public delegate void ObserverCallback(Vector2Int position, Vector2Int size, UIElement element);

    public ObserverCallback observer;

    public bool UsesOwnRenderProcess => true;

    public Type RelatedComponent => typeof(UICanvas);

    #region Lifecycle
    public void Startup()
    {
    }

    public void Shutdown()
    {
    }

    public void ClearRenderData(ushort viewID)
    {
    }

    public void Prepare()
    {
    }

    public void Preprocess((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process((Entity, Transform, IComponent)[] entities, Camera activeCamera, Transform activeCameraTransform, ushort viewID)
    {
    }
    #endregion

    public void Submit(ushort viewID)
    {
        var projection = Matrix4x4.CreateOrthographicOffCenter(0, Screen.Width, Screen.Height, 0, -1, 1);

        unsafe
        {
            var view = Matrix4x4.Identity;

            bgfx.set_view_transform(UIViewID, &view, &projection);
            bgfx.set_view_clear(UIViewID, (ushort)bgfx.ClearFlags.None, 0, 1, 0);
            bgfx.set_view_rect(UIViewID, 0, 0, (ushort)Screen.Width, (ushort)Screen.Height);
        }

        foreach (var (_, transform, canvas) in canvases.Contents)
        {
            Vector2Int GetElementPosition(UIElementAlignment alignment, Vector2Int localPosition, Vector2Int localSize, Vector2Int containerSize)
            {
                return alignment switch
                {
                    UIElementAlignment.TopRight => new Vector2Int(containerSize.X - localSize.X, 0) + localPosition,
                    UIElementAlignment.Top => new Vector2Int((containerSize.X - localSize.X) / 2, 0) + localPosition,
                    UIElementAlignment.Bottom => new Vector2Int((containerSize.X - localSize.X) / 2, containerSize.Y - localSize.Y) + localPosition,
                    UIElementAlignment.Left => new Vector2Int(0, (containerSize.Y - localSize.Y) / 2) + localPosition,
                    UIElementAlignment.Right => new Vector2Int(containerSize.X - localSize.X, (containerSize.Y - localSize.Y) / 2) + localPosition,
                    UIElementAlignment.Center => new Vector2Int((containerSize.X - localSize.X) / 2, (containerSize.Y - localSize.Y) / 2) + localPosition,
                    UIElementAlignment.BottomLeft => new Vector2Int(0, containerSize.Y - localSize.Y) + localPosition,
                    UIElementAlignment.BottomRight => new Vector2Int(containerSize.X - localSize.X, containerSize.Y - localSize.Y) + localPosition,
                    _ => localPosition,
                };
            }

            void RecursiveFindFocusedElement(Vector2Int position, Vector2Int containerSize, Transform current, UIElement element,
                UIInteractible interactible, ref UIInteractible foundElement)
            {
                if (current.entity.EnabledInHierarchy == false)
                {
                    return;
                }

                if (element.adjustToIntrinsicSize)
                {
                    element.size = element.IntrinsicSize();
                }

                var p = position + GetElementPosition(element.alignment, element.position, element.size, containerSize);

                var aabb = new AABB(new Vector3(p.X, p.Y, 0), new Vector3(element.size.X, element.size.Y, 0.1f));

                if (aabb.Contains(Input.MousePosition.ToVector3()) == false)
                {
                    return;
                }

                foundElement = interactible;

                foreach (var child in current.Children)
                {
                    if (child.entity.Enabled &&
                        child.entity.TryGetComponent<UIElement>(out var e))
                    {
                        RecursiveFindFocusedElement(p, element.size, child, e, child.entity.GetComponent<UIInteractible>(), ref foundElement);
                    }
                }
            }

            var inputPressed = false;

            foreach (var button in MouseButtons)
            {
                if (Input.GetMouseButtonDown(button))
                {
                    inputPressed = true;

                    break;
                }
            }

            var lastFocusedElement = canvas.focusedElement;

            void Clear(Transform current)
            {
                if (current.entity.TryGetComponent<UIInteractible>(out var element))
                {
                    element.Clicked = false;
                    element.Hovered = false;

                    foreach (var child in current.Children)
                    {
                        Clear(child);
                    }
                }
            }

            if (Platform.IsPlaying)
            {
                Clear(transform);

                UIInteractible foundElement = null;

                foreach (var child in transform.Children)
                {
                    if (child.entity.TryGetComponent<UIElement>(out var element))
                    {
                        RecursiveFindFocusedElement(Vector2Int.Zero, new Vector2Int(Screen.Width, Screen.Height), child, element,
                            child.entity.GetComponent<UIInteractible>(), ref foundElement);
                    }
                }

                canvas.focusedElement = foundElement;

                if (foundElement != null)
                {
                    foundElement.Hovered = true;
                    foundElement.Focused = inputPressed;
                    foundElement.Clicked = inputPressed;
                }

                if (foundElement != lastFocusedElement && inputPressed)
                {
                    if (lastFocusedElement != null)
                    {
                        lastFocusedElement.Focused = false;
                    }
                }
            }

            void Recursive(Transform parent, Vector2Int position, Vector2Int containerSize)
            {
                foreach (var child in parent.Children)
                {
                    if (child.entity.EnabledInHierarchy == false ||
                        child.entity.TryGetComponent<UIElement>(out var element) == false)
                    {
                        continue;
                    }

                    var p = position;

                    if (element.adjustToIntrinsicSize)
                    {
                        element.size = element.IntrinsicSize();
                    }

                    var localPosition = GetElementPosition(element.alignment, element.position, element.size, containerSize);
                    var localSize = element.size;

                    p.X += localPosition.X;
                    p.Y += localPosition.Y;

                    element?.Render(p, UIViewID);

                    observer?.Invoke(p, localSize, element);

                    Recursive(child, p, localSize);
                }
            }

            Recursive(transform, Vector2Int.Zero, new Vector2Int(Screen.Width, Screen.Height));

            if (Platform.IsPlaying)
            {
                canvas.focusedElement?.Interact();
            }
        }
    }
}
