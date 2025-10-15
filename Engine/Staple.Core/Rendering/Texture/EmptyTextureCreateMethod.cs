namespace Staple.Internal;

internal class EmptyTextureCreateMethod(ushort width, ushort height, bool hasMips, ushort layers, TextureFormat format, TextureFlags flags) : ITextureCreateMethod
{
    public ushort width = width;
    public ushort height = height;
    public bool hasMips = hasMips;
    public ushort layers = layers;
    public TextureFormat format = format;
    public TextureFlags flags = flags;

    public bool Create(Texture texture)
    {
        unsafe
        {
            return false;

            /*
            var handle = bgfx.create_texture_2d(width, height, hasMips, layers, BGFXUtils.GetTextureFormat(format), (ulong)flags, null);

            if (handle.Valid == false)
            {
                return false;
            }

            var renderTarget = flags.HasFlag(TextureFlags.RenderTarget);
            var readBack = flags.HasFlag(TextureFlags.ReadBack);

            texture.handle = handle;
            texture.renderTarget = renderTarget;
            texture.metadata = new()
            {
                readBack = readBack,
            };

            texture.info = new bgfx.TextureInfo()
            {
                width = width,
                height = height,
                format = BGFXUtils.GetTextureFormat(format),
                numLayers = layers,
                numMips = (byte)(hasMips ? 1 : 0),
                storageSize = (uint)(format == TextureFormat.RGBA8 ? 4 * width * height :
                    format == TextureFormat.RGB8 ? 3 * width * height :
                    0),
            };

            return true;
            */
        }
    }
}