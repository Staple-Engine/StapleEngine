using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [MessagePackObject]
    [Serializable]
    public class AppSettings
    {
        [Key(0)]
        public bool runInBackground = false;

        [Key(1)]
        public string appName;

        [Key(2)]
        public string companyName;

        [Key(3)]
        public int fixedTimeFrameRate = 30;

        [Key(4)]
        public bool multiThreadedRenderer = false;

        [Key(5)]
        public Dictionary<string, uint> layers = new Dictionary<string, uint>
        {
            { "Default", 0 }
        };

        [Key(6)]
        public Dictionary<string, uint> sortingLayers = new Dictionary<string, uint>
        {
            { "Default", 0 }
        };

        [Key(7)]
        public Dictionary<AppPlatform, RendererType> renderers = new Dictionary<AppPlatform, RendererType>
        {
            { AppPlatform.Windows, RendererType.Direct3D11 },
            { AppPlatform.Linux, RendererType.OpenGL },
            { AppPlatform.MacOSX, RendererType.Metal },
        };
    }
}
