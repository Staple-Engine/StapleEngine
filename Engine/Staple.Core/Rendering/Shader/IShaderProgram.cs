namespace Staple.Internal;

internal interface IShaderProgram
{
    ShaderType Type { get; }

    bool TryGetUniformData(byte binding, out byte[] data);

    void Destroy();
}
