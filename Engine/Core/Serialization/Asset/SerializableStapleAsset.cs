using MessagePack;
using System.Collections.Generic;

namespace Staple.Internal
{
    [MessagePackObject]
    public class SerializableStapleAssetHeader
    {
        [IgnoreMember]
        public readonly static char[] ValidHeader = new char[]
        {
            'S', 'S', 'A', 'S'
        };

        [IgnoreMember]
        public const byte ValidVersion = 1;

        [Key(0)]
        public char[] header = ValidHeader;

        [Key(1)]
        public byte version = ValidVersion;
    }

    [MessagePackObject]
    public class SerializableStapleAssetParameter
    {
        [Key(0)]
        public string typeName;

        [Key(1)]
        public object value;
    }

    [MessagePackObject]
    public class SerializableStapleAsset
    {
        [Key(0)]
        public string typeName;

        [Key(1)]
        public Dictionary<string, SerializableStapleAssetParameter> parameters = new();
    }
}
