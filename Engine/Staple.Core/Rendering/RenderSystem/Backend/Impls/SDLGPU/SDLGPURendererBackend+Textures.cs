using SDL3;
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

        resource.transferBuffer = nint.Zero;

        if (resource.texture != nint.Zero)
        {
            SDL.ReleaseGPUTexture(device, resource.texture);

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

    internal nint GetSampler(TextureFlags flags)
    {
        if (textureSamplers.TryGetValue(flags, out var sampler))
        {
            return sampler;
        }

        var anisotropy = flags.HasFlag(TextureFlags.AnisotropicFilter);

        var uMode = flags switch
        {
            _ when flags.HasFlag(TextureFlags.RepeatU) => SDL.GPUSamplerAddressMode.Repeat,
            _ when flags.HasFlag(TextureFlags.MirrorU) => SDL.GPUSamplerAddressMode.MirroredRepeat,
            _ => SDL.GPUSamplerAddressMode.ClampToEdge,
        };

        var vMode = flags switch
        {
            _ when flags.HasFlag(TextureFlags.RepeatV) => SDL.GPUSamplerAddressMode.Repeat,
            _ when flags.HasFlag(TextureFlags.MirrorV) => SDL.GPUSamplerAddressMode.MirroredRepeat,
            _ => SDL.GPUSamplerAddressMode.ClampToEdge,
        };

        var wMode = flags switch
        {
            _ when flags.HasFlag(TextureFlags.RepeatW) => SDL.GPUSamplerAddressMode.Repeat,
            _ when flags.HasFlag(TextureFlags.MirrorW) => SDL.GPUSamplerAddressMode.MirroredRepeat,
            _ => SDL.GPUSamplerAddressMode.ClampToEdge,
        };

        var magFilter = flags.HasFlag(TextureFlags.LinearFilter) ? SDL.GPUFilter.Linear :
            SDL.GPUFilter.Nearest;

        var mipmapMode = flags.HasFlag(TextureFlags.LinearFilter) ? SDL.GPUSamplerMipmapMode.Linear :
            SDL.GPUSamplerMipmapMode.Nearest;

        var info = new SDL.GPUSamplerCreateInfo()
        {
            AddressModeU = uMode,
            AddressModeV = vMode,
            AddressModeW = wMode,
            EnableAnisotropy = (byte)(anisotropy ? 1 : 0),
            MagFilter = magFilter,
            MinFilter = magFilter,
            MipmapMode = mipmapMode,
            MaxAnisotropy = 16,
        };

        sampler = SDL.CreateGPUSampler(device, in info);

        if (sampler != nint.Zero)
        {
            textureSamplers.Add(flags, sampler);
        }

        return sampler;
    }

    internal void DestroyTexture(ResourceHandle<Texture> handle)
    {
        AddCommand(new SDLGPUDestroyTextureCommand(this, handle));
    }

    public ITexture CreateTextureAssetTexture(SerializableTexture asset, TextureFlags flags)
    {
        var format = asset.metadata.Format;

        if (!TryGetTextureFormat(format, flags, out var textureFormat))
        {
            return null;
        }

        var info = new SDL.GPUTextureCreateInfo()
        {
            Format = textureFormat,
            Width = (uint)asset.width,
            Height = (uint)asset.height,
            Type = GetTextureType(flags),
            Usage = GetTextureUsage(flags),
            LayerCountOrDepth = 1,
            NumLevels = 1,
        };

        var texture = SDL.CreateGPUTexture(device, in info);

        if (texture == nint.Zero)
        {
            return null;
        }

        var handle = ReserveTextureResource(textures, texture, asset.width, asset.height, format, flags);

        if (!handle.IsValid)
        {
            SDL.ReleaseGPUTexture(device, texture);

            return null;
        }

        var outValue = new SDLGPUTexture(handle, asset.width, asset.height, format, flags, this);

        outValue.Update(asset.data);

        return outValue;
    }

    public ITexture CreatePixelTexture(byte[] data, int width, int height, TextureFormat format, TextureFlags flags)
    {
        if (!TryGetTextureFormat(format, flags, out var textureFormat))
        {
            return null;
        }

        var info = new SDL.GPUTextureCreateInfo()
        {
            Format = textureFormat,
            Width = (uint)width,
            Height = (uint)height,
            Type = GetTextureType(flags),
            Usage = GetTextureUsage(flags),
            LayerCountOrDepth = 1,
            NumLevels = 1, //TODO: Support multiple levels
        };

        var texture = SDL.CreateGPUTexture(device, in info);

        if (texture == nint.Zero)
        {
            return null;
        }

        var handle = ReserveTextureResource(textures, texture, width, height, format, flags);

        if(!handle.IsValid)
        {
            SDL.ReleaseGPUTexture(device, texture);

            return null;
        }

        var outValue = new SDLGPUTexture(handle, width, height, format, flags, this);

        outValue.Update(data);

        return outValue;
    }

    public ITexture CreateEmptyTexture(int width, int height, TextureFormat format, TextureFlags flags)
    {
        if (!TryGetTextureFormat(format, flags, out var textureFormat))
        {
            return null;
        }

        var info = new SDL.GPUTextureCreateInfo()
        {
            Format = textureFormat,
            Width = (uint)width,
            Height = (uint)height,
            Type = GetTextureType(flags),
            Usage = GetTextureUsage(flags),
            LayerCountOrDepth = 1,
            NumLevels = 1,
        };

        var texture = SDL.CreateGPUTexture(device, in info);

        if (texture == nint.Zero)
        {
            return null;
        }

        var handle = ReserveTextureResource(textures, texture, width, height, format, flags);

        if (!handle.IsValid || !TryGetTexture(handle, out var resource))
        {
            SDL.ReleaseGPUTexture(device, texture);

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

    public static SDL.GPUTextureType GetTextureType(TextureFlags flags)
    {
        if (flags.HasFlag(TextureFlags.TextureTypeCube))
        {
            return SDL.GPUTextureType.TexturetypeCube;
        }

        if (flags.HasFlag(TextureFlags.TextureTypeCubeArray))
        {
            return SDL.GPUTextureType.TexturetypeCubeArray;
        }

        if (flags.HasFlag(TextureFlags.TextureType2DArray))
        {
            return SDL.GPUTextureType.Texturetype2DArray;
        }

        if (flags.HasFlag(TextureFlags.TextureType3D))
        {
            return SDL.GPUTextureType.Texturetype3D;
        }

        return SDL.GPUTextureType.Texturetype2D;
    }

    public static SDL.GPUTextureUsageFlags GetTextureUsage(TextureFlags flags)
    {
        if (flags.HasFlag(TextureFlags.ColorTarget))
        {
            var usageFlags = SDL.GPUTextureUsageFlags.Sampler |
                SDL.GPUTextureUsageFlags.ColorTarget;

            if (flags.HasFlag(TextureFlags.ComputeRead))
            {
                usageFlags |= SDL.GPUTextureUsageFlags.ComputeStorageRead;
            }

            if (flags.HasFlag(TextureFlags.ComputeWrite))
            {
                usageFlags |= SDL.GPUTextureUsageFlags.ComputeStorageWrite;
            }

            if (flags.HasFlag(TextureFlags.Readback))
            {
                usageFlags |= SDL.GPUTextureUsageFlags.GraphicsStorageRead;
            }

            return usageFlags;
        }
        else if (flags.HasFlag(TextureFlags.DepthStencilTarget))
        {
            var usageFlags = SDL.GPUTextureUsageFlags.Sampler | 
                SDL.GPUTextureUsageFlags.DepthStencilTarget;

            if (flags.HasFlag(TextureFlags.ComputeRead))
            {
                usageFlags |= SDL.GPUTextureUsageFlags.ComputeStorageRead;
            }

            if (flags.HasFlag(TextureFlags.ComputeWrite))
            {
                usageFlags |= SDL.GPUTextureUsageFlags.ComputeStorageWrite;
            }

            if (flags.HasFlag(TextureFlags.Readback))
            {
                usageFlags |= SDL.GPUTextureUsageFlags.GraphicsStorageRead;
            }

            return usageFlags;
        }

        {
            var usageFlags = SDL.GPUTextureUsageFlags.Sampler;

            if (flags.HasFlag(TextureFlags.ComputeRead))
            {
                usageFlags |= SDL.GPUTextureUsageFlags.ComputeStorageRead;
            }

            if (flags.HasFlag(TextureFlags.ComputeWrite))
            {
                usageFlags |= SDL.GPUTextureUsageFlags.ComputeStorageWrite;
            }

            if (flags.HasFlag(TextureFlags.Readback))
            {
                usageFlags |= SDL.GPUTextureUsageFlags.GraphicsStorageRead;
            }

            return usageFlags;
        }
    }

    public static bool TryGetTextureFormat(TextureFormat format, TextureFlags flags, out SDL.GPUTextureFormat outValue)
    {
        var hasSRGB = flags.HasFlag(TextureFlags.SRGB);

        switch (format)
        {
            case TextureFormat.BC1:

                outValue = hasSRGB ? SDL.GPUTextureFormat.BC1RGBAUnormSRGB :
                    SDL.GPUTextureFormat.BC1RGBAUnorm;

                return true;

            case TextureFormat.BC2:

                outValue = hasSRGB ? SDL.GPUTextureFormat.BC2RGBAUnormSRGB :
                    SDL.GPUTextureFormat.BC2RGBAUnorm;

                return true;

            case TextureFormat.BC3:

                outValue = hasSRGB ? SDL.GPUTextureFormat.BC3RGBAUnormSRGB :
                    SDL.GPUTextureFormat.BC3RGBAUnorm;

                return true;

            case TextureFormat.BC4:

                outValue = SDL.GPUTextureFormat.BC4RUnorm;

                return true;

            case TextureFormat.BC5:

                outValue = SDL.GPUTextureFormat.BC5RGUnorm;

                return true;

            case TextureFormat.BC6H:

                outValue = SDL.GPUTextureFormat.BC6HRGBFloat;

                return true;

            case TextureFormat.BC7:

                outValue = hasSRGB ? SDL.GPUTextureFormat.BC7RGBAUnormSRGB :
                    SDL.GPUTextureFormat.BC7RGBAUnorm;

                return true;

            case TextureFormat.ASTC4x4:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC4X4UnormSRGB :
                    SDL.GPUTextureFormat.ASTC4X4Unorm;

                return true;

            case TextureFormat.ASTC5x4:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC5X4UnormSRGB :
                    SDL.GPUTextureFormat.ASTC5X4Unorm;

                return true;

            case TextureFormat.ASTC5x5:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC5X5UnormSRGB :
                    SDL.GPUTextureFormat.ASTC5X5Unorm;

                return true;

            case TextureFormat.ASTC6x5:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC6X5UnormSRGB :
                    SDL.GPUTextureFormat.ASTC5X5Unorm;

                return true;

            case TextureFormat.ASTC6x6:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC6X6UnormSRGB :
                    SDL.GPUTextureFormat.ASTC6X6Unorm;

                return true;

            case TextureFormat.ASTC8x5:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC8X5UnormSRGB :
                    SDL.GPUTextureFormat.ASTC8X5Unorm;

                return true;

            case TextureFormat.ASTC8x6:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC8X6UnormSRGB :
                    SDL.GPUTextureFormat.ASTC8X6Unorm;

                return true;

            case TextureFormat.ASTC8x8:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC8X8UnormSRGB :
                    SDL.GPUTextureFormat.ASTC8X8Unorm;

                return true;

            case TextureFormat.ASTC10x5:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC10X5UnormSRGB :
                    SDL.GPUTextureFormat.ASTC10X5Unorm;

                return true;

            case TextureFormat.ASTC10x6:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC10X6UnormSRGB :
                    SDL.GPUTextureFormat.ASTC10X6Unorm;

                return true;

            case TextureFormat.ASTC10x8:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC10X8UnormSRGB :
                    SDL.GPUTextureFormat.ASTC10X8Unorm;

                return true;

            case TextureFormat.ASTC10x10:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC10X10UnormSRGB :
                    SDL.GPUTextureFormat.ASTC10X10Unorm;

                return true;

            case TextureFormat.ASTC12x10:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC12X10UnormSRGB :
                    SDL.GPUTextureFormat.ASTC12X10Unorm;

                return true;

            case TextureFormat.ASTC12x12:

                outValue = hasSRGB ? SDL.GPUTextureFormat.ASTC12X12UnormSRGB :
                    SDL.GPUTextureFormat.ASTC12X12Unorm;

                return true;

            case TextureFormat.ASTC4x4F:

                outValue = SDL.GPUTextureFormat.ASTC4X4Float;

                return true;

            case TextureFormat.ASTC5x4F:

                outValue = SDL.GPUTextureFormat.ASTC5X4Float;

                return true;

            case TextureFormat.ASTC5x5F:

                outValue = SDL.GPUTextureFormat.ASTC5X5Float;

                return true;

            case TextureFormat.ASTC6x5F:

                outValue = SDL.GPUTextureFormat.ASTC6X5Float;

                return true;

            case TextureFormat.ASTC6x6F:

                outValue = SDL.GPUTextureFormat.ASTC6X6Float;

                return true;

            case TextureFormat.ASTC8x5F:

                outValue = SDL.GPUTextureFormat.ASTC8X5Float;

                return true;

            case TextureFormat.ASTC8x6F:

                outValue = SDL.GPUTextureFormat.ASTC8X6Float;

                return true;

            case TextureFormat.ASTC8x8F:

                outValue = SDL.GPUTextureFormat.ASTC8X8Float;

                return true;

            case TextureFormat.ASTC10x5F:

                outValue = SDL.GPUTextureFormat.ASTC10X5Float;

                return true;

            case TextureFormat.ASTC10x6F:

                outValue = SDL.GPUTextureFormat.ASTC10X6Float;

                return true;

            case TextureFormat.ASTC10x8F:

                outValue = SDL.GPUTextureFormat.ASTC10X8Float;

                return true;

            case TextureFormat.ASTC10x10F:

                outValue = SDL.GPUTextureFormat.ASTC10X10Float;

                return true;

            case TextureFormat.ASTC12x10F:

                outValue = SDL.GPUTextureFormat.ASTC12X10Float;

                return true;

            case TextureFormat.ASTC12x12F:

                outValue = SDL.GPUTextureFormat.ASTC12X12Float;

                return true;

            case TextureFormat.A8:

                outValue = SDL.GPUTextureFormat.A8Unorm;

                return true;

            case TextureFormat.R8:

                outValue = SDL.GPUTextureFormat.R8Unorm;

                return true;

            case TextureFormat.R8I:

                outValue = SDL.GPUTextureFormat.R8Int;

                return true;

            case TextureFormat.R8U:

                outValue = SDL.GPUTextureFormat.R8Uint;

                return true;

            case TextureFormat.R8S:

                outValue = SDL.GPUTextureFormat.R8Snorm;

                return true;

            case TextureFormat.R16:

                outValue = SDL.GPUTextureFormat.R16Unorm;

                return true;

            case TextureFormat.R16I:

                outValue = SDL.GPUTextureFormat.R16Int;

                return true;

            case TextureFormat.R16U:

                outValue = SDL.GPUTextureFormat.R16Uint;

                return true;

            case TextureFormat.R16F:

                outValue = SDL.GPUTextureFormat.R16Float;

                return true;

            case TextureFormat.R16S:

                outValue = SDL.GPUTextureFormat.R16Snorm;

                return true;

            case TextureFormat.R32I:

                outValue = SDL.GPUTextureFormat.R32Int;

                return true;

            case TextureFormat.R32U:

                outValue = SDL.GPUTextureFormat.R32Uint;

                return true;

            case TextureFormat.R32F:

                outValue = SDL.GPUTextureFormat.R32Float;

                return true;

            case TextureFormat.RG8:

                outValue = SDL.GPUTextureFormat.R8G8Unorm;

                return true;

            case TextureFormat.RG8I:

                outValue = SDL.GPUTextureFormat.R8G8Int;

                return true;

            case TextureFormat.RG8U:

                outValue = SDL.GPUTextureFormat.R8G8Uint;

                return true;

            case TextureFormat.RG8S:

                outValue = SDL.GPUTextureFormat.R8G8Snorm;

                return true;

            case TextureFormat.RG16:

                outValue = SDL.GPUTextureFormat.R16G16Unorm;

                return true;

            case TextureFormat.RG16I:

                outValue = SDL.GPUTextureFormat.R16G16Int;

                return true;

            case TextureFormat.RG16U:

                outValue = SDL.GPUTextureFormat.R16G16Uint;

                return true;

            case TextureFormat.RG16F:

                outValue = SDL.GPUTextureFormat.R16G16Float;

                return true;

            case TextureFormat.RG16S:

                outValue = SDL.GPUTextureFormat.R16G16Snorm;

                return true;

            case TextureFormat.RG32I:

                outValue = SDL.GPUTextureFormat.R32G32Int;

                return true;

            case TextureFormat.RG32U:

                outValue = SDL.GPUTextureFormat.R32G32Uint;

                return true;

            case TextureFormat.RG32F:

                outValue = SDL.GPUTextureFormat.R32G32Float;

                return true;

            case TextureFormat.BGRA8:

                outValue = hasSRGB ? SDL.GPUTextureFormat.B8G8R8A8UnormSRGB :
                    SDL.GPUTextureFormat.B8G8R8A8Unorm;

                return true;

            case TextureFormat.RGBA8:

                outValue = hasSRGB ? SDL.GPUTextureFormat.R8G8B8A8UnormSRGB :
                    SDL.GPUTextureFormat.R8G8B8A8Unorm;

                return true;

            case TextureFormat.RGBA8I:

                outValue = SDL.GPUTextureFormat.R8G8B8A8Int;

                return true;

            case TextureFormat.RGBA8U:

                outValue = SDL.GPUTextureFormat.R8G8B8A8Uint;

                return true;

            case TextureFormat.RGBA8S:

                outValue = SDL.GPUTextureFormat.R8G8B8A8Snorm;

                return true;

            case TextureFormat.RGBA16:

                outValue = SDL.GPUTextureFormat.R16G16B16A16Unorm;

                return true;

            case TextureFormat.RGBA16I:

                outValue = SDL.GPUTextureFormat.R16G16B16A16Int;

                return true;

            case TextureFormat.RGBA16U:

                outValue = SDL.GPUTextureFormat.R16G16B16A16Uint;

                return true;

            case TextureFormat.RGBA16F:

                outValue = SDL.GPUTextureFormat.R16G16B16A16Float;

                return true;

            case TextureFormat.RGBA16S:

                outValue = SDL.GPUTextureFormat.R16G16B16A16Snorm;

                return true;

            case TextureFormat.RGBA32I:

                outValue = SDL.GPUTextureFormat.R32G32B32A32Int;

                return true;

            case TextureFormat.RGBA32U:

                outValue = SDL.GPUTextureFormat.R32G32B32A32Uint;

                return true;

            case TextureFormat.RGBA32F:

                outValue = SDL.GPUTextureFormat.R32G32B32A32Float;

                return true;

            case TextureFormat.B5G6R5:

                outValue = SDL.GPUTextureFormat.B5G6R5Unorm;

                return true;

            case TextureFormat.BGRA4:

                outValue = SDL.GPUTextureFormat.B4G4R4A4Unorm;

                return true;

            case TextureFormat.BGR5A1:

                outValue = SDL.GPUTextureFormat.B5G5R5A1Unorm;

                return true;

            case TextureFormat.RGB10A2:

                outValue = SDL.GPUTextureFormat.R10G10B10A2Unorm;

                return true;

            case TextureFormat.RG11B10F:

                outValue = SDL.GPUTextureFormat.R11G11B10UFloat;

                return true;

            case TextureFormat.D16:

                outValue = SDL.GPUTextureFormat.D16Unorm;

                return true;

            case TextureFormat.D24:

                outValue = SDL.GPUTextureFormat.D24Unorm;

                return true;

            case TextureFormat.D24S8:

                outValue = SDL.GPUTextureFormat.D24UnormS8Uint;

                return true;

            case TextureFormat.D32S8:

                outValue = SDL.GPUTextureFormat.D32FloatS8Uint;

                return true;

            case TextureFormat.D32F:

                outValue = SDL.GPUTextureFormat.D32Float;

                return true;
        }

        outValue = SDL.GPUTextureFormat.Invalid;

        return false;
    }

    internal bool TryGetTextureSamplers(Texture[] vertexTextures, Texture[] fragmentTextures, Shader.ShaderInstance instance,
        out Span<SDL.GPUTextureSamplerBinding> vertexSamplers, out Span<SDL.GPUTextureSamplerBinding> fragmentSamplers)
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

                vertexSamplers[i].Texture = resource.texture;
                vertexSamplers[i].Sampler = GetSampler(texture.flags);
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

            fragmentSamplers[i].Texture = resource.texture;
            fragmentSamplers[i].Sampler = GetSampler(texture.flags);
        }

        return true;
    }
}
