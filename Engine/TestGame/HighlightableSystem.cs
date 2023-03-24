using Staple;
using System.Collections.Generic;
using System.Linq;

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

            world.ForEach((Entity entity, ref HighlightableComponent component, ref Sprite renderer, ref Transform transform) =>
            {
                var worldPosition = Camera.ScreenPointToWorld(Input.MousePosition, world, c.entity, c.camera, c.transform);

                if (renderer.bounds.Contains(worldPosition))
                {
                    renderer.color = new Color(0.5f, 0.5f, 0, 1);
                }
                else
                {
                    renderer.color = Color.White;
                }
            });
        }
    }
}