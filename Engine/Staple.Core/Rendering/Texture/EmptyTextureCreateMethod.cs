namespace Staple.Internal;

internal class EmptyTextureCreateMethod(int width, int height, TextureFormat format, TextureFlags flags) : ITextureCreateMethod
{
    public int width = width;
    public int height = height;
    public TextureFormat format = format;
    public TextureFlags flags = flags;

    public bool Create(Texture texture)
    {
        unsafe
        {
            texture.impl?.Destroy();

            texture.impl = RenderSystem.Backend.CreateEmptyTexture(width, height, format, flags);

            if (texture.impl == null)
            {
                return false;
            }

            return true;
        }
    }
}