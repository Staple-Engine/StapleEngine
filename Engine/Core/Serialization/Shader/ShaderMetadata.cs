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
    Float,
    Vector2,
    Vector3,
    Vector4,
    Color,
    Texture,
    Matrix3x3,
    Matrix4x4
}

[MessagePackObject]
public class ShaderUniform
{
    [Key(0)]
    public string name;

    [Key(1)]
    public ShaderUniformType type;
}

[MessagePackObject]
public class ShaderMetadata
{
    [Key(0)]
    public string guid = Guid.NewGuid().ToString();

    [Key(1)]
    public ShaderType type = ShaderType.VertexFragment;

    [Key(2)]
    public List<ShaderUniform> uniforms = new();

    [Key(3)]
    public BlendMode sourceBlend = BlendMode.Off;

    [Key(4)]
    public BlendMode destinationBlend = BlendMode.Off;

    [Key(5)]
    public string typeName = typeof(Shader).FullName;
}
