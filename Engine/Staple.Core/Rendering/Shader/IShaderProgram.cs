namespace Staple.Internal;

internal interface IShaderProgram
{
    ShaderType Type { get; }

    void Destroy();
}
