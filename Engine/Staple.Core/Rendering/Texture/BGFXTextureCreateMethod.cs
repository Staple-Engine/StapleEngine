using System;

namespace Staple.Internal;

internal class BGFXTextureCreateMethod(string path, byte[] data, TextureMetadata metadata, TextureFlags flags, byte skip) : ITextureCreateMethod
{
    public string path = path;
    public byte[] data = data;
    public TextureMetadata metadata = metadata;
    public TextureFlags flags = flags;
    public byte skip = skip;

    public bool Create(Texture texture)
    {
        unsafe
        {
            Texture.ProcessFlags(ref flags, metadata);

            /*
            bgfx.TextureInfo info;

            bgfx.Memory* memory = bgfx.alloc((uint)data.Length);

            var source = new Span<byte>(data);

            var target = new Span<byte>(memory->data, data.Length);

            source.CopyTo(target);

            texture.handle = bgfx.create_texture(memory, (ulong)flags, skip, &info);

            if (texture.handle.Valid == false)
            {
                return false;
            }

            texture.Guid.Guid = path;
            texture.metadata = metadata;
            texture.info = info;

            return true;
            */

            return false;
        }
    }
}
