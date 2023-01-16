namespace Staple
{
    public class Sprite : Renderable
    {
        public Material material;
        public Texture texture;
        public Color color = Color.White;

        internal void OnDestroy()
        {
            material?.Destroy();
        }
    }
}
