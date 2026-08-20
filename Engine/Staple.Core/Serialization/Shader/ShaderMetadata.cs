using MessagePack;
using System;
using System.Collections.Generic;

namespace Staple.Internal;

public enum ShaderType
{
    VertexFragment,
    Compute
}

public enum ShaderUniformType
{
    Int,
    Float,
    Vector2,
    Vector3,
    Vector4,
    Color,
    Texture,
    Matrix3x3,
    Matrix4x4,
    ReadOnlyBuffer,
    WriteOnlyBuffer,
    ReadWriteBuffer,
    Structure,
    Array,
}

[MessagePackObject]
public class ShaderUniform
{
    [Key(0)]
    public string name;

    [Key(1)]
    public ShaderUniformType type;

    [Key(2)]
    public int slot;

    [Key(3)]
    public string attribute;

    [Key(4)]
    public string variant;

    [Key(5)]
    public string defaultValue;
}

[MessagePackObject]
public class ShaderUniformField
{
    [Key(0)]
    public string name;

    [Key(1)]
    public ShaderUniformType type;

    [Key(2)]
    public int offset;

    [Key(3)]
    public int size;

    [Key(4)]
    public int binding;

    [Key(5)]
    public int count;
}

[MessagePackObject]
public class ShaderUniformTypeInfo
{
    [Key(0)]
    public ShaderUniformType type;

    [Key(1)]
    public int size;

    [Key(2)]
    public ShaderUniformField[] fields;
}

[MessagePackObject]
public class ShaderUniformMapping
{
    [Key(0)]
    public ShaderUniformField[] fields;

    [Key(1)]
    public int binding;

    [Key(2)]
    public int size;

    [Key(3)]
    public string name;

    [Key(4)]
    public ShaderUniformType type;

    [Key(5)]
    public ShaderUniformTypeInfo elementType;

    [Key(6)]
    public int count;
}

[MessagePackObject]
public class ShaderVertexAttributeData
{
    [Key(0)]
    public VertexAttribute attribute;

    [Key(1)]
    public VertexAttributeType attributeType;
}

[MessagePackObject]
public class ShaderUniformContainer
{
    [Key(0)]
    public List<ShaderUniformMapping> uniforms = [];

    [Key(1)]
    public List<ShaderUniformMapping> textures = [];

    [Key(2)]
    public List<ShaderUniformMapping> storageBuffers = [];

    [Key(3)]
    public List<ShaderVertexAttributeData> vertexAttributes = [];
}

[MessagePackObject]
public class ShaderInstanceParameter
{
    [Key(0)]
    public string name;

    [Key(1)]
    public ShaderUniformType type;
}

[MessagePackObject]
public class ShaderMetadata
{
    [HideInInspector]
    [Key(0)]
    public string guid = Guid.NewGuid().ToString();

    [Key(1)]
    public ShaderType type = ShaderType.VertexFragment;

    [Key(2)]
    public string[] variants = [];

    [Key(3)]
    public ShaderUniform[] uniforms = [];

    [Key(4)]
    public ShaderInstanceParameter[] instanceParameters = [];

    [Key(5)]
    public BlendMode sourceBlend = BlendMode.Off;

    [Key(6)]
    public BlendMode destinationBlend = BlendMode.Off;

    [Key(7)]
    public MaterialRenderQueue renderQueue;

    [Key(8)]
    public int renderQueueOffset;

    [HideInInspector]
    [Key(9)]
    public string typeName = typeof(Shader).FullName;
}
