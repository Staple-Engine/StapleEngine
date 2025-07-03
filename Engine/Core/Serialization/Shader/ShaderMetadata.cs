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
    public List<string> variants = [];

    [Key(3)]
    public List<ShaderUniform> uniforms = [];

    [Key(4)]
    public List<ShaderInstanceParameter> instanceParameters = [];

    [Key(5)]
    public BlendMode sourceBlend = BlendMode.Off;

    [Key(6)]
    public BlendMode destinationBlend = BlendMode.Off;

    [HideInInspector]
    [Key(7)]
    public string typeName = typeof(Shader).FullName;
}
