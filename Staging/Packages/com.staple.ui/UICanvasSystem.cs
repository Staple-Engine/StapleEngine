using System.Numerics;
using Staple.UI;

namespace Staple.Internal;

/// <summary>
/// Canvas Render System. Used to render UI.
/// </summary>
public class UICanvasSystem : RenderSystemBase
{
    private readonly SceneQuery<UICanvas> canvases = new();

    public bool IsPointerOverUI { get; private set; }

    public UICanvasSystem() : base(true, typeof(UICanvas), typeof(GenericRenderQueue<UICanvas>))
    {
    }

    public override IRenderQueue CreateRenderQueue() => new GenericRenderQueue<UICanvas>();

    [OnAssetsReimported]
    internal static void OnAssetsReloaded()
    {
        var query = Scene.Query<UICanvas>(true);

        foreach(var (_, canvas) in query)
        {
            canvas.Reload();
        }
    }

    #region Lifecycle
    public override void Startup()
    {
    }

    public override void Shutdown()
    {
    }

    public override void Prepare()
    {
    }

    public override void Preprocess(IRenderQueue renderQueue)
    {
    }

    public override void Process(IRenderQueue renderQueue, Camera activeCamera, Transform activeCameraTransform, int renderIndex)
    {
    }
    #endregion

    public override void Submit()
    {
        var projection = Matrix4x4.CreateOrthographicOffCenter(0, Screen.Width, Screen.Height, 0, -1, 1);

        RenderSystem.Render(RenderTarget.Current, CameraClearMode.None, Color.White, new(0, 0, 1, 1), Matrix4x4.Identity, projection,
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
