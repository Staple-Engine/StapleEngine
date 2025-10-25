namespace Staple.Internal;

internal class TextureAssetCreateMethod(string path, SerializableTexture asset, TextureFlags flags) : ITextureCreateMethod
{
    public string path = path;
    public SerializableTexture asset = asset;
    public TextureFlags flags = flags;

    public bool Create(Texture texture)
    {
        unsafe
        {
            Texture.ProcessFlags(ref flags, asset.metadata);

            texture.impl?.Destroy();

            texture.impl = RenderSystem.Backend.CreateTextureAssetTexture(asset, flags);

            if(texture.impl == null)
            {
                return false;
            }

            texture.Guid.Guid = path;
            texture.metadata = asset.metadata;

            return true;
        }
    }
}
