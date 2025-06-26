using System;

namespace Staple.Internal;

/// <summary>
/// Shader Handle.
/// Direct access to a shader uniform, used for caching.
/// </summary>
/// <param name="uniform">The uniform to store</param>
public readonly struct ShaderHandle(object owner, Shader.UniformInfo uniform)
{
    internal readonly Shader.UniformInfo uniform = uniform;
    internal readonly WeakReference<object> owner = new(owner);

    internal bool TryGetUniform(object owner, out Shader.UniformInfo uniform)
    {
        if(IsValid &&
            this.owner.TryGetTarget(out var actualOwner) &&
            ReferenceEquals(actualOwner, owner))
        {
            uniform = this.uniform;

            return true;
        }

        uniform = null;

        return false;
    }

    public bool IsValid => (uniform?.handle.Valid ?? false) && (owner?.TryGetTarget(out _) ?? false);
}
