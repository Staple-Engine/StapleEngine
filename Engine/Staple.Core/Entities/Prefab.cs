using Staple.Internal;

namespace Staple;

/// <summary>
/// Represents a prefab asset
/// </summary>
public sealed class Prefab : IGuidAsset
{
    internal SerializablePrefab data;

    public GuidHasher Guid { get; } = new();

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadPrefab(guid);
    }
}
