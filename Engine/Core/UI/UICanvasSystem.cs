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

    private readonly SceneQuery<UICanvas> canvases = new();

    public bool UsesOwnRenderProcess => true;

    public Type RelatedComponent => typeof(UICanvas);

    public bool IsPointerOverUI { get; private set; }

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

        IsPointerOverUI = false;

        foreach(var (_, canvas) in canvases.Contents)
        {
            if(canvas.Manager == null)
            {
                canvas.Manager = new()
                {
                    CanvasSize = new(Screen.Width, Screen.Height)
                };

                if(canvas.layout?.text != null)
                {
                    canvas.Manager.LoadLayouts(canvas.layout.text);
                }
            }

            canvas.Manager.Update();
            canvas.Manager.Draw();

            IsPointerOverUI |= canvas.Manager.MouseOverElement != null;
        }
    }
}
