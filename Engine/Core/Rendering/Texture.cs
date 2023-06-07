using Bgfx;
using Staple.Internal;
using StbImageSharp;

namespace Staple
{
    /// <summary>
    /// Texture resource
    /// </summary>
    public class Texture
    {
        internal readonly bgfx.TextureHandle handle;
        internal readonly bgfx.TextureInfo info;
        internal readonly TextureMetadata metadata;
        internal string path;
        internal bool renderTarget = false;

        public bool Disposed { get; private set; } = false;

        /// <summary>
        /// The texture's width
        /// </summary>
        public int Width => info.width;

        /// <summary>
        /// The texture's height
        /// </summary>
        public int Height => info.height;

        /// <summary>
        /// The texture's width as a sprite
        /// </summary>
        public int SpriteWidth => (int)(info.width * metadata.spriteScale);

        /// <summary>
        /// The texture's height as a sprite
        /// </summary>
        public int SpriteHeight => (int)(info.height * metadata.spriteScale);

        internal Texture(string path, TextureMetadata metadata, bgfx.TextureHandle handle, bgfx.TextureInfo info)
        {
            this.path = path;
            this.metadata = metadata;
            this.handle = handle;
            this.info = info;
        }

        internal Texture(bgfx.TextureHandle handle, ushort width, ushort height, bool readBack, bool renderTarget)
        {
            this.handle = handle;
            this.renderTarget = renderTarget;

            metadata = new TextureMetadata()
            {
                spriteScale = 1,
                readBack = readBack,
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

        /// <summary>
        /// Destroys this resource
        /// </summary>
        internal void Destroy()
        {
            if(Disposed)
            {
                return;
            }

            Disposed = true;

            if(handle.Valid)
            {
                bgfx.destroy_texture(handle);
            }
        }

        /// <summary>
        /// Sets this texture active in a shader
        /// </summary>
        /// <param name="stage">The texture stage</param>
        /// <param name="sampler">The sampler uniform</param>
        internal void SetActive(byte stage, bgfx.UniformHandle sampler)
        {
            bgfx.set_texture(stage, sampler, handle, uint.MaxValue);
        }

        /// <summary>
        /// Creates an empty texture
        /// </summary>
        /// <param name="width">The texture width</param>
        /// <param name="height">The texture height</param>
        /// <param name="hasMips">Whether to use mipmaps</param>
        /// <param name="layers">How many layers to use</param>
        /// <param name="format">The texture format</param>
        /// <param name="flags">Additional texture flags</param>
        /// <returns>The texture or null</returns>
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
                var readBack = flags.HasFlag(TextureFlags.ReadBack);

                return new Texture(handle, width, height, renderTarget, readBack);
            }
        }

        /// <summary>
        /// Creates a texture from a standard format (JPG/PNG/BMP/TGA/PSD)
        /// </summary>
        /// <param name="path">The file path of the texture</param>
        /// <param name="data">The file data in bytes</param>
        /// <param name="colorComponents">The color components we want for this image</param>
        /// <param name="flags">Additional texture flags</param>
        /// <returns>The texture, or null</returns>
        public static Texture CreateStandard(string path, byte[] data, StandardTextureColorComponents colorComponents, TextureFlags flags = TextureFlags.None)
        {
            try
            {
                var components = ColorComponents.Default;
                var format = bgfx.TextureFormat.RGBA8;

                switch(colorComponents)
                {
                    case StandardTextureColorComponents.RGB:

                        components = ColorComponents.RedGreenBlue;
                        format = bgfx.TextureFormat.RGB8;

                        break;

                    case StandardTextureColorComponents.RGBA:

                        components = ColorComponents.RedGreenBlueAlpha;
                        format = bgfx.TextureFormat.RGBA8;

                        break;

                    case StandardTextureColorComponents.Greyscale:

                        components = ColorComponents.Grey;
                        format = bgfx.TextureFormat.RGB8;

                        break;

                    case StandardTextureColorComponents.GreyscaleAlpha:

                        components = ColorComponents.GreyAlpha;
                        format = bgfx.TextureFormat.RGBA8;

                        break;
                }

                var imageData = ImageResult.FromMemory(data, components);

                data = imageData.Data;

                if(components == ColorComponents.Grey)
                {
                    var newData = new byte[imageData.Width * imageData.Height * 3];

                    for(int i = 0, index = 0; i < data.Length; i++, index += 3)
                    {
                        newData[index] = newData[index + 1] = newData[index + 2] = data[i];
                    }

                    data = newData;
                }
                else if(components == ColorComponents.GreyAlpha)
                {
                    var newData = new byte[imageData.Width * imageData.Height * 4];

                    for (int i = 0, index = 0; i < data.Length; i+=2, index += 4)
                    {
                        newData[index] = newData[index + 1] = newData[index + 2] = data[i];
                        newData[index + 3] = data[i + 1];
                    }

                    data = newData;
                }

                return CreatePixels(path, imageData.Data, (ushort)imageData.Width, (ushort)imageData.Height,
                    new TextureMetadata()
                    {
                        useMipmaps = false,
                    },
                    format, flags);
            }
            catch(System.Exception e)
            {
                Log.Error($"[Texture] Failed to load texture at {path}: {e}");

                return null;
            }
        }

        /// <summary>
        /// Creates a texture from pixels
        /// </summary>
        /// <param name="path">The file path of the texture</param>
        /// <param name="data">The pixel data</param>
        /// <param name="width">The texture width</param>
        /// <param name="height">The texture height</param>
        /// <param name="metadata">The texture metadata</param>
        /// <param name="format">The texture format</param>
        /// <param name="flags">Additional texture flags</param>
        /// <returns>The texture, or null</returns>
        public static Texture CreatePixels(string path, byte[] data, ushort width, ushort height, TextureMetadata metadata,
            bgfx.TextureFormat format, TextureFlags flags = TextureFlags.None)
        {
            unsafe
            {
                ProcessFlags(ref flags, metadata);

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

        /// <summary>
        /// Creates a texture from file data
        /// </summary>
        /// <param name="path">The texture path</param>
        /// <param name="data">The file data</param>
        /// <param name="metadata">The texture metadata</param>
        /// <param name="flags">Additional texture flags</param>
        /// <param name="skip">Which layers to skip</param>
        /// <returns>The texture or null</returns>
        internal static Texture Create(string path, byte[] data, TextureMetadata metadata, TextureFlags flags = TextureFlags.None, byte skip = 0)
        {
            unsafe
            {
                ProcessFlags(ref flags, metadata);

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

        /// <summary>
        /// Processes texture flags based on texture metadata
        /// </summary>
        /// <param name="flags">The texture flags</param>
        /// <param name="metadata">The metadata</param>
        internal static void ProcessFlags(ref TextureFlags flags, TextureMetadata metadata)
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

            if (metadata.readBack)
            {
                flags |= TextureFlags.ReadBack;
            }
        }

        //TODO
        /*
        public byte[] ReadPixels(byte mipLevel = 0)
        {
            unsafe
            {
                if (handle.Valid == false || metadata.readBack == false)
                {
                    return null;
                }

                //TODO: wait for the texture read to be ready

                var buffer = new byte[info.storageSize];

                fixed (void* ptr = buffer)
                {
                    bgfx.read_texture(handle, ptr, 0);
                }

                return buffer;
            }
        }
        */
    }
}
