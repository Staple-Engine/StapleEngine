namespace Staple;

/// <summary>
/// Describes an asset which can be manually disposed and its status checked
/// </summary>
public interface IDisposableAsset
{
    /// <summary>
    /// The status of the asset
    /// </summary>
    bool Disposed { get; }

    /// <summary>
    /// Destroy this asset
    /// </summary>
    void Destroy();
}
