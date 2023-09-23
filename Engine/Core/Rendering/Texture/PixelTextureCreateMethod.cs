using Bgfx;

namespace Staple.Internal
{
    internal class PixelTextureCreateMethod : ITextureCreateMethod
    {
        public string path;
        public byte[] data;
        public ushort width;
        public ushort height;
        public TextureMetadata metadata;
        public bgfx.TextureFormat format;
        public TextureFlags flags;

        public PixelTextureCreateMethod(string path, byte[] data, ushort width, ushort height, TextureMetadata metadata, bgfx.TextureFormat format, TextureFlags flags)
        {
            this.path = path;
            this.data = data;
            this.width = width;
            this.height = height;
            this.metadata = metadata;
            this.format = format;
            this.flags = flags;
        }

        public bool Create(Texture texture)
        {
            unsafe
            {
                Texture.ProcessFlags(ref flags, metadata);

                fixed (void* ptr = data)
                {
                    bgfx.Memory* memory = bgfx.copy(ptr, (uint)data.Length);

                    texture.handle = bgfx.create_texture_2d(width, height, metadata.useMipmaps, 1, format, (ulong)flags, memory);

                    if (texture.handle.Valid == false)
                    {
                        return false;
                    }

                    texture.path = path;
                    texture.metadata = metadata;
                    texture.info = new bgfx.TextureInfo()
                    {
                        bitsPerPixel = 24,
                        format = format,
                        height = height,
                        width = width,
                        numLayers = 1,
                    };

                    return true;
                }
            }
        }
    }
}
