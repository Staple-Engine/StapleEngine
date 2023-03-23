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
        public Dictionary<string, uint> layers = new Dictionary<string, uint>
        {
            { "Default", 0 }
        };

        /// <summary>
        /// Which sorting layers to use
        /// </summary>
        [Key(6)]
        public Dictionary<string, uint> sortingLayers = new Dictionary<string, uint>
        {
            { "Default", 0 }
        };

        /// <summary>
        /// Which renderers to use per platform
        /// </summary>
        [Key(7)]
        public Dictionary<AppPlatform, RendererType> renderers = new Dictionary<AppPlatform, RendererType>
        {
            { AppPlatform.Windows, RendererType.Direct3D11 },
            { AppPlatform.Linux, RendererType.OpenGL },
            { AppPlatform.MacOSX, RendererType.Metal },
        };
    }
}
