namespace Staple.Internal;

/// <summary>
/// Shader Handle.
/// Direct access to a shader uniform, used for caching.
/// </summary>
/// <param name="uniform">The uniform to store</param>
public readonly record struct ShaderHandle(Shader.UniformInfo uniform);
