using Staple;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Tooling;

[Serializable]
public class ShaderReflectionData
{
    [Serializable]
    public class Binding
    {
        public string kind;
        public int space;
        public int index;
    }

    [Serializable]
    public class FieldType
    {
        public string kind;
        public string baseShape;
        public string scalarType;
        public int elementCount;
        public int rowCount;
        public int columnCount;
        public FieldType elementType;
        public FieldType resultType;
        public List<ElementField> fields = [];

        public bool TryGetUniformType(out ShaderUniformType type)
        {
            switch (kind)
            {
                case "vector":

                    switch (elementCount)
                    {
                        case 2:
                            type = ShaderUniformType.Vector2;

                            return true;

                        case 3:

                            type = ShaderUniformType.Vector3;

                            return true;

                        case 4:

                            type = ShaderUniformType.Vector4;

                            return true;

                        default:

                            type = default;

                            return false;
                    }

                case "scalar":

                    switch (scalarType)
                    {
                        case "float32":

                            type = ShaderUniformType.Float;

                            return true;

                        case "int32":
                        case "uint32":

                            type = ShaderUniformType.Int;

                            return true;

                        default:

                            type = default;

                            return false;
                    }

                case "matrix":

                    switch ((rowCount, columnCount))
                    {
                        case (3, 3):

                            type = ShaderUniformType.Matrix3x3;

                            return true;

                        case (4, 4):

                            type = ShaderUniformType.Matrix4x4;

                            return true;

                        default:

                            type = default;

                            return false;
                    }

                case "resource":

                    switch (baseShape)
                    {
                        case "structuredBuffer":

                            if (resultType == null)
                            {
                                type = default;

                                return false;
                            }

                            type = ShaderUniformType.ReadOnlyBuffer;

                            return true;

                        case "texture2D":

                            type = ShaderUniformType.Texture;

                            return true;
                    }

                    break;

                case "struct":

                    type = ShaderUniformType.Structure;

                    return true;
            }

            type = default;

            return false;
        }
    }

    [Serializable]
    public class ElementFieldBinding
    {
        public string kind;
        public int offset;
        public int size;
        public int elementStride;
        public int index;
    }

    [Serializable]
    public class ElementField
    {
        public string name;
        public FieldType type;
        public ElementFieldBinding binding;
    }

    [Serializable]
    public class Parameter
    {
        public string name;
        public Binding binding;
        public FieldType type;
    }

    public List<Parameter> parameters = [];

