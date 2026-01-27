namespace Staple.Internal;

internal interface IShaderProgram
{
    ShaderType Type { get; }

    int StateKey { get; }

    void Destroy();
}
