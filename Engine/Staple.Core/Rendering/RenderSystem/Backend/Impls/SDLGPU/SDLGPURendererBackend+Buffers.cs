using SDL3;
using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal partial class SDLGPURendererBackend
{
    internal nint GetTransferBuffer(bool download, int length)
    {
        var key = new TransferBufferCacheKey(download, length);

        if(cachedTransferBuffers.TryGetValue(key, out var buffer))
        {
            return buffer;
        }

        var transferInfo = new SDL.GPUTransferBufferCreateInfo()
        {
            Size = (uint)length,
            Usage = download ? SDL.GPUTransferBufferUsage.Download : SDL.GPUTransferBufferUsage.Upload,
        };

        buffer = SDL.CreateGPUTransferBuffer(device, in transferInfo);

        if (buffer != nint.Zero)
        {
            cachedTransferBuffers.Add(key, buffer);
        }

        return buffer;
    }

    internal void ReleaseBufferResource(BufferResource resource)
    {
        if (!(resource?.used ?? false))
        {
            return;
        }

        resource.transferBuffer = nint.Zero;

        if (resource.buffer != nint.Zero)
        {
            SDL.ReleaseGPUBuffer(device, resource.buffer);

            resource.buffer = nint.Zero;
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

    private void UpdateEntityTransformBuffer()
    {
        var elementCount = RenderSystem.Instance.entityTransforms.Length;

        var targetLength = elementCount * Matrix4x4ByteSize;

        if (entityTransformsBuffer != nint.Zero &&
            entityTransformsBufferLength >= targetLength &&
            RenderSystem.Instance.changedEntityTransformRanges.Count == 0)
        {
            return;
        }

        if (entityTransformsBuffer == nint.Zero || entityTransformsBufferLength != targetLength)
        {
            entityTransformsBufferLength = targetLength;

            if (entityTransformsBuffer != nint.Zero)
            {
                SDL.ReleaseGPUBuffer(device, entityTransformsBuffer);

                entityTransformsBuffer = nint.Zero;
            }

            var createInfo = new SDL.GPUBufferCreateInfo()
            {
                Size = (uint)targetLength,
                Usage = SDL.GPUBufferUsageFlags.Vertex | SDL.GPUBufferUsageFlags.GraphicsStorageRead,
            };

            entityTransformsBuffer = SDL.CreateGPUBuffer(device, in createInfo);

            if (entityTransformsBuffer == nint.Zero)
            {
                return;
            }

            var transferBuffer = GetTransferBuffer(false, targetLength);

            if (transferBuffer == nint.Zero)
            {
                SDL.ReleaseGPUBuffer(device, entityTransformsBuffer);

                entityTransformsBuffer = nint.Zero;

                return;
            }

            if (renderPass != nint.Zero)
            {
                FinishPasses();
            }

            if (copyPass == nint.Zero)
            {
                copyPass = SDL.BeginGPUCopyPass(commandBuffer);
            }

            if (copyPass == nint.Zero)
            {
                return;
            }

            var mapData = SDL.MapGPUTransferBuffer(device, transferBuffer, true);

            unsafe
            {
                var from = RenderSystem.Instance.entityTransforms.AsSpan();
                var to = new Span<Matrix4x4>((void*)mapData, elementCount);

                from.CopyTo(to);
            }

            SDL.UnmapGPUTransferBuffer(device, transferBuffer);

            var location = new SDL.GPUTransferBufferLocation()
            {
                TransferBuffer = transferBuffer,
            };

            var region = new SDL.GPUBufferRegion()
            {
                Buffer = entityTransformsBuffer,
                Size = (uint)targetLength,
            };

            SDL.UploadToGPUBuffer(copyPass, in location, in region, false);
        }
        else
        {
            if (renderPass != nint.Zero)
            {
                FinishPasses();
            }

            if (copyPass == nint.Zero)
            {
                copyPass = SDL.BeginGPUCopyPass(commandBuffer);
            }

            if (copyPass == nint.Zero)
            {
                return;
            }

            foreach (var (start, length) in RenderSystem.Instance.changedEntityTransformRanges)
            {
                targetLength = length * Matrix4x4ByteSize;
                
                var transferBuffer = GetTransferBuffer(false, targetLength);

                if (transferBuffer == nint.Zero)
                {
                    SDL.ReleaseGPUBuffer(device, entityTransformsBuffer);

                    entityTransformsBuffer = nint.Zero;

                    return;
                }

                var mapData = SDL.MapGPUTransferBuffer(device, transferBuffer, true);

                unsafe
                {
                    var from = RenderSystem.Instance.entityTransforms.AsSpan().Slice(start, length);
                    var to = new Span<Matrix4x4>((void*)mapData, length);

                    from.CopyTo(to);
                }

                SDL.UnmapGPUTransferBuffer(device, transferBuffer);

                var location = new SDL.GPUTransferBufferLocation()
                {
                    TransferBuffer = transferBuffer,
                };

                var region = new SDL.GPUBufferRegion()
                {
                    Buffer = entityTransformsBuffer,
                    Offset = (uint)(start * Matrix4x4ByteSize),
                    Size = (uint)targetLength,
                };

                SDL.UploadToGPUBuffer(copyPass, in location, in region, false);
            }
        }
    }

    public void UpdateStaticMeshVertexBuffer<T>(BufferAttributeSource<T, VertexBuffer> buffer) where T : unmanaged
    {
        if (renderPass != nint.Zero)
        {
            FinishPasses();
        }

        var index = buffer.index;
        var targetLength = buffer.allocator.buffer.Length * buffer.allocator.elementSize;

        ref var vertexBuffer = ref staticMeshVertexBuffers[index];
        ref var vertexBufferLength = ref staticMeshVertexBuffersLength[index];

        if (vertexBuffer == nint.Zero || vertexBufferLength != targetLength)
        {
            vertexBufferLength = targetLength;

            if (vertexBuffer != nint.Zero)
            {
                SDL.ReleaseGPUBuffer(device, vertexBuffer);

                vertexBuffer = nint.Zero;
            }

            var createInfo = new SDL.GPUBufferCreateInfo()
            {
                Size = (uint)vertexBufferLength,
                Usage = SDL.GPUBufferUsageFlags.Vertex,
            };

            vertexBuffer = SDL.CreateGPUBuffer(device, in createInfo);

            if (vertexBuffer == nint.Zero)
            {
                return;
            }
        }

        var transferBuffer = GetTransferBuffer(false, vertexBufferLength);

        if (transferBuffer == nint.Zero)
        {
            SDL.ReleaseGPUBuffer(device, vertexBuffer);

            vertexBuffer = nint.Zero;

            return;
        }

        if (renderPass != nint.Zero)
        {
            FinishPasses();
        }

        if (copyPass == nint.Zero)
        {
            copyPass = SDL.BeginGPUCopyPass(commandBuffer);
        }

        if (copyPass == nint.Zero)
        {
            return;
        }

        var mapData = SDL.MapGPUTransferBuffer(device, transferBuffer, true);

        buffer.allocator.EnsurePin();

        unsafe
        {
            var from = new Span<byte>((byte *)buffer.allocator.pinAddress, vertexBufferLength);
            var to = new Span<byte>((void*)mapData, vertexBufferLength);

            from.CopyTo(to);
        }

        SDL.UnmapGPUTransferBuffer(device, transferBuffer);

        var location = new SDL.GPUTransferBufferLocation()
        {
            TransferBuffer = transferBuffer,
        };

        var region = new SDL.GPUBufferRegion()
        {
            Buffer = vertexBuffer,
            Size = (uint)vertexBufferLength,
        };

        SDL.UploadToGPUBuffer(copyPass, in location, in region, false);
    }

    public void UpdateStaticMeshIndexBuffer(BufferAttributeSource<uint, IndexBuffer> buffer)
    {
        if (renderPass != nint.Zero)
        {
            FinishPasses();
        }

        var targetLength = buffer.allocator.buffer.Length * buffer.allocator.elementSize;

        ref var indexBuffer = ref staticMeshIndexBuffer;
        ref var bufferLength = ref staticMeshIndexBufferLength;

        if (indexBuffer == nint.Zero || bufferLength != targetLength)
        {
            bufferLength = targetLength;

            if (indexBuffer != nint.Zero)
            {
                SDL.ReleaseGPUBuffer(device, indexBuffer);

                indexBuffer = nint.Zero;
            }

            var createInfo = new SDL.GPUBufferCreateInfo()
            {
                Size = (uint)bufferLength,
                Usage = SDL.GPUBufferUsageFlags.Index,
            };

            indexBuffer = SDL.CreateGPUBuffer(device, in createInfo);

            if (indexBuffer == nint.Zero)
            {
                return;
            }
        }

        var transferBuffer = GetTransferBuffer(false, bufferLength);

        if (transferBuffer == nint.Zero)
        {
            SDL.ReleaseGPUBuffer(device, indexBuffer);

            indexBuffer = nint.Zero;

            return;
        }

        if (renderPass != nint.Zero)
        {
            FinishPasses();
        }

        if (copyPass == nint.Zero)
        {
            copyPass = SDL.BeginGPUCopyPass(commandBuffer);
        }

        if (copyPass == nint.Zero)
        {
            return;
        }

        var mapData = SDL.MapGPUTransferBuffer(device, transferBuffer, true);

        buffer.allocator.EnsurePin();

        unsafe
        {
            var from = new Span<byte>((byte*)buffer.allocator.pinAddress, bufferLength);
            var to = new Span<byte>((void*)mapData, bufferLength);

            from.CopyTo(to);
        }

        SDL.UnmapGPUTransferBuffer(device, transferBuffer);

        var location = new SDL.GPUTransferBufferLocation()
        {
            TransferBuffer = transferBuffer,
        };

        var region = new SDL.GPUBufferRegion()
        {
            Buffer = indexBuffer,
            Size = (uint)bufferLength,
        };

        SDL.UploadToGPUBuffer(copyPass, in location, in region, false);
    }
}
