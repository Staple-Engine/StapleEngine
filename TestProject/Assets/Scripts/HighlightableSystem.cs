using Staple;
using System.Linq;
using System.Numerics;

namespace TestGame;

public class HighlightableSystem : IEntitySystemUpdate
{
    private readonly SceneQuery<HighlightableComponent, SpriteRenderer> highlightables = new();

    public void Update(float deltaTime)
    {
        var sortedCameras = Scene.SortedCameras;

        if (sortedCameras.Length == 0)
        {
            return;
        }

        var c = sortedCameras.FirstOrDefault();

        var mousePosition = Vector2.Zero;

        if(Platform.IsDesktopPlatform)
        {
            mousePosition = Input.MousePosition;
        }
        else
        {
            if(Input.TouchCount > 0)
            {
                var pointer = Input.GetPointerID(0);

                mousePosition = Input.GetTouchPosition(pointer);
            }
        }

        var worldPosition = Camera.ScreenPointToWorld(mousePosition, c.entity, c.camera, c.transform);

        foreach((_, _, SpriteRenderer renderer) in highlightables.Contents)
        {
            renderer.color = Color.White;
        }

        if (Physics.RayCast3D(new Ray(worldPosition, c.transform.Forward), out var body, out _, LayerMask.Everything, maxDistance: 5))
        {
            var entity = body.Entity;

            var renderer = entity.GetComponent<SpriteRenderer>();

            if(renderer != null)
            {
                renderer.color = new Color(0.5f, 0.5f, 0, 1);
            }

            if(Input.GetMouseButtonDown(MouseButton.Left) || Input.GetTouchDown(0))
            {
                var audioSource = entity.GetComponent<AudioSource>();

                audioSource?.Play();
            }
        }
    }
}
