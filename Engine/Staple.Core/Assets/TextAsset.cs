using Staple.Internal;

namespace Staple;

public class TextAsset : IGuidAsset
{
    public string text;

    public byte[] bytes;

    public GuidHasher Guid { get; } = new();

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadTextAsset(guid);
    }
}
