using MessagePack;
using System;
using System.Collections.Generic;

namespace Staple
{
    /// <summary>
    /// Stores information on app data
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class AppSettings
    {
        /// <summary>
        /// Whether to run in the background (not pause)
        /// </summary>
        [Key(0)]
        public bool runInBackground = false;

        /// <summary>
        /// The app's name
        /// </summary>
        [Key(1)]
        public string appName;

        /// <summary>
        /// The company's name
        /// </summary>
        [Key(2)]
        public string companyName;

        /// <summary>
        /// How many frames per second to run in fixed time
        /// </summary>
        [Key(3)]
        public int fixedTimeFrameRate = 30;

        /// <summary>
        /// Whether to use the multithreaded renderer
        /// </summary>
        [Key(4)]
        public bool multiThreadedRenderer = false;

        /// <summary>
        /// Which layers to use
        /// </summary>
        [Key(5)]
        public List<string> layers = new();

        /// <summary>
        /// Which sorting layers to use
        /// </summary>
        [Key(6)]
        public List<string> sortingLayers = new();

        /// <summary>
        /// Which renderers to use per platform
        /// </summary>
        [Key(7)]
        public Dictionary<AppPlatform, List<RendererType>> renderers = new();
    }
}
