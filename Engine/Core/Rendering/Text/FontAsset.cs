using Staple.Internal;

namespace Staple;

public class FontAsset : IGuidAsset
{
    public string Guid { get; set; }

    internal FontMetadata metadata;

    internal TextFont font;

    public int FontSize
    {
        get => font?.FontSize ?? 0;

        set
        {
            if(value <= 0 || font == null)
            {
                return;
            }

            font.FontSize = value;
        }
    }

    public static object Create(string guid)
    {
        return ResourceManager.instance.LoadFont(guid);
    }
}
