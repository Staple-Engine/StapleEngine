using Staple.Internal;

namespace Staple;

public class TextAsset : IStapleAsset, IGuidAsset
{
    public string text;

    public byte[] bytes;

    public int GuidHash { get; set; }

    public string Guid { get; set; }

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadAsset<TextAsset>(guid);
    }
}
