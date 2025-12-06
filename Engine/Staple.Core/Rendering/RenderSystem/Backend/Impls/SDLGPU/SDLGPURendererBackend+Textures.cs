using SDL3;
using System;

namespace Staple.Internal;

internal partial class SDLGPURendererBackend
{
    internal void ReleaseTextureResource(TextureResource resource)
    {
        if ((resource?.used ?? false) == false)
        {
            return;
        }

        for(var i = readTextureQueue.Count - 1; i >= 0; i--)
        {
            if (TryGetTexture(readTextureQueue[i].Item1?.handle ?? default, out var r) &&
                r == resource)
            {
                readTextureQueue[i].Item2?.Invoke(null);

                readTextureQueue.RemoveAt(i);
            }
        }

        SDL.SDL_WaitForGPUIdle(device);

        if (resource.transferBuffer != nint.Zero)
        {
            SDL.SDL_ReleaseGPUTransferBuffer(device, resource.transferBuffer);

            resource.transferBuffer = nint.Zero;
        }

        if (resource.texture != nint.Zero)
        {
            SDL.SDL_ReleaseGPUTexture(device, resource.texture);

            resource.texture = nint.Zero;
        }

        resource.used = false;
    }

    internal static ResourceHandle<Texture> ReserveTextureResource(TextureResource[] resources, nint texture, int width, int height,
        TextureFormat format, TextureFlags flags)
    {
        for (var i = 0; i < resources.Length; i++)
        {
            if (resources[i]?.used ?? false)
            {
                continue;
            }

            if (resources[i] is null)
            {
                resources[i] = new();
            }

            var resource = resources[i];

            resource.used = true;
            resource.texture = texture;
            resource.width = width;
            resource.height = height;
            resource.format = format;
            resource.flags = flags;
            resource.length = 0;

            return new ResourceHandle<Texture>((ushort)i);
        }

        return ResourceHandle<Texture>.Invalid;
    }

    internal bool TryGetTexture(ResourceHandle<Texture> handle, out TextureResource resource)
    {
        if (handle.IsValid == false ||
            (textures[handle.handle]?.used ?? false) == false)
        {
            resource = default;

            return false;
        }

        resource = textures[handle.handle];

        return true;
    }

    internal nint GetSampler(TextureFlags flags)
    {
        if (textureSamplers.TryGetValue(flags, out var sampler) == false)
        {
            SDL.SDL_GPUSamplerAddressMode GetAddressModeU()
            {
                if (flags.HasFlag(TextureFlags.RepeatU))
                {
                    return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT;
                }

                if (flags.HasFlag(TextureFlags.MirrorU))
                {
                    return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT;
                }

                if (flags.HasFlag(TextureFlags.ClampU))
                {
                    return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
                }

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            }

            SDL.SDL_GPUSamplerAddressMode GetAddressModeV()
            {
                if (flags.HasFlag(TextureFlags.RepeatV))
                {
                    return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT;
                }

                if (flags.HasFlag(TextureFlags.MirrorV))
                {
                    return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT;
                }

                if (flags.HasFlag(TextureFlags.ClampV))
                {
                    return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
                }

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            }

            SDL.SDL_GPUSamplerAddressMode GetAddressModeW()
            {
                if (flags.HasFlag(TextureFlags.RepeatW))
                {
                    return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT;
                }

                if (flags.HasFlag(TextureFlags.MirrorW))
                {
                    return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT;
                }

                if (flags.HasFlag(TextureFlags.ClampW))
                {
                    return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
                }

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            }

            var anisotropy = false;

            if (flags.HasFlag(TextureFlags.AnisotropicFilter))
            {
                anisotropy = true;
            }

            var uMode = GetAddressModeU();
            var vMode = GetAddressModeV();
            var wMode = GetAddressModeW();

            var magFilter = flags.HasFlag(TextureFlags.LinearFilter) ? SDL.SDL_GPUFilter.SDL_GPU_FILTER_LINEAR :
                SDL.SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;

            var mipmapMode = flags.HasFlag(TextureFlags.LinearFilter) ? SDL.SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR :
                SDL.SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST;

            var info = new SDL.SDL_GPUSamplerCreateInfo()
            {
                address_mode_u = uMode,
                address_mode_v = vMode,
                address_mode_w = wMode,
                enable_anisotropy = anisotropy,
                mag_filter = magFilter,
                min_filter = magFilter,
                mipmap_mode = mipmapMode,
                max_anisotropy = 16,
            };

            sampler = SDL.SDL_CreateGPUSampler(device, in info);

            if (sampler != nint.Zero)
            {
                textureSamplers.Add(flags, sampler);
            }
        }

        return sampler;
    }

