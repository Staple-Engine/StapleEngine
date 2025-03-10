using Bgfx;
using Staple.Internal;
using StbImageSharp;
using StbRectPackSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Staple;

/// <summary>
/// Texture resource
/// </summary>
public class Texture : IGuidAsset
{
    internal bgfx.TextureHandle handle;
    internal bgfx.TextureInfo info;
    internal TextureMetadata metadata;
    internal bool renderTarget = false;

    internal RawTextureData readbackData;

    private readonly ITextureCreateMethod createMethod;

    private readonly GuidHasher guidHasher = new();

    public GuidHasher Guid => guidHasher;

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
    /// The texture's sprite scale
    /// </summary>
    public float SpriteScale => 1.0f / (float)metadata.spritePixelsPerUnit;

    /// <summary>
    /// The contained sprites of this texture
    /// </summary>
    public Sprite[] Sprites { get; internal set; } = [];

    /// <summary>
    /// Create a texture from an existing bgfx texture
    /// </summary>
    /// <param name="textureHandle">The handle</param>
    /// <param name="width">The texture's width</param>
    /// <param name="height">The texture's height</param>
    /// <param name="readBack">Whether it can be read back</param>
    internal Texture(bgfx.TextureHandle textureHandle, ushort width, ushort height, bool readBack)
    {
        handle = textureHandle;
        renderTarget = true;

        metadata = new TextureMetadata()
        {
            readBack = readBack,
        };

        info = new bgfx.TextureInfo()
        {
            width = width,
            height = height,
        };
    }

    internal Texture(ITextureCreateMethod createMethod)
    {
        this.createMethod = createMethod;
    }

    ~Texture()
    {
        Destroy();
    }

    internal bool Create()
    {
        if(renderTarget && Disposed)
        {
            return false;
        }

        if(renderTarget || createMethod.Create(this))
        {
            Disposed = false;
        }

        if((metadata?.sprites?.Count ?? 0) > 0)
        {
            var sprites = Sprites;

            if ((Sprites?.Length ?? 0) != metadata.sprites.Count)
            {
                sprites = new Sprite[metadata.sprites.Count];
            }

            for (var i = 0; i < metadata.sprites.Count; i++)
            {
                sprites[i] ??= new();

                sprites[i].texture = this;
                sprites[i].spriteIndex = i;
            }

            Sprites = sprites;
        }

        return true;
    }

    /// <summary>
    /// Destroys this resource
    /// </summary>
    public void Destroy()
    {
        if (Disposed)
        {
            return;
        }

        Disposed = true;

        if (handle.Valid)
        {
            bgfx.destroy_texture(handle);

            handle.idx = ushort.MaxValue;
        }
    }

    /// <summary>
    /// IPathAsset implementation. Loads a texture from path.
    /// </summary>
    /// <param name="path">The path to load from</param>
    /// <returns>The texture, or null</returns>
    public static object Create(string path) => ResourceManager.instance.LoadTexture(path);

