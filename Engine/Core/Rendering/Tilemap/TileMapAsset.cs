using Staple.Internal;
using System;
using System.Collections.Generic;

namespace Staple
{
    [AssetCategory("2D")]
    public class TileMapAsset : IStapleAsset, IGuidAsset
    {
        [Serializable]
        public class Layer
        {
            public List<int> tileIndices = new();
        }

        public List<Texture> tilesets = new();

        public int width = 0;

        public int height;

        public List<Layer> layers = new();

        public string Guid { get; set; }

        public static object Create(string path)
        {
            return ResourceManager.instance.LoadAsset<TileMapAsset>(path);
        }
    }
}
