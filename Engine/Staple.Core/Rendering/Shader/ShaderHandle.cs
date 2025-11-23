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

    /// <summary>
    /// Whether this handle is valid
    /// </summary>
    public bool IsValid => uniform != null && (owner?.TryGetTarget(out _) ?? false);

    /// <summary>
    /// The uniform's attribute, if any
    /// </summary>
    public string Attribute => IsValid ? uniform.uniform.attribute : null;

    /// <summary>
    /// The uniform's variant, if any
    /// </summary>
    public string Variant => IsValid ? uniform.uniform.variant : null;

    /// <summary>
    /// The default value of this uniform, if any
    /// </summary>
    public string DefaultValue => IsValid ? uniform.uniform.defaultValue : null;
}
