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
        internal string path;
        internal bool renderTarget = false;

        private bool destroyed = false;

        public int Width => info.width;

        public int Height => info.height;

        public int SpriteWidth => (int)(info.width * metadata.spriteScale);

        public int SpriteHeight => (int)(info.height * metadata.spriteScale);

        internal Texture(string path, TextureMetadata metadata, bgfx.TextureHandle handle, bgfx.TextureInfo info)
        {
            this.path = path;
            this.metadata = metadata;
            this.handle = handle;
            this.info = info;
        }

        internal Texture(bgfx.TextureHandle handle, ushort width, ushort height, bool renderTarget)
        {
            this.handle = handle;
            this.renderTarget = renderTarget;

            metadata = new TextureMetadata()
            {
                spriteScale = 1,
            };

            info = new bgfx.TextureInfo()
            {
                width = width,
                height = height,
            };
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

        public static Texture CreateEmpty(ushort width, ushort height, bool hasMips, ushort layers, bgfx.TextureFormat format, TextureFlags flags = TextureFlags.None)
        {
            unsafe
            {
                var handle = bgfx.create_texture_2d(width, height, hasMips, layers, format, (ulong)flags, null);

                if(handle.Valid == false)
                {
                    return null;
                }

                var renderTarget = flags.HasFlag(TextureFlags.RenderTarget);

                return new Texture(handle, width, height, renderTarget);
            }
        }

        internal static Texture CreatePixels(string path, byte[] data, ushort width, ushort height, TextureMetadata metadata,
            bgfx.TextureFormat format, TextureFlags flags = TextureFlags.None, byte skip = 0)
        {
            unsafe
            {
                switch (metadata.type)
                {
                    case TextureType.SRGB:

                        flags |= TextureFlags.SRGB;

                        break;
                }

                switch (metadata.wrapU)
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

                fixed (void* ptr = data)
                {
                    bgfx.Memory* memory = bgfx.copy(ptr, (uint)data.Length);

                    var handle = bgfx.create_texture_2d(width, height, metadata.useMipmaps, 1, format, (ulong)flags, memory);

                    if (handle.Valid == false)
                    {
                        return null;
                    }

                    return new Texture(path, metadata, handle, new bgfx.TextureInfo()
                    {
                        bitsPerPixel = 24,
                        format = format,
                        height = height,
                        width = width,
                        numLayers = 1,
                    });
                }
            }
        }

        internal static Texture Create(string path, byte[] data, TextureMetadata metadata, TextureFlags flags = TextureFlags.None, byte skip = 0)
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

                    return new Texture(path, metadata, handle, info);
                }
            }
        }
    }
}