    internal void DestroyTexture(ResourceHandle<Texture> handle)
    {
        AddCommand(new SDLGPUDestroyTextureCommand(handle));
    }

    public ITexture CreateTextureAssetTexture(SerializableTexture asset, TextureFlags flags)
    {
        var format = asset.metadata.Format;

        if (TryGetTextureFormat(format, flags, out var textureFormat) == false)
        {
            return null;
        }

        var info = new SDL.SDL_GPUTextureCreateInfo()
        {
            format = textureFormat,
            width = (uint)asset.width,
            height = (uint)asset.height,
            type = GetTextureType(flags),
            usage = GetTextureUsage(flags),
            layer_count_or_depth = 1,
            num_levels = 1,
        };

        var texture = SDL.SDL_CreateGPUTexture(device, in info);

        if (texture == nint.Zero)
        {
            return null;
        }

        var handle = ReserveTextureResource(textures, texture, asset.width, asset.height, format, flags);

        if (handle.IsValid == false)
        {
            SDL.SDL_ReleaseGPUTexture(device, texture);

            return null;
        }

        var outValue = new SDLGPUTexture(handle, asset.width, asset.height, format, flags, this);

        outValue.Update(asset.data);

        return outValue;
    }

    public ITexture CreatePixelTexture(byte[] data, int width, int height, TextureFormat format, TextureFlags flags)
    {
        if (TryGetTextureFormat(format, flags, out var textureFormat) == false)
        {
            return null;
        }

        var info = new SDL.SDL_GPUTextureCreateInfo()
        {
            format = textureFormat,
            width = (uint)width,
            height = (uint)height,
            type = GetTextureType(flags),
            usage = GetTextureUsage(flags),
            layer_count_or_depth = 1,
            num_levels = 1, //TODO: Support multiple levels
        };

        var texture = SDL.SDL_CreateGPUTexture(device, in info);

        if (texture == nint.Zero)
        {
            return null;
        }

        var handle = ReserveTextureResource(textures, texture, width, height, format, flags);

        if(handle.IsValid == false)
        {
            SDL.SDL_ReleaseGPUTexture(device, texture);

            return null;
        }

        var outValue = new SDLGPUTexture(handle, width, height, format, flags, this);

        outValue.Update(data);

        return outValue;
    }

