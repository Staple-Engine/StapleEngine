using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

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
}

[MessagePackObject]
public class ShaderUniformTypeInfo
{
    [Key(0)]
    public ShaderUniformType type;

    [Key(1)]
    public int size;

    [Key(2)]
    public List<ShaderUniformField> fields;
}

[MessagePackObject]
public class ShaderUniformMapping
{
    [Key(0)]
    public List<ShaderUniformField> fields = [];

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

    public void Merge(ShaderUniformContainer other)
    {
        static void Add(List<ShaderUniformMapping> container, ShaderUniformMapping target)
        {
            foreach(var uniform in container)
            {
                if(uniform.name == target.name)
                {
                    return;
                }
            }

            container.Add(target);
        }

        foreach(var uniform in other.uniforms)
        {
            Add(uniforms, uniform);
        }

        foreach (var uniform in other.textures)
        {
            Add(textures, uniform);
        }

        foreach (var uniform in other.storageBuffers)
        {
            Add(storageBuffers, uniform);
        }
    }
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
