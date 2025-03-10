using Staple.Internal;

namespace Staple;

public class TextAsset : IStapleAsset, IGuidAsset
{
    public string text;

    public byte[] bytes;

    private readonly GuidHasher guidHasher = new();

    public GuidHasher Guid => guidHasher;

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadAsset<TextAsset>(guid);
    }
}
