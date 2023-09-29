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
        /// The sprite texture to use
        /// </summary>
        public Texture texture;

        /// <summary>
        /// The sprite index to use
        /// </summary>
        public int spriteIndex = 0;

        /// <summary>
        /// The sprite's color
        /// </summary>
        public Color color = Color.White;

        /// <summary>
        /// Whether to flip the X axis
        /// </summary>
        public bool flipX = false;

        /// <summary>
        /// Whether to flip the Y axis
        /// </summary>
        public bool flipY = false;

        internal void OnDestroy()
        {
            material?.Destroy();
        }
    }
}
