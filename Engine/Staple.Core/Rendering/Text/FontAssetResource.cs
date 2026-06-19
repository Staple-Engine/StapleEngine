namespace Staple.Internal;

internal class FontAssetResource
{
    public GuidHasher Guid { get; } = new();

    internal FontMetadata metadata;

    internal TextFont font;
}
