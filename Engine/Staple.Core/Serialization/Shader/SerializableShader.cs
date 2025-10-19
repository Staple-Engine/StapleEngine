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
public class VertexFragmentShaderMetrics
{
    [Key(0)]
    public int samplerCount;

    [Key(1)]
    public int storageTextureCount;

    [Key(2)]
    public int storageBufferCount;

    [Key(3)]
    public int uniformBufferCount;
}

[MessagePackObject]
public class ComputeShaderMetrics
{
    [Key(0)]
    public int samplerCount;

    [Key(1)]
    public int readOnlyStorageTextureCount;

    [Key(2)]
    public int readOnlyStorageBufferCount;

    [Key(3)]
    public int readWriteStorageTextureCount;

    [Key(4)]
    public int readWriteStorageBufferCount;

    [Key(5)]
    public int uniformBufferCount;

    [Key(6)]
    public int threadCountX;

    [Key(7)]
    public int threadCountY;

    [Key(8)]
    public int threadCountZ;
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

    [Key(3)]
    public VertexFragmentShaderMetrics vertexMetrics = new();

    [Key(4)]
    public VertexFragmentShaderMetrics fragmentMetrics = new();

    [Key(5)]
    public ComputeShaderMetrics computeMetrics = new();
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
