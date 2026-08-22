using System;

namespace Staple.Internal;

/// <summary>
/// Shader Handle.
/// Direct access to a shader uniform, used for caching.
/// </summary>
/// <param name="uniform">The uniform to store</param>
public readonly struct ShaderHandle
{
    internal readonly ShaderUniformInfo uniform;
    internal readonly WeakReference<object> owner;

    public ShaderHandle(object owner, ShaderUniformInfo uniform)
    {
        if(owner is not IDisposableAsset ||
            owner is not IGuidAsset)
        {
            throw new ArgumentException("Owner needs to implement both IDisposableAsset and IGuidAsset!", nameof(owner));
        }

        this.owner = new(owner);
        this.uniform = uniform;
    }

    internal bool TryGetUniform(IGuidAsset owner, out ShaderUniformInfo uniform)
    {
        if(IsValid &&
            this.owner.TryGetTarget(out var actualOwner) &&
            actualOwner is IGuidAsset guidAsset &&
            guidAsset.Guid.GuidHash == owner.Guid.GuidHash)
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
    public bool IsValid => uniform != null &&
        (owner?.TryGetTarget(out var s) ?? false) &&
        s is IDisposableAsset disposable &&
        !disposable.Disposed;

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

    public override string ToString()
    {
        return uniform != null ? $"{uniform.uniform.name} ({IsValid})" : IsValid.ToString();
    }
}
