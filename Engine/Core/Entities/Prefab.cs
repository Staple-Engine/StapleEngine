using Staple.Internal;

namespace Staple;

/// <summary>
/// Represents a prefab asset
/// </summary>
public sealed class Prefab : IGuidAsset
{
    internal SerializablePrefab data;

    private readonly GuidHasher guidHasher = new();

    public GuidHasher Guid => guidHasher;

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadPrefab(guid);
    }
}