    public ITexture CreateEmptyTexture(int width, int height, TextureFormat format, TextureFlags flags)
    {
        if (TryGetTextureFormat(format, flags, out var textureFormat) == false)
        {
            return null;
        }

        var info = new SDL.SDL_GPUTextureCreateInfo()
        {
            format = textureFormat,
            width = (uint)width,
            height = (uint)height,
            type = GetTextureType(flags),
            usage = GetTextureUsage(flags),
            layer_count_or_depth = 1,
            num_levels = 1,
        };

        var texture = SDL.SDL_CreateGPUTexture(device, in info);

        if (texture == nint.Zero)
        {
            return null;
        }

        var handle = ReserveTextureResource(textures, texture, width, height, format, flags);

        if (handle.IsValid == false || TryGetTexture(handle, out var resource) == false)
        {
            SDL.SDL_ReleaseGPUTexture(device, texture);

            return null;
        }

        resource.length = width * height * format switch
        {
            TextureFormat.A8 => sizeof(byte),
            TextureFormat.R8 => sizeof(byte),
            TextureFormat.R8I => sizeof(byte),
            TextureFormat.R8U => sizeof(byte),
            TextureFormat.R8S => sizeof(byte),
            TextureFormat.R16 => sizeof(ushort),
            TextureFormat.R16I => sizeof(ushort),
            TextureFormat.R16U => sizeof(ushort),
            TextureFormat.R16F => sizeof(ushort),
            TextureFormat.R16S => sizeof(ushort),
            TextureFormat.R32I => sizeof(uint),
            TextureFormat.R32U => sizeof(uint),
            TextureFormat.R32F => sizeof(uint),
            TextureFormat.RG8 => sizeof(ushort),
            TextureFormat.RG8I => sizeof(ushort),
            TextureFormat.RG8U => sizeof(ushort),
            TextureFormat.RG8S => sizeof(ushort),
            TextureFormat.RG16 => sizeof(uint),
            TextureFormat.RG16I => sizeof(uint),
            TextureFormat.RG16U => sizeof(uint),
            TextureFormat.RG16F => sizeof(uint),
            TextureFormat.RG16S => sizeof(uint),
            TextureFormat.RG32I => sizeof(ulong),
            TextureFormat.RG32U => sizeof(ulong),
            TextureFormat.RG32F => sizeof(ulong),
            TextureFormat.BGRA8 => sizeof(uint),
            TextureFormat.RGBA8 => sizeof(uint),
            TextureFormat.RGBA8I => sizeof(uint),
            TextureFormat.RGBA8U => sizeof(uint),
            TextureFormat.RGBA8S => sizeof(uint),
            TextureFormat.RGBA16 => sizeof(ulong),
            TextureFormat.RGBA16I => sizeof(ulong),
            TextureFormat.RGBA16U => sizeof(ulong),
            TextureFormat.RGBA16F => sizeof(ulong),
            TextureFormat.RGBA16S => sizeof(ulong),
            TextureFormat.RGBA32I => sizeof(uint) * 4,
            TextureFormat.RGBA32U => sizeof(uint) * 4,
            TextureFormat.RGBA32F => sizeof(uint) * 4,
            TextureFormat.B5G6R5 => sizeof(ushort),
            TextureFormat.BGRA4 => sizeof(ushort),
            TextureFormat.BGR5A1 => sizeof(ushort),
            TextureFormat.RGB10A2 => sizeof(uint),
            TextureFormat.RG11B10F => sizeof(uint),
            TextureFormat.D16 => sizeof(ushort),
            TextureFormat.D24 => sizeof(uint),
            TextureFormat.D24S8 => sizeof(uint),
            TextureFormat.D32S8 => sizeof(uint) + sizeof(byte),
            TextureFormat.D32F => sizeof(uint),
            _ => 0,
        };

        return new SDLGPUTexture(handle, width, height, format, flags, this);
    }

    public void UpdateTexture(ResourceHandle<Texture> handle, Span<byte> data)
    {
        AddCommand(new SDLGPUUpdateTextureCommand(handle, data.ToArray()));
    }

    public void ReadTexture(ITexture texture, Action<byte[]> onComplete)
    {
        if(texture is not SDLGPUTexture t ||
            t.Disposed ||
            onComplete == null)
        {
            onComplete?.Invoke(null);

            return;
        }

        AddCommand(new SDLGPUReadTextureCommand(t, onComplete));
    }

    internal void QueueTextureRead(SDLGPUTexture texture, Action<byte[]> onComplete)
    {
        if(texture == null ||
            texture.Disposed ||
            onComplete == null)
        {
            onComplete?.Invoke(null);

            return;
        }

        readTextureQueue.Add((texture, onComplete));
    }

