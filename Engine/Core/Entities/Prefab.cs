using Staple.Internal;

namespace Staple;

/// <summary>
/// Represents a prefab asset
/// </summary>
public class Prefab : IGuidAsset
{
    internal SerializablePrefab data;

    private int guidHash;
    private string guid;

    public int GuidHash => guidHash;

    public string Guid
    {
        get => guid;

        set
        {
            guid = value;

            guidHash = guid?.GetHashCode() ?? 0;
        }
    }

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadPrefab(guid);
    }
}