    public ShaderUniformContainer ToContainer()
    {
        var outValue = new ShaderUniformContainer();

        foreach (var parameter in parameters)
        {
            var data = new ShaderUniformMapping()
            {
                name = parameter.name,
                binding = parameter.binding.index,
            };

            if (parameter.type?.elementType?.fields != null)
            {
                var last = parameter.type.elementType.fields.LastOrDefault();

                data.size = last.binding.offset + last.binding.size;

                foreach (var field in parameter.type.elementType.fields)
                {
                    if (field.type.TryGetUniformType(out var uniformType) == false)
                    {
                        continue;
                    }

                    if(uniformType == ShaderUniformType.Texture)
                    {
                        outValue.textures.Add(new()
                        {
                            name = field.name,
                            binding = field.binding.index,
                            type = uniformType,
                        });
                    }
                    else
                    {
                        data.fields.Add(new()
                        {
                            binding = parameter.binding.index,
                            name = field.name,
                            offset = field.binding.offset,
                            size = field.binding.size,
                            type = uniformType,
                        });
                    }
                }

                if(data.fields.Count == 0)
                {
                    continue;
                }
            }
            else if (parameter.type.TryGetUniformType(out var parameterUniformType))
            {
                data.type = parameterUniformType;

                if((parameterUniformType == ShaderUniformType.ReadOnlyBuffer ||
                    parameterUniformType == ShaderUniformType.ReadWriteBuffer ||
                    parameterUniformType == ShaderUniformType.WriteOnlyBuffer))
                {
                    FieldType fieldType = null;

                    ShaderUniformType? elementType = null;

                    if(parameter.type.resultType != null)
                    {
                        if(parameter.type.resultType.TryGetUniformType(out var type))
                        {
                            elementType = type;
                            fieldType = parameter.type.resultType;

                            data.elementType = new()
                            {
                                type = type,
                                size = type switch
                                {
                                    ShaderUniformType.Int or ShaderUniformType.Float => sizeof(int),
                                    ShaderUniformType.Color or ShaderUniformType.Vector4 => Marshal.SizeOf<Vector4>(),
                                    ShaderUniformType.Vector3 => Marshal.SizeOf<Vector3>(),
                                    ShaderUniformType.Vector2 => Marshal.SizeOf<Vector2>(),
                                    ShaderUniformType.Matrix3x3 => Marshal.SizeOf<Matrix3x3>(),
                                    ShaderUniformType.Matrix4x4 => Marshal.SizeOf<Matrix4x4>(),
                                    _ => 0,
                                },
                            };
                        }
                    }
                    
                    if(elementType == null && parameter.type.elementType != null)
                    {
                        if(parameter.type.elementType.TryGetUniformType(out var type))
                        {
                            elementType = type;
                            fieldType = parameter.type.elementType;

                            data.elementType = new()
                            {
                                type = type,
                                size = type switch
                                {
                                    ShaderUniformType.Int or ShaderUniformType.Float => sizeof(int),
                                    ShaderUniformType.Color or ShaderUniformType.Vector4 => Marshal.SizeOf<Vector4>(),
                                    ShaderUniformType.Vector3 => Marshal.SizeOf<Vector3>(),
                                    ShaderUniformType.Vector2 => Marshal.SizeOf<Vector2>(),
                                    ShaderUniformType.Matrix3x3 => Marshal.SizeOf<Matrix3x3>(),
                                    ShaderUniformType.Matrix4x4 => Marshal.SizeOf<Matrix4x4>(),
                                    _ => 0,
                                },
                            };
                        }
                    }

                    if (elementType == ShaderUniformType.Structure)
                    {
                        data.elementType.fields = [];

                        var last = fieldType.fields.LastOrDefault();

                        data.elementType.size = last.binding.offset + last.binding.size;

                        foreach (var field in fieldType.fields)
                        {
                            if (field.type.TryGetUniformType(out var uniformType) == false)
                            {
                                continue;
                            }

                            if (uniformType == ShaderUniformType.Texture)
                            {
                                continue;
                            }

                            data.elementType.fields.Add(new()
                            {
                                binding = parameter.binding.index,
                                name = field.name,
                                offset = field.binding.offset,
                                size = field.binding.size,
                                type = uniformType,
                            });
                        }
                    }
                }

                if(parameterUniformType != ShaderUniformType.ReadOnlyBuffer &&
                    parameterUniformType != ShaderUniformType.WriteOnlyBuffer &&
                    parameterUniformType != ShaderUniformType.ReadWriteBuffer)
                {
                    data.size = parameterUniformType switch
                    {
                        ShaderUniformType.Int or ShaderUniformType.Float => sizeof(int),
                        ShaderUniformType.Color or ShaderUniformType.Vector4 => Marshal.SizeOf<Vector4>(),
                        ShaderUniformType.Vector3 => Marshal.SizeOf<Vector3>(),
                        ShaderUniformType.Vector2 => Marshal.SizeOf<Vector2>(),
                        ShaderUniformType.Matrix3x3 => Marshal.SizeOf<Matrix3x3>(),
                        ShaderUniformType.Matrix4x4 => Marshal.SizeOf<Matrix4x4>(),
                        _ => 0,
                    };
                }
            }

            if(data.type == ShaderUniformType.Texture)
            {
                outValue.textures.Add(data);
            }
            else if(data.type == ShaderUniformType.ReadOnlyBuffer ||
                data.type == ShaderUniformType.ReadWriteBuffer ||
                data.type == ShaderUniformType.WriteOnlyBuffer)
            {
                outValue.storageBuffers.Add(data);
            }
            else
            {
                outValue.uniforms.Add(data);
            }
        }

        return outValue;
    }
}
