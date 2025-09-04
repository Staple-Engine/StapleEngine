using MessagePack;
using System.Collections.Generic;

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
public class SerializableShaderData
{
    [Key(0)]
    public byte[] vertexShader;

    [Key(1)]
    public byte[] fragmentShader;

    [Key(2)]
    public byte[] computeShader;
}

[MessagePackObject]
public class SerializableShaderEntry
{
    [Key(0)]
    public Dictionary<string, SerializableShaderData> data = [];
}

[MessagePackObject]
public class SerializableShader
{
    /// <summary>
    /// Shader metadata
    /// </summary>
    [Key(0)]
    public ShaderMetadata metadata;

    /// <summary>
    /// Renderer Type -> List of variants
    /// </summary>
    [Key(1)]
    public Dictionary<RendererType, SerializableShaderEntry> data = [];
}
