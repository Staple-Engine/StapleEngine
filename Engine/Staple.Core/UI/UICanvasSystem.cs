using System;
using System.Numerics;
using Staple.UI;

namespace Staple.Internal;

/// <summary>
/// Canvas Render System. Used to render UI.
/// </summary>
public class UICanvasSystem : IRenderSystem
{
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

    public void Prepare()
    {
    }

    public void Preprocess(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
    }

    public void Process(Span<RenderEntry> renderQueue, Camera activeCamera, Transform activeCameraTransform)
    {
    }
    #endregion

    public void Submit()
    {
        var projection = Matrix4x4.CreateOrthographicOffCenter(0, Screen.Width, Screen.Height, 0, -1, 1);

        RenderSystem.Render(null, CameraClearMode.None, Color.White, new(0, 0, 1, 1), Matrix4x4.Identity, projection,
            () =>
            {
                IsPointerOverUI = false;

                foreach (var (_, canvas) in canvases.Contents)
                {
                    canvas.CheckLayoutChanges();

                    canvas.manager.Update();
                    canvas.manager.Draw();

                    IsPointerOverUI |= canvas.manager.MouseOverElement != null;
                }
            });
    }
}
