using MessagePack;

namespace Staple.Internal
{
    [MessagePackObject]
    public class AssetHolder
    {
        [Key(0)]
        public string guid;

        [Key(1)]
        public string typeName;
    }
}
