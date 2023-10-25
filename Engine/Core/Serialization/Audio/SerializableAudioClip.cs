using MessagePack;

namespace Staple.Internal
{
    [MessagePackObject]
    public class SerializableAudioClipHeader
    {
        [IgnoreMember]
        public readonly static char[] ValidHeader = new char[]
        {
            'S', 'T', 'A', 'C'
        };

        [IgnoreMember]
        public const byte ValidVersion = 1;

        [Key(0)]
        public char[] header = ValidHeader;

        [Key(1)]
        public byte version = ValidVersion;
    }

    [MessagePackObject]
    public class SerializableAudioClip
    {
        [Key(0)]
        public AudioClipMetadata metadata;
    }
}
