using MessagePack;
using System;
using System.Numerics;

namespace Staple.Internal
{
    [Serializable]
    [MessagePackObject]
    public class MaterialMetadata
    {
        [Key(0)]
        public string shaderPath;

        [Key(1)]
        public string mainTexturePath;

        [Key(2)]
        public Color32 color;

        [Key(3)]
        public Vector4 textureScale = new Vector4(0, 0, 1, 1);
    }
}
