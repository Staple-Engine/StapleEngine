namespace Staple;

/// <summary>
/// Describes an asset that can be serialized as its guid rather than its full data when used as a field
/// </summary>
public interface IGuidAsset
{
    /// <summary>
    /// The asset's guid and hash
    /// </summary>
    GuidHasher Guid { get; }

    static abstract object Create(string guid);
}