    public static SDL.SDL_GPUTextureType GetTextureType(TextureFlags flags)
    {
        if (flags.HasFlag(TextureFlags.TextureTypeCube))
        {
            return SDL.SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_CUBE;
        }

        if (flags.HasFlag(TextureFlags.TextureTypeCubeArray))
        {
            return SDL.SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_CUBE_ARRAY;
        }

        if (flags.HasFlag(TextureFlags.TextureType2DArray))
        {
            return SDL.SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D_ARRAY;
        }

        if (flags.HasFlag(TextureFlags.TextureType3D))
        {
            return SDL.SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_3D;
        }

        return SDL.SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D;
    }

    public static SDL.SDL_GPUTextureUsageFlags GetTextureUsage(TextureFlags flags)
    {
        SDL.SDL_GPUTextureUsageFlags HandleFlags(SDL.SDL_GPUTextureUsageFlags f)
        {
            if (flags.HasFlag(TextureFlags.ComputeRead))
            {
                f |= SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ;
            }

            if (flags.HasFlag(TextureFlags.ComputeWrite))
            {
                f |= SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE;
            }

            if (flags.HasFlag(TextureFlags.Readback))
            {
                f |= SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ;
            }

            return f;
        }

        if (flags.HasFlag(TextureFlags.ColorTarget))
        {
            return HandleFlags(SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET);
        }
        else if (flags.HasFlag(TextureFlags.DepthStencilTarget))
        {
            return HandleFlags(SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET);
        }

        return HandleFlags(SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER);
    }

