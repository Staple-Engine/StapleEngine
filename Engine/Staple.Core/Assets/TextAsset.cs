using Staple.Internal;

namespace Staple;

public class TextAsset : IGuidAsset
{
    internal TextAssetResource textResource;

    public string Text => textResource?.text;

    public byte[] Bytes => textResource?.bytes;

    public GuidHasher Guid => textResource?.Guid ?? new();

    public static object Create(string guid) => ResourceManager.instance.LoadTextAsset(guid);
}
