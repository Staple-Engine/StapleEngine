using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    [MessagePackObject]
    public class SerializableMaterialHeader
    {
        [IgnoreMember]
        public readonly static char[] ValidHeader = new char[]
        {
            'S', 'M', 'A', 'T'
        };

        [IgnoreMember]
        public const byte ValidVersion = 1;

        [Key(0)]
        public char[] header = ValidHeader;

        [Key(1)]
        public byte version = ValidVersion;
    }

    [MessagePackObject]
    public class SerializableMaterial
    {
        [Key(0)]
        public MaterialMetadata metadata;
    }
}
