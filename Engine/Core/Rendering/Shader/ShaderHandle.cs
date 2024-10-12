namespace Staple.Internal;

/// <summary>
/// Shader Handle.
/// Direct access to a shader uniform, used for caching.
/// </summary>
/// <param name="uniform">The uniform to store</param>
internal class ShaderHandle(Shader.UniformInfo uniform)
{
    public readonly Shader.UniformInfo uniform = uniform;
}
