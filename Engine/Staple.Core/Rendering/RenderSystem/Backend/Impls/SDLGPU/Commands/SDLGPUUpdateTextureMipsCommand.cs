using SDL;
using System;

namespace Staple.Internal;

internal unsafe class SDLGPUUpdateTextureMipsCommand(SDLGPURendererBackend backend, ResourceHandle<Texture> handle, TextureMipData[] mips) :
    IRenderCommand
{
    public void Update()
    {
        if (!backend.TryGetTexture(handle, out var resource) || !backend.BeginCopyPass())
        {
            return;
        }

        for(var i = 0; i < mips.Length; i++)
        {
            bool UpdateNormal()
            {
                ref var mip = ref mips[i];

                if (resource.length != mip.data.Length || resource.transferBuffer == null)
                {
                    resource.transferBuffer = backend.GetTransferBuffer(false, mip.data.Length);

                    if (resource.transferBuffer == null)
                    {
                        return false;
                    }
                }

                var mapData = SDL3.SDL_MapGPUTransferBuffer(backend.device, resource.transferBuffer, true);

                var mapDataSpan = new Span<byte>((void*)mapData, mip.data.Length);

                mip.data.AsSpan().CopyTo(mapDataSpan);

                SDL3.SDL_UnmapGPUTransferBuffer(backend.device, resource.transferBuffer);

                var textureInfo = new SDL_GPUTextureTransferInfo()
                {
                    offset = 0,
                    pixels_per_row = (uint)mip.width,
                    rows_per_layer = (uint)mip.height,
                    transfer_buffer = resource.transferBuffer,
                };

                var destination = new SDL_GPUTextureRegion()
                {
                    texture = resource.texture,
                    w = (uint)mip.width,
                    h = (uint)mip.height,
                    d = 1,
                    mip_level = (uint)i,
                };

                SDL3.SDL_UploadToGPUTexture(backend.copyPass, &textureInfo, &destination, false);

                return true;
            }

            bool UpdateCubeMap()
            {
                ref var mip = ref mips[i];

                var sliceLength = mip.data.Length / 6;

                for(int j = 0, sliceIndex = 0; j < 6; j++, sliceIndex += sliceLength)
                {
                    if (resource.length != sliceLength || resource.transferBuffer == null)
                    {
                        resource.transferBuffer = backend.GetTransferBuffer(false, sliceLength);

                        if (resource.transferBuffer == null)
                        {
                            return false;
                        }
                    }

                    var mapData = SDL3.SDL_MapGPUTransferBuffer(backend.device, resource.transferBuffer, true);

                    var mapDataSpan = new Span<byte>((void*)mapData, sliceLength);

                    mip.data.AsSpan(sliceIndex, sliceLength).CopyTo(mapDataSpan);

                    SDL3.SDL_UnmapGPUTransferBuffer(backend.device, resource.transferBuffer);

                    var textureInfo = new SDL_GPUTextureTransferInfo()
                    {
                        offset = 0,
                        pixels_per_row = (uint)mip.height,
                        rows_per_layer = (uint)mip.height,
                        transfer_buffer = resource.transferBuffer,
                    };

                    var destination = new SDL_GPUTextureRegion()
                    {
                        texture = resource.texture,
                        w = (uint)mip.height,
                        h = (uint)mip.height,
                        d = 1,
                        mip_level = (uint)i,
                        layer = (uint)j,
                    };

                    SDL3.SDL_UploadToGPUTexture(backend.copyPass, &textureInfo, &destination, false);
                }

                return true;
            }

            switch (resource.type)
            {
                case TextureType.CubeMap:

                    if(!UpdateCubeMap())
                    {
                        return;
                    }

                    break;

                default:

                    if(!UpdateNormal())
                    {
                        return;
                    }

                    break;
            }
        }
    }
}
