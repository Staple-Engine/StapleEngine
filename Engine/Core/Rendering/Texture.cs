using Bgfx;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class Texture
    {
        internal readonly bgfx.TextureHandle handle;
        internal readonly bgfx.TextureInfo info;
        internal readonly TextureMetadata metadata;
        private bool destroyed = false;

        public int Width => info.width;

        public int Height => info.height;

        public int SpriteWidth => (int)(info.width * metadata.spriteScale);

        public int SpriteHeight => (int)(info.height * metadata.spriteScale);

        internal Texture(TextureMetadata metadata, bgfx.TextureHandle handle, bgfx.TextureInfo info)
        {
            this.metadata = metadata;
            this.handle = handle;
            this.info = info;
        }

        ~Texture()
        {
            Destroy();
        }

        internal void Destroy()
        {
            if(destroyed)
            {
                return;
            }

            destroyed = true;

            if(handle.Valid)
            {
                bgfx.destroy_texture(handle);
            }
        }

        internal void SetActive(byte stage, bgfx.UniformHandle sampler)
        {
            bgfx.set_texture(stage, sampler, handle, uint.MaxValue);
        }

        internal static Texture Create(byte[] data, TextureMetadata metadata, TextureFlags flags = TextureFlags.None, byte skip = 0)
        {
            unsafe
            {
                switch(metadata.type)
                {
                    case TextureType.SRGB:

                        flags |= TextureFlags.SRGB;

                        break;
                }

                switch(metadata.wrapU)
                {
                    case TextureWrap.Repeat:
                        //This is the default

                        break;

                    case TextureWrap.Mirror:
                        flags |= TextureFlags.SamplerUMirror;

                        break;

                    case TextureWrap.Clamp:
                        flags |= TextureFlags.SamplerUClamp;

                        break;
                }

                switch (metadata.wrapV)
                {
                    case TextureWrap.Repeat:
                        //This is the default

                        break;

                    case TextureWrap.Mirror:
                        flags |= TextureFlags.SamplerVMirror;

                        break;

                    case TextureWrap.Clamp:
                        flags |= TextureFlags.SamplerVClamp;

                        break;
                }

                switch (metadata.wrapW)
                {
                    case TextureWrap.Repeat:
                        //This is the default

                        break;

                    case TextureWrap.Mirror:
                        flags |= TextureFlags.SamplerWMirror;

                        break;

                    case TextureWrap.Clamp:
                        flags |= TextureFlags.SamplerWClamp;

                        break;
                }

                switch (metadata.filter)
                {
                    case TextureFilter.Linear:
                        //This is the default

                        break;

                    case TextureFilter.Point:

                        flags |= TextureFlags.SamplerMagPoint;
                        flags |= TextureFlags.SamplerMinPoint;
                        flags |= TextureFlags.SamplerMipPoint;

                        break;

                    case TextureFilter.Anisotropic:

                        flags |= TextureFlags.SamplerMagAnisotropic;
                        flags |= TextureFlags.SamplerMinAnisotropic;

                        break;
                }

                bgfx.TextureInfo info;

                fixed(void *ptr = data)
                {
                    bgfx.Memory* memory = bgfx.copy(ptr, (uint)data.Length);

                    var handle = bgfx.create_texture(memory, (ulong)flags, skip, &info);

                    if(handle.Valid == false)
                    {
                        return null;
                    }

                    return new Texture(metadata, handle, info);
                }
            }
        }
    }
}
