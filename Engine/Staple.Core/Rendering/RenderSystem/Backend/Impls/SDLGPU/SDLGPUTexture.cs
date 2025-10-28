using SDL3;
using System;

namespace Staple.Internal;

internal class SDLGPUTexture(nint device, nint texture, int width, int height, TextureFormat format, TextureFlags flags,
    SDLGPURendererBackend backend) : ITexture
{
    public readonly nint device = device;

    public nint texture = texture;

    public nint transferBuffer;

    public readonly TextureFormat format = format;

    public readonly TextureFlags flags = flags;

    private readonly SDLGPURendererBackend backend = backend;

    public int Width { get; private set; } = width;

    public int Height { get; private set; } = height;

    public bool Disposed { get; private set; }

    public static SDL.SDL_GPUTextureType GetTextureType(TextureFlags flags)
    {
        if(flags.HasFlag(TextureFlags.TextureTypeCube))
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
            if(flags.HasFlag(TextureFlags.ComputeRead))
            {
                f |= SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_READ;
            }

            if (flags.HasFlag(TextureFlags.ComputeWrite))
            {
                f |= SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COMPUTE_STORAGE_WRITE;
            }

            if(flags.HasFlag(TextureFlags.Readback))
            {
                f |= SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_GRAPHICS_STORAGE_READ;
            }

            return f;
        }

        if (flags.HasFlag(TextureFlags.ColorTarget))
        {
            return HandleFlags(SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET);
        }
        else if(flags.HasFlag(TextureFlags.DepthStencilTarget))
        {
            return HandleFlags(SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_DEPTH_STENCIL_TARGET);
        }

        return HandleFlags(SDL.SDL_GPUTextureUsageFlags.SDL_GPU_TEXTUREUSAGE_SAMPLER);
    }

    public static bool TryGetTextureFormat(TextureFormat format, TextureFlags flags, out SDL.SDL_GPUTextureFormat outValue)
    {
        var hasSRGB = flags.HasFlag(TextureFlags.SRGB);

        switch(format)
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

    public void Destroy()
    {
        if(Disposed)
        {
            return;
        }

        Disposed = true;

        void Finish()
        {
            SDL.SDL_WaitForGPUIdle(device);

            if (texture != nint.Zero)
            {
                SDL.SDL_ReleaseGPUTexture(device, texture);

                texture = nint.Zero;
            }

            if (transferBuffer != nint.Zero)
            {
                SDL.SDL_ReleaseGPUTransferBuffer(device, transferBuffer);

                transferBuffer = nint.Zero;
            }
        }

        if (backend.CanUpdateResources == false)
        {
            SDLGPURendererBackend.ReportResourceUnavailability();

            backend.QueueRenderUpdate(Finish);

            return;
        }

        Finish();
    }

    public void Update(Span<byte> data)
    {
        if(Disposed)
        {
            return;
        }

        if (backend.CanUpdateResources == false)
        {
            SDLGPURendererBackend.ReportResourceUnavailability();

            return;
        }

        if (backend.TryGetCommandBuffer(out var command) == false)
        {
            return;
        }

        if (transferBuffer == nint.Zero)
        {
            var info = new SDL.SDL_GPUTransferBufferCreateInfo()
            {
                size = (uint)data.Length,
                usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            };

            transferBuffer = SDL.SDL_CreateGPUTransferBuffer(device, in info);

            if (transferBuffer == nint.Zero)
            {
                return;
            }
        }

        var copyPass = SDL.SDL_BeginGPUCopyPass(command);

        if (copyPass == nint.Zero)
        {
            return;
        }

        var mapData = SDL.SDL_MapGPUTransferBuffer(device, transferBuffer, true);

        unsafe
        {
            var to = new Span<byte>((void*)mapData, data.Length);

            data.CopyTo(to);
        }

        SDL.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

        var textureInfo = new SDL.SDL_GPUTextureTransferInfo()
        {
            offset = 0,
            pixels_per_row = (uint)Width,
            rows_per_layer = (uint)Height,
            transfer_buffer = transferBuffer,
        };

        var destination = new SDL.SDL_GPUTextureRegion()
        {
            texture = texture,
            w = (uint)Width,
            h = (uint)Height,
            d = 1,
        };

        SDL.SDL_UploadToGPUTexture(copyPass, in textureInfo, in destination, true);

        SDL.SDL_EndGPUCopyPass(copyPass);
    }
}
