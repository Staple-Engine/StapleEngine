namespace Staple.Internal;

internal class PixelTextureCreateMethod(string path, byte[] data, ushort width, ushort height, TextureMetadata metadata, TextureFormat format, TextureFlags flags) :
    ITextureCreateMethod
{
    public string path = path;
    public byte[] data = data;
    public ushort width = width;
    public ushort height = height;
    public TextureMetadata metadata = metadata;
    public TextureFormat format = format;
    public TextureFlags flags = flags;

    public bool Create(Texture texture)
    {
        unsafe
        {
            Texture.ProcessFlags(ref flags, metadata);

            texture.impl?.Destroy();

            texture.impl = RenderSystem.Backend.CreatePixelTexture(data, width, height, format, flags);

            if(texture.impl == null)
            {
                return false;
            }

            texture.Guid.Guid = path;
            texture.metadata = metadata;

            return true;
        }
    }
}
