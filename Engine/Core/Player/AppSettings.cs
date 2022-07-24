using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [Serializable]
    public class AppSettings
    {
        public bool runInBackground = false;
        public string appName;

        public Dictionary<string, uint> layers = new Dictionary<string, uint>
        {
            { "Default", 0 }
        };

        public Dictionary<string, uint> sortingLayers = new Dictionary<string, uint>
        {
            { "Default", 0 }
        };

        public Dictionary<AppPlatform, RendererType> renderers = new Dictionary<AppPlatform, RendererType>
        {
            { AppPlatform.Windows, RendererType.Direct3D11 },
            { AppPlatform.Linux, RendererType.OpenGL },
            { AppPlatform.MacOSX, RendererType.Metal },
        };
    }
}
