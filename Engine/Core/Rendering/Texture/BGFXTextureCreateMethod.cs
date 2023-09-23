using Bgfx;

namespace Staple.Internal
{
    internal class BGFXTextureCreateMethod : ITextureCreateMethod
    {
        public string path;
        public byte[] data;
        public TextureMetadata metadata;
        public TextureFlags flags;
        public byte skip;

        public BGFXTextureCreateMethod(string path, byte[] data, TextureMetadata metadata, TextureFlags flags, byte skip)
        {
            this.path = path;
            this.data = data;
            this.metadata = metadata;
            this.flags = flags;
            this.skip = skip;
        }

        public bool Create(Texture texture)
        {
            unsafe
            {
                Texture.ProcessFlags(ref flags, metadata);

                bgfx.TextureInfo info;

                fixed (void* ptr = data)
                {
                    bgfx.Memory* memory = bgfx.copy(ptr, (uint)data.Length);

                    texture.handle = bgfx.create_texture(memory, (ulong)flags, skip, &info);

                    if (texture.handle.Valid == false)
                    {
                        return false;
                    }

                    texture.path = path;
                    texture.metadata = metadata;
                    texture.info = info;

                    return true;
                }
            }
        }
    }
}
