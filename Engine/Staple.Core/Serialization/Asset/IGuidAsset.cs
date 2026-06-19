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

    /// <summary>
    /// Attempts to create an instance of this asset from a guid or path
    /// </summary>
    /// <param name="guid">The guid or path</param>
    /// <returns>The instance or null</returns>
    static abstract object Create(string guid);
}
