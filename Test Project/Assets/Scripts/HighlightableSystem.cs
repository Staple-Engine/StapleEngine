using Staple;
using System.Linq;
using System.Numerics;

namespace TestGame
{
    public class HighlightableSystem : IEntitySystem
    {
        public SubsystemType UpdateType => SubsystemType.Update;

        public void Startup()
        {
        }

        public void Process(float deltaTime)
        {
            var sortedCameras = Scene.SortedCameras;

            var c = sortedCameras.FirstOrDefault();

            if(c == null)
            {
                return;
            }

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

            Scene.ForEach((Entity entity, bool enabled, ref HighlightableComponent component, ref SpriteRenderer renderer) =>
            {
                if (enabled == false)
                {
                    return;
                }

                renderer.color = Color.White;
            });

            if (Physics.RayCast3D(new Ray(worldPosition, c.transform.Forward), out var body, out var fraction, maxDistance: 5))
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

        public void Shutdown()
        {
        }
    }
}