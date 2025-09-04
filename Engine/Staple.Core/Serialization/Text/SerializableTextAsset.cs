using MessagePack;

namespace Staple.Internal;

[MessagePackObject]
public class SerializableTextAssetHeader
{
    [IgnoreMember]
    public readonly static char[] ValidHeader =
    [
        'S', 'T', 'X', 'T'
    ];

    [IgnoreMember]
    public const byte ValidVersion = 1;

    [Key(0)]
    public char[] header = ValidHeader;

    [Key(1)]
    public byte version = ValidVersion;
}

[MessagePackObject]
public class SerializableTextAsset
{
    [Key(0)]
    public TextAssetMetadata metadata;

    [Key(1)]
    public byte[] data;
}
