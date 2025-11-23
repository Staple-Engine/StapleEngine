namespace Staple.Internal;

internal interface IShaderProgram
{
    ShaderType Type { get; }

    bool TryGetVertexUniformData(ShaderUniformField field, out byte[] data);

    bool TryGetVertexUniformData(ShaderUniformMapping mapping, out byte[] data);

    bool TryGetFragmentUniformData(ShaderUniformField field, out byte[] data);

    bool TryGetFragmentUniformData(ShaderUniformMapping mapping, out byte[] data);

    bool TryGetComputeUniformData(ShaderUniformField field, out byte[] data);

    bool TryGetComputeUniformData(ShaderUniformMapping mapping, out byte[] data);

    void Destroy();
}
