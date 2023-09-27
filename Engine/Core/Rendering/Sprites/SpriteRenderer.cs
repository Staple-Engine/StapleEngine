namespace Staple
{
    /// <summary>
    /// Sprite Renderer component
    /// </summary>
    public class SpriteRenderer : Renderable
    {
        /// <summary>
        /// The renderer's material
        /// </summary>
        public Material material;

        /// <summary>
        /// The sprite to use
        /// </summary>
        public Sprite sprite;

        /// <summary>
        /// The sprite's color
        /// </summary>
        public Color color = Color.White;

        internal void OnDestroy()
        {
            material?.Destroy();
        }
    }
}
