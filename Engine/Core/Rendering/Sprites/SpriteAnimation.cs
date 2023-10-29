using Staple.Internal;
using System.Collections.Generic;

namespace Staple
{
    /// <summary>
    /// Represents a sprite animation
    /// </summary>
    [AssetCategory("2D")]
    public class SpriteAnimation : IStapleAsset, IPathAsset
    {
        /// <summary>
        /// The sprite's texture
        /// </summary>
        public Texture texture;

        /// <summary>
        /// The animation frame rate
        /// </summary>
        public int frameRate = 30;

        /// <summary>
        /// Whether the frame rate is milliseconds instead of frames
        /// </summary>
        public bool frameRateIsMilliseconds = false;

        /// <summary>
        /// The list of all animation frames
        /// </summary>
        public List<int> frames = new();

        public string Path { get; set; }

        /// <summary>
        /// IPathAsset implementation. Loads a Sprite Animation from path.
        /// </summary>
        /// <param name="path">The path to load from</param>
        /// <returns>The Sprite Animation, or null</returns>
        public static object Create(string path)
        {
            return ResourceManager.instance.LoadAsset<SpriteAnimation>(path);
        }
    }
}
