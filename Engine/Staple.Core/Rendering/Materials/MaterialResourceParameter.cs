using System.Numerics;

namespace Staple.Internal;

internal class MaterialResourceParameter
{
    public string name;
    public MaterialParameterType type;
    public ShaderHandle shaderHandle;
    public int intValue;
    public float floatValue;
    public Vector2 vector2Value;
    public Vector3 vector3Value;
    public Vector4 vector4Value;
    public Color colorValue;
    public Texture textureValue;
    public Matrix3x3 matrix3x3Value = Matrix3x3.Identity;
    public Matrix4x4 matrix4x4Value = Matrix4x4.Identity;
    public TextureWrap textureWrapValue;
    public bool hasTexture;

    public MaterialResourceParameter Clone()
    {
        return new()
        {
            name = name,
            type = type,
            shaderHandle = shaderHandle,
            intValue = intValue,
            floatValue = floatValue,
            vector2Value = vector2Value,
            vector3Value = vector3Value,
            vector4Value = vector4Value,
            colorValue = colorValue,
            textureValue = textureValue,
            matrix3x3Value = matrix3x3Value,
            matrix4x4Value = matrix4x4Value,
            textureWrapValue = textureWrapValue,
            hasTexture = hasTexture,
        };
    }

    public override string ToString()
    {
        return $"{name} ({type})";
    }
}
