using SDL;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal partial class SDLGPURendererBackend
{
    internal unsafe SDL_GPUTransferBuffer *GetTransferBuffer(bool download, int length)
    {
        var key = new TransferBufferCacheKey(download, length);

        if(cachedtransferBuffers.TryGetValue(key, out var buffer))
        {
            return buffer.ptr;
        }

        var transferInfo = new SDL_GPUTransferBufferCreateInfo()
        {
            size = (uint)length,
            usage = download ? SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_DOWNLOAD :
                SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
        };

        buffer = new(SDL3.SDL_CreateGPUTransferBuffer(device, &transferInfo));

        if (buffer.ptr != null)
        {
            cachedtransferBuffers.Add(key, buffer);
        }

        return buffer.ptr;
    }

    internal unsafe void ReleaseBufferResource(BufferResource resource)
    {
        if (!(resource?.used ?? false))
        {
            return;
        }

        resource.transferBuffer = null;

        if (resource.buffer != null)
        {
            SDL3.SDL_ReleaseGPUBuffer(device, resource.buffer);

            resource.buffer = null;
        }

        resource.used = false;
    }

    internal static ResourceHandle<T> ReserveResourceBuffer<T>(BufferResource[] resources, RenderBufferFlags flags)
    {
        for (var i = 0; i < resources.Length; i++)
        {
            if (resources[i]?.used ?? false)
            {
                continue;
            }

            resources[i] ??= new();

            resources[i].used = true;
            resources[i].flags = flags;

            return new ResourceHandle<T>((ushort)i);
        }

        return ResourceHandle<T>.Invalid;
    }

    internal bool TryGetVertexBuffer(ResourceHandle<VertexBuffer> handle, out BufferResource resource)
    {
        if (!handle.IsValid ||
            !(vertexBuffers[handle.handle]?.used ?? false))
        {
            resource = null;

            return false;
        }

        resource = vertexBuffers[handle.handle];

        return true;
    }

    internal bool TryGetIndexBuffer(ResourceHandle<IndexBuffer> handle, out BufferResource resource)
    {
        if (!handle.IsValid ||
            !(indexBuffers[handle.handle]?.used ?? false))
        {
            resource = null;

            return false;
        }

        resource = indexBuffers[handle.handle];

        return true;
    }

    public VertexBuffer CreateVertexBuffer(Span<byte> data, VertexLayout layout, RenderBufferFlags flags)
    {
        if (layout == null || data.Length == 0)
        {
            return null;
        }

        var handle = ReserveResourceBuffer<VertexBuffer>(vertexBuffers, flags);

        if (!handle.IsValid)
        {
            return null;
        }

        var outValue = new SDLGPUVertexBuffer(handle, layout, this)
        {
            Flags = flags,
        };

        outValue.Update(data);

        return outValue.IsValid ? outValue : null;
    }

    public VertexBuffer CreateVertexBuffer<T>(Span<T> data, VertexLayout layout, RenderBufferFlags flags) where T : unmanaged
    {
        if (layout == null || data.Length == 0)
        {
            return null;
        }

        var handle = ReserveResourceBuffer<VertexBuffer>(vertexBuffers, flags);

        if (!handle.IsValid)
        {
            return null;
        }

        var outValue = new SDLGPUVertexBuffer(handle, layout, this)
        {
            Flags = flags,
        };

        outValue.Update(data);

        return outValue.IsValid ? outValue : null;
    }

    public IndexBuffer CreateIndexBuffer(Span<ushort> data, RenderBufferFlags flags)
    {
        var handle = ReserveResourceBuffer<IndexBuffer>(indexBuffers, flags);

        if (!handle.IsValid)
        {
            return null;
        }

        var outValue = new SDLGPUIndexBuffer(handle, this)
        {
            Flags = flags,
        };

        outValue.Update(data);

        return outValue.Valid ? outValue : null;
    }

    public IndexBuffer CreateIndexBuffer(Span<uint> data, RenderBufferFlags flags)
    {
        var handle = ReserveResourceBuffer<IndexBuffer>(indexBuffers, flags);

        if (!handle.IsValid)
        {
            return null;
        }

        var outValue = new SDLGPUIndexBuffer(handle, this)
        {
            Flags = flags,
        };

        outValue.Update(data);

        return outValue.Valid ? outValue : null;
    }

    public VertexLayoutBuilder CreateVertexLayoutBuilder()
    {
        return new SDLGPUVertexLayoutBuilder();
    }

    public void UpdateVertexBuffer(ResourceHandle<VertexBuffer> buffer, Span<byte> data)
    {
        if(data.Length == 0)
        {
            return;
        }

        AddCommand(new SDLGPUUpdateVertexBufferCommand(this, buffer, data.ToArray()));
    }

    public void UpdateIndexBuffer(ResourceHandle<IndexBuffer> buffer, Span<ushort> data)
    {
        if(data.Length == 0)
        {
            return;
        }

        unsafe
        {
            var holder = new byte[data.Length * sizeof(ushort)];

            fixed(void *ptr = holder)
            {
                var target = new Span<ushort>(ptr, data.Length);

                data.CopyTo(target);
            }

            AddCommand(new SDLGPUUpdateIndexBufferCommand(this, buffer, holder));
        }
    }

    public void UpdateIndexBuffer(ResourceHandle<IndexBuffer> buffer, Span<uint> data)
    {
        if (data.Length == 0)
        {
            return;
        }

        unsafe
        {
            var holder = new byte[data.Length * sizeof(uint)];

            fixed (void* ptr = holder)
            {
                var target = new Span<uint>(ptr, data.Length);

                data.CopyTo(target);
            }

            AddCommand(new SDLGPUUpdateIndexBufferCommand(this, buffer, holder));
        }
    }

    public void DestroyVertexBuffer(ResourceHandle<VertexBuffer> buffer)
    {
        AddCommand(new SDLGPUDestroyVertexBufferCommand(this, buffer));
    }

    public void DestroyIndexBuffer(ResourceHandle<IndexBuffer> buffer)
    {
        AddCommand(new SDLGPUDestroyIndexBufferCommand(this, buffer));
    }

    private unsafe void UpdateEntityTransformBuffer()
    {
        var elementCount = RenderSystem.Instance.entityTransforms.Length;

        var targetLength = elementCount * Matrix4x4ByteSize;

        if (entityTransformsBuffer != null &&
            entityTransformsBufferLength >= targetLength &&
            RenderSystem.Instance.changedEntityTransformRanges.Count == 0)
        {
            return;
        }

        if (entityTransformsBuffer == null || entityTransformsBufferLength != targetLength)
        {
            entityTransformsBufferLength = targetLength;

            if (entityTransformsBuffer != null)
            {
                SDL3.SDL_ReleaseGPUBuffer(device, entityTransformsBuffer);

                entityTransformsBuffer = null;
            }

            var createInfo = new SDL_GPUBufferCreateInfo()
            {
                size = (uint)targetLength,
                usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX |
                    SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ,
            };

            entityTransformsBuffer = SDL3.SDL_CreateGPUBuffer(device, &createInfo);

            if (entityTransformsBuffer == null)
            {
                return;
            }

            var transferBuffer = GetTransferBuffer(false, targetLength);

            if (transferBuffer == null)
            {
                SDL3.SDL_ReleaseGPUBuffer(device, entityTransformsBuffer);

                entityTransformsBuffer = null;

                return;
            }

            if (!BeginCopyPass())
            {
                return;
            }

            var mapData = SDL3.SDL_MapGPUTransferBuffer(device, transferBuffer, true);

            unsafe
            {
                var from = RenderSystem.Instance.entityTransforms.AsSpan();
                var to = new Span<Matrix4x4>((void*)mapData, elementCount);

                from.CopyTo(to);
            }

            SDL3.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

            var location = new SDL_GPUTransferBufferLocation()
            {
                transfer_buffer = transferBuffer,
            };

            var region = new SDL_GPUBufferRegion()
            {
                buffer = entityTransformsBuffer,
                size = (uint)targetLength,
            };

            SDL3.SDL_UploadToGPUBuffer(copyPass, &location, &region, false);
        }
        else
        {
            if (!BeginCopyPass())
            {
                return;
            }

            foreach (var (start, length) in RenderSystem.Instance.changedEntityTransformRanges)
            {
                targetLength = length * Matrix4x4ByteSize;
                
                var transferBuffer = GetTransferBuffer(false, targetLength);

                if (transferBuffer == null)
                {
                    SDL3.SDL_ReleaseGPUBuffer(device, entityTransformsBuffer);

                    entityTransformsBuffer = null;

                    return;
                }

                var mapData = SDL3.SDL_MapGPUTransferBuffer(device, transferBuffer, true);

                unsafe
                {
                    var from = RenderSystem.Instance.entityTransforms.AsSpan().Slice(start, length);
                    var to = new Span<Matrix4x4>((void*)mapData, length);

                    from.CopyTo(to);
                }

                SDL3.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

                var location = new SDL_GPUTransferBufferLocation()
                {
                    transfer_buffer = transferBuffer,
                };

                var region = new SDL_GPUBufferRegion()
                {
                    buffer = entityTransformsBuffer,
                    offset = (uint)(start * Matrix4x4ByteSize),
                    size = (uint)targetLength,
                };

                SDL3.SDL_UploadToGPUBuffer(copyPass, &location, &region, false);
            }
        }
    }

    public void UpdateStaticMeshVertexBuffer<T>(BufferAttributeSource<T, VertexBuffer> buffer) where T : unmanaged
    {
        unsafe
        {
            var index = buffer.index;
            var targetLength = buffer.allocator.buffer.Length * buffer.allocator.elementSize;

            ref var vertexBuffer = ref staticMeshVertexBuffers[index];
            ref var vertexBufferLength = ref staticMeshVertexBuffersLength[index];

            if (vertexBuffer == null || vertexBufferLength != targetLength)
            {
                vertexBufferLength = targetLength;

                if (vertexBuffer != null)
                {
                    SDL3.SDL_ReleaseGPUBuffer(device, vertexBuffer);

                    vertexBuffer = null;
                }

                var createInfo = new SDL_GPUBufferCreateInfo()
                {
                    size = (uint)vertexBufferLength,
                    usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX,
                };

                vertexBuffer = SDL3.SDL_CreateGPUBuffer(device, &createInfo);

                if (vertexBuffer == null)
                {
                    return;
                }
            }

            var transferBuffer = GetTransferBuffer(false, vertexBufferLength);

            if (transferBuffer == null)
            {
                SDL3.SDL_ReleaseGPUBuffer(device, vertexBuffer);

                vertexBuffer = null;

                return;
            }

            if(!BeginCopyPass())
            {
                return;
            }

            var mapData = SDL3.SDL_MapGPUTransferBuffer(device, transferBuffer, true);

            unsafe
            {
                var from = new Span<byte>((byte*)buffer.allocator.NativePointer, vertexBufferLength);
                var to = new Span<byte>((void*)mapData, vertexBufferLength);

                from.CopyTo(to);
            }

            SDL3.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

            var location = new SDL_GPUTransferBufferLocation()
            {
                transfer_buffer = transferBuffer,
            };

            var region = new SDL_GPUBufferRegion()
            {
                buffer = vertexBuffer,
                size = (uint)vertexBufferLength,
            };

            SDL3.SDL_UploadToGPUBuffer(copyPass, &location, &region, false);
        }
    }

    public void UpdateStaticMeshIndexBuffer(BufferAttributeSource<int, IndexBuffer> buffer)
    {
        unsafe
        {
            var targetLength = buffer.allocator.buffer.Length * buffer.allocator.elementSize;

            ref var indexBuffer = ref staticMeshIndexBuffer;
            ref var bufferLength = ref staticMeshIndexBufferLength;

            if (indexBuffer == null || bufferLength != targetLength)
            {
                bufferLength = targetLength;

                if (indexBuffer != null)
                {
                    SDL3.SDL_ReleaseGPUBuffer(device, indexBuffer);

                    indexBuffer = null;
                }

                var createInfo = new SDL_GPUBufferCreateInfo()
                {
                    size = (uint)bufferLength,
                    usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX,
                };

                indexBuffer = SDL3.SDL_CreateGPUBuffer(device, &createInfo);

                if (indexBuffer == null)
                {
                    return;
                }
            }

            var transferBuffer = GetTransferBuffer(false, bufferLength);

            if (transferBuffer == null)
            {
                SDL3.SDL_ReleaseGPUBuffer(device, indexBuffer);

                indexBuffer = null;

                return;
            }

            if (!BeginCopyPass())
            {
                return;
            }

            var mapData = SDL3.SDL_MapGPUTransferBuffer(device, transferBuffer, true);

            unsafe
            {
                var from = new Span<byte>((byte*)buffer.allocator.NativePointer, bufferLength);
                var to = new Span<byte>((void*)mapData, bufferLength);

                from.CopyTo(to);
            }

            SDL3.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

            var location = new SDL_GPUTransferBufferLocation()
            {
                transfer_buffer = transferBuffer,
            };

            var region = new SDL_GPUBufferRegion()
            {
                buffer = indexBuffer,
                size = (uint)bufferLength,
            };

            SDL3.SDL_UploadToGPUBuffer(copyPass, &location, &region, false);
        }
    }

    private void UpdateIndirectEntityBuffer()
    {
        unsafe
        {
            var targetSize = indirectEntityIndices.Length * sizeof(uint);

            if (entityTransformIndexBufferLength != targetSize)
            {
                if (entityTransformIndexBuffer != null)
                {
                    SDL3.SDL_ReleaseGPUBuffer(device, entityTransformIndexBuffer);

                    entityTransformIndexBuffer = null;
                }

                var createInfo = new SDL_GPUBufferCreateInfo()
                {
                    size = (uint)targetSize,
                    usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDIRECT |
                        SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ,
                };

                entityTransformIndexBuffer = SDL3.SDL_CreateGPUBuffer(device, &createInfo);

                if (entityTransformIndexBuffer == null)
                {
                    return;
                }

                entityTransformIndexBufferLength = targetSize;
            }

            var transferBuffer = GetTransferBuffer(false, targetSize);

            if (transferBuffer == null)
            {
                SDL3.SDL_ReleaseGPUBuffer(device, entityTransformIndexBuffer);

                entityTransformIndexBuffer = null;

                return;
            }

            if (!BeginCopyPass())
            {
                return;
            }

            var mapData = SDL3.SDL_MapGPUTransferBuffer(device, transferBuffer, true);

            unsafe
            {
                var from = indirectEntityIndices.AsSpan();
                var to = new Span<uint>((void*)mapData, indirectEntityIndices.Length);

                from.CopyTo(to);
            }

            SDL3.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

            var location = new SDL_GPUTransferBufferLocation()
            {
                transfer_buffer = transferBuffer,
            };

            var region = new SDL_GPUBufferRegion()
            {
                buffer = entityTransformIndexBuffer,
                size = (uint)targetSize,
            };

            SDL3.SDL_UploadToGPUBuffer(copyPass, &location, &region, false);
        }
    }

    private void UpdateIndirectCommandBuffer()
    {
        unsafe
        {
            if (RenderSystem.Instance.changedEntityTransformRanges.Count == 0 &&
                !needsIndirectBufferUpdate &&
                indirectCommandBuffer != null &&
                entityTransformIndexBuffer != null)
            {
                return;
            }

            needsIndirectBufferUpdate = false;

            if (renderPass != null)
            {
                FinishPasses();
            }

            var targetSize = indirectCommands.Length * Marshal.SizeOf<SDL_GPUIndexedIndirectDrawCommand>();

            if (indirectCommandBufferLength != targetSize)
            {
                if (indirectCommandBuffer != null)
                {
                    SDL3.SDL_ReleaseGPUBuffer(device, indirectCommandBuffer);

                    indirectCommandBuffer = null;
                }

                var createInfo = new SDL_GPUBufferCreateInfo()
                {
                    size = (uint)targetSize,
                    usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDIRECT,
                };

                indirectCommandBuffer = SDL3.SDL_CreateGPUBuffer(device, &createInfo);

                if (indirectCommandBuffer == null)
                {
                    return;
                }

                indirectCommandBufferLength = targetSize;
            }

            var transferBuffer = GetTransferBuffer(false, targetSize);

            if (transferBuffer == null)
            {
                SDL3.SDL_ReleaseGPUBuffer(device, indirectCommandBuffer);

                indirectCommandBuffer = null;

                return;
            }

            if (!BeginCopyPass())
            {
                return;
            }

            var mapData = SDL3.SDL_MapGPUTransferBuffer(device, transferBuffer, true);

            unsafe
            {
                var from = indirectCommands.AsSpan();
                var to = new Span<SDL_GPUIndexedIndirectDrawCommand>((void*)mapData, indirectCommands.Length);

                from.CopyTo(to);
            }

            SDL3.SDL_UnmapGPUTransferBuffer(device, transferBuffer);

            var location = new SDL_GPUTransferBufferLocation()
            {
                transfer_buffer = transferBuffer,
            };

            var region = new SDL_GPUBufferRegion()
            {
                buffer = indirectCommandBuffer,
                size = (uint)targetSize,
            };

            SDL3.SDL_UploadToGPUBuffer(copyPass, &location, &region, false);

            UpdateIndirectEntityBuffer();
        }
    }
}
