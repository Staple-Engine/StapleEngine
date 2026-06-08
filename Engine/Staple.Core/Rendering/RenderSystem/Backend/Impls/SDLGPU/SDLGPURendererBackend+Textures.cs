using SDL;
using System;

namespace Staple.Internal;

internal partial class SDLGPURendererBackend
{
    internal void ReleaseTextureResource(TextureResource resource)
    {
        if (!(resource?.used ?? false))
        {
            return;
        }

        for(var i = readTextureQueue.Count - 1; i >= 0; i--)
        {
            if (!TryGetTexture(readTextureQueue[i].Item1?.handle ?? default, out var r) ||
                r != resource)
            {
                continue;
            }
            
            readTextureQueue[i].Item2?.Invoke(null);

            readTextureQueue.RemoveAt(i);
        }

        unsafe
        {
            resource.transferBuffer = null;

            if (resource.texture != null)
            {
                SDL3.SDL_ReleaseGPUTexture(device, resource.texture);

                resource.texture = null;
            }
        }

        resource.used = false;
    }

    internal static unsafe ResourceHandle<Texture> ReserveTextureResource(TextureResource[] resources, SDL_GPUTexture *texture,
        int width, int height, TextureFormat format, TextureFlags flags)
    {
        for (var i = 0; i < resources.Length; i++)
        {
            if (resources[i]?.used ?? false)
            {
                continue;
            }

            resources[i] ??= new();

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
        if (!handle.IsValid ||
            !(textures[handle.handle]?.used ?? false))
        {
            resource = null;

            return false;
        }

        resource = textures[handle.handle];

        return true;
    }

    internal unsafe SDL_GPUSampler *GetSampler(TextureFlags flags)
    {
        if (textureSamplers.TryGetValue(flags, out var sampler))
        {
            return sampler.ptr;
        }

        var anisotropy = flags.HasFlag(TextureFlags.AnisotropicFilter);

        var uMode = flags switch
        {
            _ when flags.HasFlag(TextureFlags.RepeatU) => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
            _ when flags.HasFlag(TextureFlags.MirrorU) => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT,
            _ => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        };

        var vMode = flags switch
        {
            _ when flags.HasFlag(TextureFlags.RepeatV) => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
            _ when flags.HasFlag(TextureFlags.MirrorV) => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT,
            _ => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        };

        var wMode = flags switch
        {
            _ when flags.HasFlag(TextureFlags.RepeatW) => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
            _ when flags.HasFlag(TextureFlags.MirrorW) => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT,
            _ => SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        };

        var magFilter = flags.HasFlag(TextureFlags.LinearFilter) ? SDL_GPUFilter.SDL_GPU_FILTER_LINEAR :
            SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;

        var mipmapMode = flags.HasFlag(TextureFlags.LinearFilter) ? SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR :
            SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST;

        var info = new SDL_GPUSamplerCreateInfo()
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

        sampler = new(SDL3.SDL_CreateGPUSampler(device, &info));

        if (sampler.ptr != null)
        {
            textureSamplers.Add(flags, sampler);
        }

        return sampler.ptr;
    }

    internal void DestroyTexture(ResourceHandle<Texture> handle)
    {
        AddCommand(new SDLGPUDestroyTextureCommand(this, handle));
    }

    public ITexture CreateTextureAssetTexture(SerializableTexture asset, TextureFlags flags)
    {
        unsafe
        {
            var format = asset.metadata.Format;

            if (!TryGetTextureFormat(format, flags, out var textureFormat))
            {
                return null;
            }

            var info = new SDL_GPUTextureCreateInfo()
            {
                format = textureFormat,
                width = (uint)asset.width,
                height = (uint)asset.height,
                type = GetTextureType(flags),
                usage = GetTextureUsage(flags),
                layer_count_or_depth = 1,
                num_levels = 1,
            };

            var texture = SDL3.SDL_CreateGPUTexture(device, &info);

            if (texture == null)
            {
                return null;
            }

            var handle = ReserveTextureResource(textures, texture, asset.width, asset.height, format, flags);

            if (!handle.IsValid)
            {
                SDL3.SDL_ReleaseGPUTexture(device, texture);

                return null;
            }

            var outValue = new SDLGPUTexture(handle, asset.width, asset.height, format, flags, this);

            outValue.Update(asset.data);

            return outValue;
        }
    }

    public ITexture CreatePixelTexture(byte[] data, int width, int height, TextureFormat format, TextureFlags flags)
    {
        unsafe
        {
            if (!TryGetTextureFormat(format, flags, out var textureFormat))
            {
                return null;
            }

            var info = new SDL_GPUTextureCreateInfo()
            {
                format = textureFormat,
                width = (uint)width,
                height = (uint)height,
                type = GetTextureType(flags),
                usage = GetTextureUsage(flags),
                layer_count_or_depth = 1,
                num_levels = 1, //TODO: Support multiple levels
            };

            var texture = SDL3.SDL_CreateGPUTexture(device, &info);

            if (texture == null)
            {
                return null;
            }

            var handle = ReserveTextureResource(textures, texture, width, height, format, flags);

            if (!handle.IsValid)
            {
                SDL3.SDL_ReleaseGPUTexture(device, texture);

                return null;
            }

            var outValue = new SDLGPUTexture(handle, width, height, format, flags, this);

            outValue.Update(data);

            return outValue;
        }
    }

    public ITexture CreateEmptyTexture(int width, int height, TextureFormat format, TextureFlags flags)
    {
        unsafe
        {
            if (!TryGetTextureFormat(format, flags, out var textureFormat))
            {
                return null;
            }

            var info = new SDL_GPUTextureCreateInfo()
            {
                format = textureFormat,
                width = (uint)width,
                height = (uint)height,
                type = GetTextureType(flags),
                usage = GetTextureUsage(flags),
                layer_count_or_depth = 1,
                num_levels = 1,
            };

            var texture = SDL3.SDL_CreateGPUTexture(device, &info);

            if (texture == null)
            {
                return null;
            }

            var handle = ReserveTextureResource(textures, texture, width, height, format, flags);

            if (!handle.IsValid || !TryGetTexture(handle, out var resource))
            {
                SDL3.SDL_ReleaseGPUTexture(device, texture);

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
    }

    public void UpdateTexture(ResourceHandle<Texture> handle, Span<byte> data)
    {
        AddCommand(new SDLGPUUpdateTextureCommand(this, handle, data.ToArray()));
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

        AddCommand(new SDLGPUReadTextureCommand(this, t, onComplete));
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

    public static SDL_GPUTextureType GetTextureType(TextureFlags flags)
    {
        if (flags.HasFlag(TextureFlags.TextureTypeCube))
        {
            return SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_CUBE;
        }

        if (flags.HasFlag(TextureFlags.TextureTypeCubeArray))
        {
            return SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_CUBE_ARRAY;
        }

        if (flags.HasFlag(TextureFlags.TextureType2DArray))
        {
            return SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D_ARRAY;
        }

        if (flags.HasFlag(TextureFlags.TextureType3D))
        {
            return SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_3D;
        }

        return SDL_GPUTextureType.SDL_GPU_TEXTURETYPE_2D;
    }

    public static SDL_GPUTextureUsageFlags GetTextureUsage(TextureFlags flags)
    {
        var usageFlags = SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER;

        if (flags.HasFlag(TextureFlags.ComputeRead))
        {
            usageFlags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ;
        }

        if (flags.HasFlag(TextureFlags.ComputeWrite))
        {
            usageFlags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE;
        }

        if (flags.HasFlag(TextureFlags.Readback))
        {
            usageFlags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ;
        }

        if (flags.HasFlag(TextureFlags.ColorTarget))
        {
            usageFlags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET;
        }
        else if (flags.HasFlag(TextureFlags.DepthStencilTarget))
        {
            usageFlags |= SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET;
        }

        return usageFlags;
    }

    public static bool TryGetTextureFormat(TextureFormat format, TextureFlags flags, out SDL_GPUTextureFormat outValue)
    {
        var hasSRGB = flags.HasFlag(TextureFlags.SRGB);

        switch (format)
        {
            case TextureFormat.BC1:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM;

                return true;

            case TextureFormat.BC2:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM;

                return true;

            case TextureFormat.BC3:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM;

                return true;

            case TextureFormat.BC4:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC4_R_UNORM;

                return true;

            case TextureFormat.BC5:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC5_RG_UNORM;

                return true;

            case TextureFormat.BC6H:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC6H_RGB_FLOAT;

                return true;

            case TextureFormat.BC7:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM;

                return true;

            case TextureFormat.ASTC4x4:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_4x4_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_4x4_UNORM;

                return true;

            case TextureFormat.ASTC5x4:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x4_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x4_UNORM;

                return true;

            case TextureFormat.ASTC5x5:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x5_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x5_UNORM;

                return true;

            case TextureFormat.ASTC6x5:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x5_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x5_UNORM;

                return true;

            case TextureFormat.ASTC6x6:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x6_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x6_UNORM;

                return true;

            case TextureFormat.ASTC8x5:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x5_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x5_UNORM;

                return true;

            case TextureFormat.ASTC8x6:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x6_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x6_UNORM;

                return true;

            case TextureFormat.ASTC8x8:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x8_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x8_UNORM;

                return true;

            case TextureFormat.ASTC10x5:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x5_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x5_UNORM;

                return true;

            case TextureFormat.ASTC10x6:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x6_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x6_UNORM;

                return true;

            case TextureFormat.ASTC10x8:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x8_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x8_UNORM;

                return true;

            case TextureFormat.ASTC10x10:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x10_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x10_UNORM;

                return true;

            case TextureFormat.ASTC12x10:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x10_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x10_UNORM;

                return true;

            case TextureFormat.ASTC12x12:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x12_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x12_UNORM;

                return true;

            case TextureFormat.ASTC4x4F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_4x4_FLOAT;

                return true;

            case TextureFormat.ASTC5x4F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x4_FLOAT;

                return true;

            case TextureFormat.ASTC5x5F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x5_FLOAT;

                return true;

            case TextureFormat.ASTC6x5F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x5_FLOAT;

                return true;

            case TextureFormat.ASTC6x6F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x6_FLOAT;

                return true;

            case TextureFormat.ASTC8x5F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x5_FLOAT;

                return true;

            case TextureFormat.ASTC8x6F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x6_FLOAT;

                return true;

            case TextureFormat.ASTC8x8F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x8_FLOAT;

                return true;

            case TextureFormat.ASTC10x5F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x5_FLOAT;

                return true;

            case TextureFormat.ASTC10x6F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x6_FLOAT;

                return true;

            case TextureFormat.ASTC10x8F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x8_FLOAT;

                return true;

            case TextureFormat.ASTC10x10F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x10_FLOAT;

                return true;

            case TextureFormat.ASTC12x10F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x10_FLOAT;

                return true;

            case TextureFormat.ASTC12x12F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x12_FLOAT;

                return true;

            case TextureFormat.A8:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_A8_UNORM;

                return true;

            case TextureFormat.R8:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM;

                return true;

            case TextureFormat.R8I:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_INT;

                return true;

            case TextureFormat.R8U:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UINT;

                return true;

            case TextureFormat.R8S:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_SNORM;

                return true;

            case TextureFormat.R16:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_UNORM;

                return true;

            case TextureFormat.R16I:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_INT;

                return true;

            case TextureFormat.R16U:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_UINT;

                return true;

            case TextureFormat.R16F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_FLOAT;

                return true;

            case TextureFormat.R16S:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_SNORM;

                return true;

            case TextureFormat.R32I:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_INT;

                return true;

            case TextureFormat.R32U:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_UINT;

                return true;

            case TextureFormat.R32F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_FLOAT;

                return true;

            case TextureFormat.RG8:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;

                return true;

            case TextureFormat.RG8I:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_INT;

                return true;

            case TextureFormat.RG8U:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UINT;

                return true;

            case TextureFormat.RG8S:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_SNORM;

                return true;

            case TextureFormat.RG16:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_UNORM;

                return true;

            case TextureFormat.RG16I:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_INT;

                return true;

            case TextureFormat.RG16U:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_UINT;

                return true;

            case TextureFormat.RG16F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_FLOAT;

                return true;

            case TextureFormat.RG16S:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_SNORM;

                return true;

            case TextureFormat.RG32I:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_INT;

                return true;

            case TextureFormat.RG32U:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_UINT;

                return true;

            case TextureFormat.RG32F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_FLOAT;

                return true;

            case TextureFormat.BGRA8:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM;

                return true;

            case TextureFormat.RGBA8:

                outValue = hasSRGB ? SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB :
                    SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM;

                return true;

            case TextureFormat.RGBA8I:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_INT;

                return true;

            case TextureFormat.RGBA8U:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UINT;

                return true;

            case TextureFormat.RGBA8S:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_SNORM;

                return true;

            case TextureFormat.RGBA16:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UNORM;

                return true;

            case TextureFormat.RGBA16I:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_INT;

                return true;

            case TextureFormat.RGBA16U:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UINT;

                return true;

            case TextureFormat.RGBA16F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_FLOAT;

                return true;

            case TextureFormat.RGBA16S:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_SNORM;

                return true;

            case TextureFormat.RGBA32I:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_INT;

                return true;

            case TextureFormat.RGBA32U:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_UINT;

                return true;

            case TextureFormat.RGBA32F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_FLOAT;

                return true;

            case TextureFormat.B5G6R5:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B5G6R5_UNORM;

                return true;

            case TextureFormat.BGRA4:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B4G4R4A4_UNORM;

                return true;

            case TextureFormat.BGR5A1:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B5G5R5A1_UNORM;

                return true;

            case TextureFormat.RGB10A2:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R10G10B10A2_UNORM;

                return true;

            case TextureFormat.RG11B10F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R11G11B10_UFLOAT;

                return true;

            case TextureFormat.D16:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D16_UNORM;

                return true;

            case TextureFormat.D24:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM;

                return true;

            case TextureFormat.D24S8:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT;

                return true;

            case TextureFormat.D32S8:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT;

                return true;

            case TextureFormat.D32F:

                outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT;

                return true;
        }

        outValue = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_INVALID;

        return false;
    }

    public static bool TryGetStapleTextureFormat(SDL_GPUTextureFormat format, out TextureFormat outValue)
    {
        switch (format)
        {
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC1_RGBA_UNORM:

                outValue = TextureFormat.BC1;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC2_RGBA_UNORM:

                outValue = TextureFormat.BC2;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC3_RGBA_UNORM:

                outValue = TextureFormat.BC3;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC4_R_UNORM:

                outValue = TextureFormat.BC4;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC5_RG_UNORM:

                outValue = TextureFormat.BC5;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC6H_RGB_FLOAT:

                outValue = TextureFormat.BC6H;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_BC7_RGBA_UNORM:

                outValue = TextureFormat.BC7;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_4x4_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_4x4_UNORM:

                outValue = TextureFormat.ASTC4x4;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x4_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x4_UNORM:

                outValue = TextureFormat.ASTC5x4;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x5_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x5_UNORM:

                outValue = TextureFormat.ASTC5x5;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x5_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x5_UNORM:

                outValue = TextureFormat.ASTC6x5;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x6_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x6_UNORM:

                outValue = TextureFormat.ASTC6x6;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x5_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x5_UNORM:

                outValue = TextureFormat.ASTC8x5;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x6_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x6_UNORM:

                outValue = TextureFormat.ASTC8x6;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x8_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x8_UNORM:

                outValue = TextureFormat.ASTC8x8;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x5_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x5_UNORM:

                outValue = TextureFormat.ASTC10x5;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x6_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x6_UNORM:

                outValue = TextureFormat.ASTC10x6;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x8_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x8_UNORM:

                outValue = TextureFormat.ASTC10x8;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x10_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x10_UNORM:

                outValue = TextureFormat.ASTC10x10;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x10_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x10_UNORM:

                outValue = TextureFormat.ASTC12x10;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x12_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x12_UNORM:

                outValue = TextureFormat.ASTC12x12;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_4x4_FLOAT:

                outValue = TextureFormat.ASTC4x4F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x4_FLOAT:

                outValue = TextureFormat.ASTC5x4F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_5x5_FLOAT:

                outValue = TextureFormat.ASTC5x5F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x5_FLOAT:

                outValue = TextureFormat.ASTC6x5F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_6x6_FLOAT:

                outValue = TextureFormat.ASTC6x6F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x5_FLOAT:

                outValue = TextureFormat.ASTC8x5F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x6_FLOAT:

                outValue = TextureFormat.ASTC8x6F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_8x8_FLOAT:

                outValue = TextureFormat.ASTC8x8F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x5_FLOAT:

                outValue = TextureFormat.ASTC10x5F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x6_FLOAT:

                outValue = TextureFormat.ASTC10x6F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x8_FLOAT:

                outValue = TextureFormat.ASTC10x8F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_10x10_FLOAT:

                outValue = TextureFormat.ASTC10x10F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x10_FLOAT:

                outValue = TextureFormat.ASTC12x10F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_ASTC_12x12_FLOAT:

                outValue = TextureFormat.ASTC12x12F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_A8_UNORM:

                outValue = TextureFormat.A8;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UNORM:

                outValue = TextureFormat.R8;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_INT:

                outValue = TextureFormat.R8I;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_UINT:

                outValue = TextureFormat.R8U;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8_SNORM:

                outValue = TextureFormat.R8S;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_UNORM:

                outValue = TextureFormat.R16;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_INT:

                outValue = TextureFormat.R16I;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_UINT:

                outValue = TextureFormat.R16U;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_FLOAT:

                outValue = TextureFormat.R16F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16_SNORM:

                outValue = TextureFormat.R16S;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_INT:

                outValue = TextureFormat.R32I;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_UINT:

                outValue = TextureFormat.R32U;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32_FLOAT:

                outValue = TextureFormat.R32F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UNORM:

                outValue = TextureFormat.RG8;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_INT:

                outValue = TextureFormat.RG8I;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_UINT:

                outValue = TextureFormat.RG8U;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8_SNORM:

                outValue = TextureFormat.RG8S;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_UNORM:

                outValue = TextureFormat.RG16;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_INT:

                outValue = TextureFormat.RG16I;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_UINT:

                outValue = TextureFormat.RG16U;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_FLOAT:

                outValue = TextureFormat.RG16F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16_SNORM:

                outValue = TextureFormat.RG16S;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_INT:

                outValue = TextureFormat.RG32I;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_UINT:

                outValue = TextureFormat.RG32U;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32_FLOAT:

                outValue = TextureFormat.RG32F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B8G8R8A8_UNORM:

                outValue = TextureFormat.BGRA8;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB:
            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM:

                outValue = TextureFormat.RGBA8;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_INT:

                outValue = TextureFormat.RGBA8I;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UINT:

                outValue = TextureFormat.RGBA8U;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_SNORM:

                outValue = TextureFormat.RGBA8S;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UNORM:

                outValue = TextureFormat.RGBA16;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_INT:

                outValue = TextureFormat.RGBA16I;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_UINT:

                outValue = TextureFormat.RGBA16U;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_FLOAT:

                outValue = TextureFormat.RGBA16F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R16G16B16A16_SNORM:

                outValue = TextureFormat.RGBA16S;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_INT:

                outValue = TextureFormat.RGBA32I;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_UINT:

                outValue = TextureFormat.RGBA32U;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R32G32B32A32_FLOAT:

                outValue = TextureFormat.RGBA32F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B5G6R5_UNORM:

                outValue = TextureFormat.B5G6R5;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B4G4R4A4_UNORM:

                outValue = TextureFormat.BGRA4;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_B5G5R5A1_UNORM:

                outValue = TextureFormat.BGR5A1;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R10G10B10A2_UNORM:

                outValue = TextureFormat.RGB10A2;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_R11G11B10_UFLOAT:

                outValue = TextureFormat.RG11B10F;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D16_UNORM:

                outValue = TextureFormat.D16;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM:

                outValue = TextureFormat.D24;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM_S8_UINT:

                outValue = TextureFormat.D24S8;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT_S8_UINT:

                outValue = TextureFormat.D32S8;

                return true;

            case SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D32_FLOAT:

                outValue = TextureFormat.D32F;

                return true;
        }

        outValue = TextureFormat.RGBA8;

        return false;
    }

    internal bool TryGetTextureSamplers(Texture[] vertexTextures, Texture[] fragmentTextures, Shader.ShaderInstance instance,
        out Span<SDL_GPUTextureSamplerBinding> vertexSamplers, out Span<SDL_GPUTextureSamplerBinding> fragmentSamplers)
    {
        var vertexSamplerCount = instance.vertexTextureBindings.Count;
        var fragmentSamplerCount = instance.fragmentTextureBindings.Count;

        vertexSamplers = vertexSamplerCount > 0 ? textureSampleBindingFrameAllocator.Allocate(vertexSamplerCount) : default;
        fragmentSamplers = fragmentSamplerCount > 0 ? textureSampleBindingFrameAllocator.Allocate(fragmentSamplerCount) : default;

        if (vertexSamplers.IsEmpty == false)
        {
            if(vertexTextures == null)
            {
                vertexSamplers = fragmentSamplers = null;

                return false;
            }

            for (var i = 0; i < vertexSamplers.Length; i++)
            {
                if (vertexTextures[i]?.impl is not SDLGPUTexture texture ||
                    texture.Disposed ||
                    !TryGetTexture(texture.handle, out var resource))
                {
                    vertexSamplers = fragmentSamplers = null;

                    return false;
                }

                unsafe
                {
                    vertexSamplers[i].texture = resource.texture;
                    vertexSamplers[i].sampler = GetSampler(texture.flags);
                }
            }
        }

        if (fragmentSamplers.IsEmpty)
        {
            return true;
        }
        
        if (fragmentTextures == null)
        {
            vertexSamplers = fragmentSamplers = null;

            return false;
        }

        for (var i = 0; i < fragmentSamplers.Length; i++)
        {
            if (fragmentTextures[i]?.impl is not SDLGPUTexture texture ||
                texture.Disposed ||
                !TryGetTexture(texture.handle, out var resource))
            {
                vertexSamplers = fragmentSamplers = null;

                return false;
            }

            unsafe
            {
                fragmentSamplers[i].texture = resource.texture;
                fragmentSamplers[i].sampler = GetSampler(texture.flags);
            }
        }

        return true;
    }
}