    public static bool TryGetTextureFormat(TextureFormat format, TextureFlags flags, out SDL.SDL_GPUTextureFormat outValue)
    {
        var hasSRGB = flags.HasFlag(TextureFlags.SRGB);

        switch (format)
        {
            case TextureFormat.BC1:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM;

                return true;

            case TextureFormat.BC2:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM;

                return true;

            case TextureFormat.BC3:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM;

                return true;

            case TextureFormat.BC4:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC4_R_UNORM;

                return true;

            case TextureFormat.BC5:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC5_RG_UNORM;

                return true;

            case TextureFormat.BC6H:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC6H_RGB_FLOAT;

                return true;

            case TextureFormat.BC7:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM;

                return true;

            case TextureFormat.ASTC4x4:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_4x4_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_4x4_UNORM;

                return true;

            case TextureFormat.ASTC5x4:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x4_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x4_UNORM;

                return true;

            case TextureFormat.ASTC5x5:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x5_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x5_UNORM;

                return true;

            case TextureFormat.ASTC6x5:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x5_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x5_UNORM;

                return true;

            case TextureFormat.ASTC6x6:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x6_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x6_UNORM;

                return true;

            case TextureFormat.ASTC8x5:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x5_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x5_UNORM;

                return true;

            case TextureFormat.ASTC8x6:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x6_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x6_UNORM;

                return true;

            case TextureFormat.ASTC8x8:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x8_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x8_UNORM;

                return true;

            case TextureFormat.ASTC10x5:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x5_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x5_UNORM;

                return true;

            case TextureFormat.ASTC10x6:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x6_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x6_UNORM;

                return true;

            case TextureFormat.ASTC10x8:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x8_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x8_UNORM;

                return true;

            case TextureFormat.ASTC10x10:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x10_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x10_UNORM;

                return true;

            case TextureFormat.ASTC12x10:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x10_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x10_UNORM;

                return true;

            case TextureFormat.ASTC12x12:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x12_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x12_UNORM;

                return true;

            case TextureFormat.ASTC4x4F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_4x4_FLOAT;

                return true;

            case TextureFormat.ASTC5x4F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x4_FLOAT;

                return true;

            case TextureFormat.ASTC5x5F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x5_FLOAT;

                return true;

            case TextureFormat.ASTC6x5F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x5_FLOAT;

                return true;

            case TextureFormat.ASTC6x6F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x6_FLOAT;

                return true;

            case TextureFormat.ASTC8x5F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x5_FLOAT;

                return true;

            case TextureFormat.ASTC8x6F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x6_FLOAT;

                return true;

            case TextureFormat.ASTC8x8F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x8_FLOAT;

                return true;

            case TextureFormat.ASTC10x5F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x5_FLOAT;

                return true;

            case TextureFormat.ASTC10x6F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x6_FLOAT;

                return true;

            case TextureFormat.ASTC10x8F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x8_FLOAT;

                return true;

            case TextureFormat.ASTC10x10F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x10_FLOAT;

                return true;

            case TextureFormat.ASTC12x10F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x10_FLOAT;

                return true;

            case TextureFormat.ASTC12x12F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x12_FLOAT;

                return true;

            case TextureFormat.A8:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_A8_UNORM;

                return true;

            case TextureFormat.R8:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM;

                return true;

            case TextureFormat.R8I:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_INT;

                return true;

            case TextureFormat.R8U:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UINT;

                return true;

            case TextureFormat.R8S:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_SNORM;

                return true;

            case TextureFormat.R16:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_UNORM;

                return true;

            case TextureFormat.R16I:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_INT;

                return true;

            case TextureFormat.R16U:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_UINT;

                return true;

            case TextureFormat.R16F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_FLOAT;

                return true;

            case TextureFormat.R16S:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_SNORM;

                return true;

            case TextureFormat.R32I:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_INT;

                return true;

            case TextureFormat.R32U:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_UINT;

                return true;

            case TextureFormat.R32F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_FLOAT;

                return true;

            case TextureFormat.RG8:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UNORM;

                return true;

            case TextureFormat.RG8I:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_INT;

                return true;

            case TextureFormat.RG8U:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UINT;

                return true;

            case TextureFormat.RG8S:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_SNORM;

                return true;

            case TextureFormat.RG16:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_UNORM;

                return true;

            case TextureFormat.RG16I:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_INT;

                return true;

            case TextureFormat.RG16U:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_UINT;

                return true;

            case TextureFormat.RG16F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_FLOAT;

                return true;

            case TextureFormat.RG16S:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_SNORM;

                return true;

            case TextureFormat.RG32I:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_INT;

                return true;

            case TextureFormat.RG32U:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_UINT;

                return true;

            case TextureFormat.RG32F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_FLOAT;

                return true;

            case TextureFormat.BGRA8:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM;

                return true;

            case TextureFormat.RGBA8:

                outValue = hasSRGB ? SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB :
                    SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;

                return true;

            case TextureFormat.RGBA8I:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_INT;

                return true;

            case TextureFormat.RGBA8U:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UINT;

                return true;

            case TextureFormat.RGBA8S:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_SNORM;

                return true;

            case TextureFormat.RGBA16:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UNORM;

                return true;

            case TextureFormat.RGBA16I:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_INT;

                return true;

            case TextureFormat.RGBA16U:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UINT;

                return true;

            case TextureFormat.RGBA16F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_FLOAT;

                return true;

            case TextureFormat.RGBA16S:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_SNORM;

                return true;

            case TextureFormat.RGBA32I:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_INT;

                return true;

            case TextureFormat.RGBA32U:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_UINT;

                return true;

            case TextureFormat.RGBA32F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_FLOAT;

                return true;

            case TextureFormat.B5G6R5:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B5G6R5_UNORM;

                return true;

            case TextureFormat.BGRA4:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B4G4R4A4_UNORM;

                return true;

            case TextureFormat.BGR5A1:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B5G5R5A1_UNORM;

                return true;

            case TextureFormat.RGB10A2:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R10G10B10A2_UNORM;

                return true;

            case TextureFormat.RG11B10F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R11G11B10_UFLOAT;

                return true;

            case TextureFormat.D16:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D16_UNORM;

                return true;

            case TextureFormat.D24:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM;

                return true;

            case TextureFormat.D24S8:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT;

                return true;

            case TextureFormat.D32S8:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT;

                return true;

            case TextureFormat.D32F:

                outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT;

                return true;
        }

        outValue = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID;

        return false;
    }
}
