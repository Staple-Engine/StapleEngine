namespace Staple
{
    /// <summary>
    /// Sprite component
    /// </summary>
    public class Sprite : Renderable
    {
        /// <summary>
        /// The sprite's material
        /// </summary>
        public Material material;

        /// <summary>
        /// The sprite's texture
        /// </summary>
        public Texture texture;

        /// <summary>
        /// The sprite's color
        /// </summary>
        public Color color = Color.White;

        /// <summary>
        /// The sprite's sorting layer
        /// </summary>
        public uint sortingLayer;

        internal void OnDestroy()
        {
            material?.Destroy();
        }
    }
}
