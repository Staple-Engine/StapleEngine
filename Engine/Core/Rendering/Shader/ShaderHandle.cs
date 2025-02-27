using System;

namespace Staple.Internal;

/// <summary>
/// Shader Handle.
/// Direct access to a shader uniform, used for caching.
/// </summary>
/// <param name="uniform">The uniform to store</param>
public readonly struct ShaderHandle(Shader.UniformInfo uniform)
{
    internal readonly Shader.UniformInfo uniform = uniform;

    internal bool TryGetUniform(out Shader.UniformInfo uniform)
    {
        if(IsValid)
        {
            uniform = this.uniform;

            return true;
        }

        if(this.uniform == null)
        {
            uniform = null;

            return false;
        }

        throw new ArgumentException("Shader Handle is no longer valid. You should ensure your shader handles are validated as editor reloads can make them invalid!");
    }

    public bool IsValid => uniform?.handle.Valid ?? false;
}
