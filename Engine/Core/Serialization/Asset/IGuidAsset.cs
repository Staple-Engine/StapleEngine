namespace Staple;

/// <summary>
/// Describes an asset that can be serialized as its guid rather than its full data when used as a field
/// </summary>
public interface IGuidAsset
{
    /// <summary>
    /// The asset's guid hash (or 0)
    /// </summary>
    int GuidHash { get; }

    /// <summary>
    /// The asset's guid (if any)
    /// </summary>
    string Guid { get; set; }

    static abstract object Create(string guid);
}
