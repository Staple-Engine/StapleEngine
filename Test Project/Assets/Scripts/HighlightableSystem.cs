using Staple;
using System.Linq;
using System.Numerics;

namespace TestGame
{
    public class HighlightableSystem : IEntitySystem
    {
        public SubsystemType UpdateType => SubsystemType.Update;

        public void Process(World world, float deltaTime)
        {
            var sortedCameras = world.SortedCameras;

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

            var worldPosition = Camera.ScreenPointToWorld(mousePosition, world, c.entity, c.camera, c.transform);

            world.ForEach((Entity entity, bool enabled, ref HighlightableComponent component, ref SpriteRenderer renderer) =>
            {
                if (enabled == false)
                {
                    return;
                }

                renderer.color = Color.White;
            });

            if (Physics.RayCast3D(new Ray(worldPosition, c.transform.Forward), out var body, out var fraction))
            {
                var entity = body.Entity;

                var renderer = world.GetComponent<SpriteRenderer>(entity);

                if(renderer != null)
                {
                    renderer.color = new Color(0.5f, 0.5f, 0, 1);
                }
            }
        }
    }
}