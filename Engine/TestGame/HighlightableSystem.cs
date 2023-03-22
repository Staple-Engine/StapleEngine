using Staple;
using System.Collections.Generic;
using System.Linq;

namespace TestGame
{
    public class HighlightableSystem : IEntitySystem
    {
        public void Process(World world, float deltaTime)
        {
            var cameras = new List<Camera>();
            var transforms = new List<Transform>();

            world.ForEach((Entity entity, ref Camera camera, ref Transform transform) =>
            {
                cameras.Add(camera);
                transforms.Add(transform);
            });

            var camera = cameras.FirstOrDefault();
            var cameraTransform = transforms.FirstOrDefault();

            world.ForEach((Entity entity, ref HighlightableComponent component, ref Sprite renderer, ref Transform transform) =>
            {
                var worldPosition = camera.ScreenPointToWorld(Input.MousePosition, cameraTransform);

                if(renderer.bounds.Contains(worldPosition))
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