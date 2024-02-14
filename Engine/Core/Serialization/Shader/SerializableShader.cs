using MessagePack;

namespace Staple.Internal;

[MessagePackObject]
public class SerializableShaderHeader
{
    [IgnoreMember]
    public readonly static char[] ValidHeader = new char[]
    {
        'S', 'S', 'H', 'D'
    };

    [IgnoreMember]
    public const byte ValidVersion = 1;

    [Key(0)]
    public char[] header = ValidHeader;

    [Key(1)]
    public byte version = ValidVersion;
}

[MessagePackObject]
public class SerializableShader
{
    [Key(0)]
    public ShaderMetadata metadata;

    [Key(1)]
    public byte[] vertexShader;

    [Key(2)]
    public byte[] fragmentShader;

    [Key(3)]
    public byte[] computeShader;
}