    /// <summary>
    /// Sets this texture active in a shader
    /// </summary>
    /// <param name="stage">The texture stage</param>
    /// <param name="sampler">The sampler uniform</param>
    internal void SetActive(byte stage, bgfx.UniformHandle sampler, TextureFlags flags = (TextureFlags)uint.MaxValue)
    {
        //CPU-Only
        if(metadata.readBack)
        {
            return;
        }

        bgfx.set_texture(stage, sampler, handle, (uint)flags);
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
    public static Texture CreateEmpty(ushort width, ushort height, bool hasMips, ushort layers, TextureFormat format, TextureFlags flags = TextureFlags.None)
    {
        var texture = new Texture(new EmptyTextureCreateMethod(width, height, hasMips, layers, format, flags));

        if (texture.Create())
        {
            ResourceManager.instance.userCreatedTextures.Add(new WeakReference<Texture>(texture));

            return texture;
        }

        return null;
    }

    /// <summary>
    /// Loads the raw texture data from a standard format image data
    /// </summary>
    /// <param name="data">The data of the raw image file, in bytes</param>
    /// <param name="colorComponents">The color components we want</param>
    /// <returns>The raw texture data with the pixel data, width, height, and color components</returns>
    /// <remarks>The data passed here should be of standard raw image files such as png or jpg</remarks>
    public static RawTextureData LoadStandard(byte[] data, StandardTextureColorComponents colorComponents)
    {
        try
        {
            var components = ColorComponents.Default;

            switch (colorComponents)
            {
                case StandardTextureColorComponents.RGB:

                    components = ColorComponents.RedGreenBlue;

                    break;

                case StandardTextureColorComponents.RGBA:

                    components = ColorComponents.RedGreenBlueAlpha;

                    break;

                case StandardTextureColorComponents.Greyscale:

                    components = ColorComponents.Grey;

                    break;

                case StandardTextureColorComponents.GreyscaleAlpha:

                    components = ColorComponents.GreyAlpha;

                    break;
            }

            var imageData = ImageResult.FromMemory(data, components);

            data = imageData.Data;

            if (components == ColorComponents.Grey)
            {
                var newData = new byte[imageData.Width * imageData.Height * 3];

                for (int i = 0, index = 0; i < data.Length; i++, index += 3)
                {
                    newData[index] = newData[index + 1] = newData[index + 2] = data[i];
                }

                data = newData;
            }
            else if (components == ColorComponents.GreyAlpha)
            {
                var newData = new byte[imageData.Width * imageData.Height * 4];

                for (int i = 0, index = 0; i < data.Length; i += 2, index += 4)
                {
                    newData[index] = newData[index + 1] = newData[index + 2] = data[i];
                    newData[index + 3] = data[i + 1];
                }

                data = newData;
            }

            return new RawTextureData()
            {
                colorComponents = colorComponents,
                width = imageData.Width,
                height = imageData.Height,
                data = data,
            };
        }
        catch (System.Exception e)
        {
            return null;
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
            var rawData = LoadStandard(data, colorComponents);

            if(rawData == null)
            {
                Log.Error($"[Texture] Failed to load texture at {path}: Failed to load image data");

                return null;
            }

            var format = TextureFormat.RGBA8;

            switch (colorComponents)
            {
                case StandardTextureColorComponents.RGB:
                case StandardTextureColorComponents.Greyscale:

                    format = TextureFormat.RGB8;

                    break;

                case StandardTextureColorComponents.RGBA:
                case StandardTextureColorComponents.GreyscaleAlpha:

                    format = TextureFormat.RGBA8;

                    break;
            }

            return CreatePixels(path, rawData.data, (ushort)rawData.width, (ushort)rawData.height,
                new TextureMetadata()
                {
                    useMipmaps = false,
                },
                format, flags);
        }
        catch (System.Exception e)
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
        TextureFormat format, TextureFlags flags = TextureFlags.None)
    {
        var texture = new Texture(new PixelTextureCreateMethod(path, data, width, height, metadata, format, flags));

        if(texture.Create())
        {
            ResourceManager.instance.userCreatedTextures.Add(new WeakReference<Texture>(texture));

            return texture;
        }

        return null;
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
        var texture = new Texture(new BGFXTextureCreateMethod(path, data, metadata, flags, skip));

        return texture.Create() ? texture : null;
    }

    /// <summary>
    /// Processes texture flags based on texture metadata
    /// </summary>
    /// <param name="flags">The texture flags</param>
    /// <param name="metadata">The metadata</param>
    /// <param name="ignoreWrap">Whether to ignore wrapping mode</param>
    internal static void ProcessFlags(ref TextureFlags flags, TextureMetadata metadata, bool ignoreWrap = false)
    {
        switch (metadata.type)
        {
            case TextureType.Texture:
            case TextureType.Sprite:

                if(metadata.isLinear == false)
                {
                    flags |= TextureFlags.SRGB;
                }

                break;
        }

        if(ignoreWrap == false)
        {
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

    /// <summary>
    /// Packs multiple textures into a single atlas one
    /// </summary>
    /// <param name="textureData">The data of each texture</param>
    /// <param name="width">The width of the atlas</param>
    /// <param name="height">The height of the atlas</param>
    /// <param name="maxSize">The maximum size on both width and height</param>
    /// <param name="padding">Amount of transparent pixels between textures</param>
    /// <param name="outRects">The rectangles representing the areas of each texture in the atlas</param>
    /// <param name="outTextureData">The texture data with the full atlas</param>
    /// <param name="expandWidth">Whether to expand width (will be toggled with each attempt to pack, used internally)</param>
    /// <param name="expandHeight">Whether to expand height (will be toggled with each attempt to pack, used internally)</param>
    /// <returns>Whether the textures were packed</returns>
    public static bool PackTextures(RawTextureData[] textureData, int width, int height, int maxSize, int padding,
        out Rect[] outRects, out RawTextureData outTextureData, bool expandWidth = true, bool expandHeight = false)
    {
        outRects = default;
        outTextureData = default;

        if(textureData.Any(x => x == null || x.colorComponents != StandardTextureColorComponents.RGBA))
        {
            return false;
        }

        if(width > maxSize || height > maxSize)
        {
            return false;
        }

        var pack = new Packer(width, height);

        var rects = new PackerRectangle[textureData.Length];

        var doublePadding = padding * 2;

        for(var i = 0; i < rects.Length; i++)
        {
            var texture = textureData[i];

            var rect = pack.PackRect(texture.width + doublePadding, texture.height + doublePadding, null);

            if(rect == null)
            {
                pack.Dispose();

                return PackTextures(textureData, width * (expandWidth ? 2 : 1), height * (expandHeight ? 2 : 1), maxSize, padding, out outRects, out outTextureData,
                    !expandWidth, !expandHeight);
            }

            rects[i] = rect;
        }

        outTextureData = new()
        {
            width = width,
            height = height,
            colorComponents = StandardTextureColorComponents.RGBA,
            data = new byte[width * height * 4],
        };

        outRects = new Rect[rects.Length];

        for(var i = 0; i < rects.Length; i++)
        {
            var texture = textureData[i];
            var rect = rects[i];

            var outRect = new Rect(new Vector2Int(rect.X, rect.Y), new Vector2Int(rect.Width, rect.Height));

            outRect.left += padding;
            outRect.top += padding;
            outRect.Width -= padding;
            outRect.Height -= padding;

            outRects[i] = outRect;

            outTextureData.Blit(0, 0, outRect.Width, outRect.Height, texture.width * 4, texture.data, outRect.left, outRect.top);
        }

        return true;
    }

    /// <summary>
    /// Reads back the pixels for a texture. Requires readback on the texture metadata
    /// </summary>
    /// <param name="completion">The completion block when the data is ready. This is an async operation.</param>
    /// <param name="mipLevel">The mip level to read</param>
    /// <remarks>Render target textures need to use <see cref="RenderTarget.ReadTexture"/> instead</remarks>
    public void ReadPixels(Action<Texture, byte[]> completion, byte mipLevel = 0)
    {
        unsafe
        {
            if (handle.Valid == false ||
                metadata.readBack == false ||
                renderTarget ||
                info.storageSize == 0)
            {
                completion?.Invoke(this, null);

                return;
            }

            var buffer = new byte[info.storageSize];

            fixed (void* ptr = buffer)
            {
                var targetFrame = bgfx.read_texture(handle, ptr, 0);

                RenderSystem.Instance.QueueFrameCallback(targetFrame, () =>
                {
                    completion?.Invoke(this, buffer);
                });
            }
        }
    }

    /// <summary>
    /// Gets the pixels in this texture as a Color array, if any
    /// </summary>
    /// <returns>The pixels</returns>
    public Color[] GetPixels()
    {
        var c = readbackData?.ToColorArray() ?? [];

        var outValue = new Color[c.Length];

        for(var i = 0; i < outValue.Length; i++)
        {
            outValue[i] = c[i];
        }

        return outValue;
    }

    /// <summary>
    /// Gets the pixels in this texture as a Color32 array, if any
    /// </summary>
    /// <returns>The pixels</returns>
    public Color32[] GetPixels32()
    {
        return readbackData?.ToColorArray() ?? [];
    }

    /// <summary>
    /// Gets the raw texture data of this texture (if available)
    /// </summary>
    /// <remarks>Texture needs to have the <see cref="TextureMetadata.keepOnCPU"/> flag enabled</remarks>
    /// <returns>The raw texture data, or null</returns>
    public RawTextureData GetRawTextureData()
    {
        if(readbackData == null)
        {
            Log.Error($"Texture {Guid} isn't readable (missing readback flag)");

            return null;
        }

        return new()
        {
            colorComponents = readbackData.colorComponents,
            data = readbackData.data,
            height = readbackData.height,
            width = readbackData.width,
        };
    }
}
