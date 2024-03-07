using Staple.Internal;

namespace Staple;

public class Prefab : IGuidAsset
{
    internal SerializablePrefab data;

    public string Guid { get; set; }

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadPrefab(guid);
    }
}
