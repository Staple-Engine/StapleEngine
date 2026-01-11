using Evergine.Bindings.Vulkan;
using SDL3;
using Standart.Hash.xxHash;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Internal;

internal partial class SDLGPURendererBackend : IRendererBackend
{
    private const uint VulkanVersionMajor = 1;
    private const uint VulkanVersionMinor = 2;
    private const uint VulkanVersionPatch = 0;

    private const string StapleRenderDataUniformName = "StapleRenderData";
    private const string StapleFragmentDataUniformName = "StapleFragmentRenderData";

    private static uint MakeVulkanVersion(uint major, uint minor, uint patch)
    {
        return (major << 22) | (minor << 12) | patch;
    }

    #region Classes
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct StapleRenderData
    {
        public Matrix4x4 world;
        public Matrix4x4 view;
        public Matrix4x4 projection;
        public int instanceOffset;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct StapleFragmentRenderData
    {
        public float time;
    }

    internal class BufferResource
    {
        public nint buffer;
        public nint transferBuffer;

        public RenderBufferFlags flags;

        public int length;

        public bool used = false;
    }

    internal class TextureResource
    {
        public nint texture;
        public nint transferBuffer;
        public TextureFormat format;
        public TextureFlags flags;
        public int width;
        public int height;

        public int length;

        public bool used = false;
    }

    internal class ViewData
    {
        public RenderTarget renderTarget;
        public CameraClearMode clearMode;
        public Color clearColor;
        public Vector4 viewport;

        public StapleRenderData renderData;
        public StapleFragmentRenderData fragmentData;
    }

    private readonly struct TransferBufferCacheKey(bool download, int length) : IEquatable<TransferBufferCacheKey>
    {
        private readonly bool download = download;
        private readonly int length = length;

        public static bool operator==(TransferBufferCacheKey lhs, TransferBufferCacheKey rhs)
        {
            return lhs.download == rhs.download &&
                lhs.length == rhs.length;
        }

        public static bool operator !=(TransferBufferCacheKey lhs, TransferBufferCacheKey rhs)
        {
            return lhs.download != rhs.download ||
                lhs.length != rhs.length;
        }

        public override bool Equals(object obj)
        {
            return obj is TransferBufferCacheKey key && this == key;
        }
        
        public bool Equals(TransferBufferCacheKey other)
        {
            return download == other.download && length == other.length;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(download, length);
        }
    }

    internal class TransientEntry
    {
        public readonly List<byte> vertices = [];

        public readonly List<ushort> indices = [];

        public readonly List<uint> uintIndices = [];

        public nint vertexBuffer;

        public nint indexBuffer;

        public nint uintIndexBuffer;

        public int startVertex;

        public int startIndex;

        public int startIndexUInt;

        public SDLGPURendererBackend backend;

        public void Clear()
        {
            if(vertexBuffer != nint.Zero)
            {
                SDL.ReleaseGPUBuffer(backend.device, vertexBuffer);

                vertexBuffer = nint.Zero;
            }

            if (indexBuffer != nint.Zero)
            {
                SDL.ReleaseGPUBuffer(backend.device, indexBuffer);

                indexBuffer = nint.Zero;
            }

            if (uintIndexBuffer != nint.Zero)
            {
                SDL.ReleaseGPUBuffer(backend.device, uintIndexBuffer);

                uintIndexBuffer = nint.Zero;
            }

            startVertex = startIndex = startIndexUInt = 0;

            vertices.Clear();
            indices.Clear();
            uintIndices.Clear();
        }

        public void CreateBuffers()
        {
            if(vertices.Count == 0)
            {
                return;
            }

            {
                var createInfo = new SDL.GPUBufferCreateInfo()
                {
                    Size = (uint)vertices.Count,
                    Usage = SDL.GPUBufferUsageFlags.Vertex,
                };

                vertexBuffer = SDL.CreateGPUBuffer(backend.device, in createInfo);

                if (vertexBuffer == nint.Zero)
                {
                    return;
                }

                var transferBuffer = backend.GetTransferBuffer(false, vertices.Count);

                if (transferBuffer == nint.Zero)
                {
                    SDL.ReleaseGPUBuffer(backend.device, vertexBuffer);

                    vertexBuffer = nint.Zero;

                    return;
                }

                if (backend.renderPass != nint.Zero)
                {
                    backend.FinishPasses();
                }

                if (backend.copyPass == nint.Zero)
                {
                    backend.copyPass = SDL.BeginGPUCopyPass(backend.commandBuffer);
                }

                if (backend.copyPass == nint.Zero)
                {
                    SDL.ReleaseGPUBuffer(backend.device, vertexBuffer);

                    return;
                }

                var mapData = SDL.MapGPUTransferBuffer(backend.device, transferBuffer, false);

                var from = CollectionsMarshal.AsSpan(vertices);

                unsafe
                {
                    var to = new Span<byte>((void*)mapData, vertices.Count);

                    from.CopyTo(to);
                }

                SDL.UnmapGPUTransferBuffer(backend.device, transferBuffer);

                var location = new SDL.GPUTransferBufferLocation()
                {
                    TransferBuffer = transferBuffer,
                    Offset = 0,
                };

                var region = new SDL.GPUBufferRegion()
                {
                    Buffer = vertexBuffer,
                    Size = (uint)vertices.Count,
                };

                SDL.UploadToGPUBuffer(backend.copyPass, in location, in region, false);
            }

            if(indices.Count > 0)
            {
                var createInfo = new SDL.GPUBufferCreateInfo()
                {
                    Size = (uint)(indices.Count * sizeof(ushort)),
                    Usage = SDL.GPUBufferUsageFlags.Index,
                };

                indexBuffer = SDL.CreateGPUBuffer(backend.device, in createInfo);

                if (indexBuffer == nint.Zero)
                {
                    return;
                }

                var transferBuffer = backend.GetTransferBuffer(false, indices.Count * sizeof(ushort));

                if (transferBuffer == nint.Zero)
                {
                    SDL.ReleaseGPUBuffer(backend.device, vertexBuffer);
                    SDL.ReleaseGPUBuffer(backend.device, indexBuffer);

                    vertexBuffer = nint.Zero;
                    indexBuffer = nint.Zero;

                    return;
                }

                if (backend.renderPass != nint.Zero)
                {
                    backend.FinishPasses();
                }

                if (backend.copyPass == nint.Zero)
                {
                    backend.copyPass = SDL.BeginGPUCopyPass(backend.commandBuffer);
                }

                if (backend.copyPass == nint.Zero)
                {
                    return;
                }

                var mapData = SDL.MapGPUTransferBuffer(backend.device, transferBuffer, false);

                var from = CollectionsMarshal.AsSpan(indices);

                unsafe
                {
                    var to = new Span<ushort>((void*)mapData, indices.Count);

                    from.CopyTo(to);
                }

                SDL.UnmapGPUTransferBuffer(backend.device, transferBuffer);

                var location = new SDL.GPUTransferBufferLocation()
                {
                    TransferBuffer = transferBuffer,
                    Offset = 0,
                };

                var region = new SDL.GPUBufferRegion()
                {
                    Buffer = indexBuffer,
                    Size = (uint)(indices.Count * sizeof(ushort)),
                };

                SDL.UploadToGPUBuffer(backend.copyPass, in location, in region, false);
            }

            if (uintIndices.Count <= 0)
            {
                return;
            }
            
            {
                var createInfo = new SDL.GPUBufferCreateInfo()
                {
                    Size = (uint)(uintIndices.Count * sizeof(uint)),
                    Usage = SDL.GPUBufferUsageFlags.Index,
                };

                uintIndexBuffer = SDL.CreateGPUBuffer(backend.device, in createInfo);

                if (uintIndexBuffer == nint.Zero)
                {
                    return;
                }

                var transferBuffer = backend.GetTransferBuffer(false, uintIndices.Count * sizeof(uint));

                if (transferBuffer == nint.Zero)
                {
                    SDL.ReleaseGPUBuffer(backend.device, vertexBuffer);
                    SDL.ReleaseGPUBuffer(backend.device, uintIndexBuffer);

                    if (indexBuffer != nint.Zero)
                    {
                        SDL.ReleaseGPUBuffer(backend.device, indexBuffer);
                    }

                    vertexBuffer = nint.Zero;
                    indexBuffer = nint.Zero;
                    uintIndexBuffer = nint.Zero;

                    return;
                }

                if (backend.renderPass != nint.Zero)
                {
                    backend.FinishPasses();
                }

                if (backend.copyPass == nint.Zero)
                {
                    backend.copyPass = SDL.BeginGPUCopyPass(backend.commandBuffer);
                }

                if (backend.copyPass == nint.Zero)
                {
                    return;
                }

                var mapData = SDL.MapGPUTransferBuffer(backend.device, transferBuffer, false);

                var from = CollectionsMarshal.AsSpan(uintIndices);

                unsafe
                {
                    var to = new Span<uint>((void*)mapData, uintIndices.Count);

                    from.CopyTo(to);
                }

                SDL.UnmapGPUTransferBuffer(backend.device, transferBuffer);

                var location = new SDL.GPUTransferBufferLocation()
                {
                    TransferBuffer = transferBuffer,
                    Offset = 0,
                };

                var region = new SDL.GPUBufferRegion()
                {
                    Buffer = uintIndexBuffer,
                    Size = (uint)(uintIndices.Count * sizeof(uint)),
                };

                SDL.UploadToGPUBuffer(backend.copyPass, in location, in region, false);
            }
        }
    }

    internal class MemoryAllocator
    {
        public byte[] buffer = new byte[1024];

        private GCHandle pinHandle;

        private nint pinAddress;

        internal int position;

        public void Allocate(int size)
        {
            var targetSize = position + size;

            if (targetSize >= buffer.Length)
            {
                var newSize = buffer.Length * 2;

                while(newSize < targetSize)
                {
                    newSize *= 2;
                }

                newSize *= 2;

                Array.Resize(ref buffer, newSize);

                Repin();
            }

            position += size;
        }

        private void Repin()
        {
            if (pinHandle.IsAllocated)
            {
                pinHandle.Free();
            }

            pinHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            pinAddress = pinHandle.AddrOfPinnedObject();
        }

        public void EnsurePin()
        {
            if(pinHandle.IsAllocated)
            {
                return;
            }

            pinHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            pinAddress = pinHandle.AddrOfPinnedObject();
        }

        public void Clear()
        {
            position = 0;
        }

        public nint Get(int position)
        {
            if (pinAddress == nint.Zero)
            {
                throw new InvalidOperationException("Memory Allocator was not pinned, ensure you call EnsurePin() before getting an address!");
            }

            return pinAddress + position;
        }
    }
    #endregion

    #region Fields
    internal Vector2Int renderSize;

    internal nint device;
    internal nint commandBuffer;
    internal nint renderPass;
    internal nint copyPass;

    internal nint swapchainTexture;
    internal int swapchainWidth;
    internal int swapchainHeight;
    internal ITexture depthTexture;

    internal readonly ViewData viewData = new();
    internal readonly MemoryAllocator frameAllocator = new();

    private SDL3RenderWindow window;
    private readonly Dictionary<int, nint> graphicsPipelines = [];
    private readonly Dictionary<TextureFlags, nint> textureSamplers = [];
    private bool needsDepthTextureUpdate;
    private readonly List<IRenderCommand> commands = [];
    private readonly List<(SDLGPUTexture, Action<byte[]>)> readTextureQueue = [];

    private readonly BufferResource[] vertexBuffers = new BufferResource[ushort.MaxValue - 1];
    private readonly BufferResource[] indexBuffers = new BufferResource[ushort.MaxValue - 1];
    private readonly Dictionary<VertexLayout, TransientEntry> transientBuffers = [];
    private readonly TextureResource[] textures = new TextureResource[ushort.MaxValue - 1];
    private readonly List<SDLGPUShaderProgram> shaders = [];
    private readonly Dictionary<TransferBufferCacheKey, nint> cachedTransferBuffers = [];
    private readonly ulong[] lastVertexShaderUniformHashes = new ulong[20];
    private readonly ulong[] lastFragmentShaderUniformHashes = new ulong[20];
    private RenderTarget currentRenderTarget;

    internal static nint[] staticMeshVertexBuffers = new nint[18];
    internal static nint staticMeshIndexBuffer = nint.Zero;
    internal static int[] staticMeshVertexBuffersLength = new int[18];
    internal static int[] staticMeshVertexBuffersElementSize = new int[18];
    internal static int staticMeshIndexBufferLength = 0;
    internal static nint entityTransformsBuffer = nint.Zero;
    internal static int entityTransformsBufferLength = 0;

    private bool iteratingCommands;
    private int commandIndex;
    #endregion

    #region Command Support Fields
    internal static nint lastGraphicsPipeline;
    internal static nint lastQueuedGraphicsPipeline;
    internal static nint lastVertexBuffer;
    internal static nint lastIndexBuffer;
    internal static nint[] singleBuffer = new nint[1];
    #endregion

    public bool SupportsTripleBuffering => SDL.WindowSupportsGPUPresentMode(device, window.window,
        SDL.GPUPresentMode.Mailbox);

    public bool SupportsHDRColorSpace => SDL.WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL.GPUSwapchainComposition.HDR10ST2084) ||
        SDL.WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL.GPUSwapchainComposition.HDRExtendedLinear);

    public bool SupportsLinearColorSpace => SDL.WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL.GPUSwapchainComposition.SDRLinear);

    public TextureFormat? DepthStencilFormat
    {
        get
        {
            if(SupportsTextureFormat(TextureFormat.D32S8, TextureFlags.DepthStencilTarget))
            {
                return TextureFormat.D32S8;
            }

            if(SupportsTextureFormat(TextureFormat.D24S8, TextureFlags.DepthStencilTarget))
            {
                return TextureFormat.D24S8;
            }

            if (SupportsTextureFormat(TextureFormat.D24, TextureFlags.DepthStencilTarget))
            {
                return TextureFormat.D24;
            }

            if (SupportsTextureFormat(TextureFormat.D16, TextureFlags.DepthStencilTarget))
            {
                return TextureFormat.D16;
            }

            return null;
        }
    }

    public BufferAttributeContainer StaticMeshData { get; } = new();

    public bool Initialize(RendererType renderer, bool debug, IRenderWindow window, RenderModeFlags renderFlags)
    {
        if(window is not SDL3RenderWindow w)
        {
            return false;
        }

#if DEBUG
        SDL.SetLogPriority(SDL.LogCategory.GPU, SDL.LogPriority.Verbose);
#endif

        this.window = w;

        var version = SDL.GetVersion();

        Log.Debug($"SDL version {SDL.VersionNumMajor(version)}.{SDL.VersionNumMinor(version)}.{SDL.VersionNumMicro(version)}");

        var props = SDL.CreateProperties();

        SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateDebugModeBoolean, debug);

        unsafe
        {
            var createOptions = new SDL.GPUVulkanOptions()
            {
                VulkanApiVersion = MakeVulkanVersion(VulkanVersionMajor, VulkanVersionMinor, VulkanVersionPatch),
            };

            var deviceExtensions = new[]
            {
                "VK_KHR_shader_draw_parameters",
            };

            createOptions.DeviceExtensionCount = (uint)deviceExtensions.Length;

            createOptions.DeviceExtensionNames = SDL.StringArrayToPointer(deviceExtensions);

            var drawParametersStruct = new VkPhysicalDeviceShaderDrawParametersFeatures()
            {
                sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_SHADER_DRAW_PARAMETERS_FEATURES,
                shaderDrawParameters = true,
            };

            void* drawPtr = &drawParametersStruct;

            createOptions.FeatureList = (nint)drawPtr;

            var physicalDeviceFeatures = new VkPhysicalDeviceFeatures()
            {
                robustBufferAccess = true,
            };

            void *physicalDevicePtr = &physicalDeviceFeatures;

            createOptions.Vulkan10PhysicalDeviceFeatures = (nint)physicalDevicePtr;

            switch (renderer)
            {
                case RendererType.Vulkan:

                    SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersSPIRVBoolean, true);

                    void *ptr = &createOptions;

                    SDL.SetPointerProperty(props, SDL.Props.GPUDeviceCreateVulkanOptionsPointer, (nint)ptr);

                    break;

                case RendererType.Direct3D12:

                    SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersDXBCBoolean, true);
                    SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersDXILBoolean, true);

                    break;

                case RendererType.Metal:

                    SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersMSLBoolean, true);
                    SDL.SetBooleanProperty(props, SDL.Props.GPUDeviceCreateShadersMetalLibBoolean, true);

                    break;
            }

            SDL.SetStringProperty(props, SDL.Props.GPUDeviceCreateNameString, renderer switch
            {
                RendererType.Metal => "metal",
                RendererType.Direct3D12 => "direct3d12",
                _ => "vulkan",
            });

            device = SDL.CreateGPUDeviceWithProperties(props);
        }

        SDL.DestroyProperties(props);

        if (device == nint.Zero)
        {
            return false;
        }

        if(!DepthStencilFormat.HasValue || !SDL.ClaimWindowForGPUDevice(device, w.window))
        {
            SDL.DestroyGPUDevice(device);

            device = nint.Zero;

            return false;
        }

        UpdateRenderMode(renderFlags);

        for(var i = 0; i < staticMeshVertexBuffersElementSize.Length; i++)
        {
            staticMeshVertexBuffersElementSize[i] = BufferAttributeContainer.BufferElementSize(i);
        }

        return true;
    }

    public void UpdateRenderMode(RenderModeFlags flags)
    {
        if (device == nint.Zero ||
            window == null ||
            window.window == nint.Zero)
        {
            return;
        }

        var swapchainComposition = SDL.GPUSwapchainComposition.SDR;
        var presentMode = SDL.GPUPresentMode.Immediate;

        if(flags.HasFlag(RenderModeFlags.Vsync))
        {
            presentMode = SDL.GPUPresentMode.VSync;
        }

        if(flags.HasFlag(RenderModeFlags.TripleBuffering) && SupportsTripleBuffering)
        {
            presentMode = SDL.GPUPresentMode.Mailbox;
        }

        if(flags.HasFlag(RenderModeFlags.HDR10) && SupportsHDRColorSpace)
        {
            if(flags.HasFlag(RenderModeFlags.sRGB))
            {
                swapchainComposition = SDL.WindowSupportsGPUSwapchainComposition(device, window.window,
                    SDL.GPUSwapchainComposition.HDRExtendedLinear) ?
                    SDL.GPUSwapchainComposition.HDRExtendedLinear :

                    SDL.WindowSupportsGPUSwapchainComposition(device, window.window,
                    SDL.GPUSwapchainComposition.SDRLinear) ?
                    SDL.GPUSwapchainComposition.SDRLinear :
                    SDL.GPUSwapchainComposition.SDR;
            }
            else
            {
                swapchainComposition =
                    SDL.WindowSupportsGPUSwapchainComposition(device, window.window,
                    SDL.GPUSwapchainComposition.HDR10ST2084) ?
                    SDL.GPUSwapchainComposition.HDR10ST2084 :
                    SDL.GPUSwapchainComposition.SDR;
            }
        }
        else
        {
            if (flags.HasFlag(RenderModeFlags.sRGB) && SupportsLinearColorSpace)
            {
                swapchainComposition = SDL.GPUSwapchainComposition.SDRLinear;
            }
        }

        SDL.SetGPUSwapchainParameters(device, window.window, swapchainComposition, presentMode);

        SDL.GetWindowSizeInPixels(window.window, out var w, out var h);

        renderSize.X = w;
        renderSize.Y = h;

        needsDepthTextureUpdate = true;
    }

    public void UpdateViewport(int width, int height)
    {
        renderSize.X = width;
        renderSize.Y = height;
    }

    public void Destroy()
    {
        if (device == nint.Zero)
        {
            return;
        }
        
        foreach(var pair in graphicsPipelines)
        {
            SDL.ReleaseGPUGraphicsPipeline(device, pair.Value);
        }

        graphicsPipelines.Clear();

        foreach(var pair in textureSamplers)
        {
            SDL.ReleaseGPUSampler(device, pair.Value);
        }

        textureSamplers.Clear();

        foreach(var resource in vertexBuffers)
        {
            ReleaseBufferResource(resource);
        }

        foreach (var resource in indexBuffers)
        {
            ReleaseBufferResource(resource);
        }

        foreach(var resource in textures)
        {
            ReleaseTextureResource(resource);
        }

        for(var i = 0; i < staticMeshVertexBuffers.Length; i++)
        {
            ref var buffer = ref staticMeshVertexBuffers[i];

            if (buffer == nint.Zero)
            {
                continue;
            }
            
            SDL.ReleaseGPUBuffer(device, buffer);

            buffer = nint.Zero;
        }

        if(staticMeshIndexBuffer != nint.Zero)
        {
            SDL.ReleaseGPUBuffer(device, staticMeshIndexBuffer);

            staticMeshIndexBuffer = nint.Zero;
        }

        depthTexture?.Destroy();

        depthTexture = null;

        needsDepthTextureUpdate = true;

        SDL.ReleaseWindowFromGPUDevice(device, window.window);

        SDL.DestroyGPUDevice(device);

        device = nint.Zero;
    }

    private void AddCommand(IRenderCommand command)
    {
        //If we're iterating, add the new command in front of the current one
        if(iteratingCommands)
        {
            commands.Insert(commandIndex + 1, command);
        }
        else
        {
            commands.Add(command);
        }
    }

    public void BeginFrame()
    {
        if(window.window == nint.Zero)
        {
            return;
        }

        frameAllocator.Clear();
    }

    public void EndFrame()
    {
        commandBuffer = SDL.AcquireGPUCommandBuffer(device);

        if (commandBuffer == nint.Zero)
        {
            return;
        }

        if (!SDL.WaitAndAcquireGPUSwapchainTexture(commandBuffer, window.window, out swapchainTexture,
            out var w, out var h))
        {
            SDL.CancelGPUCommandBuffer(commandBuffer);

            commandBuffer = nint.Zero;

            return;
        }

        swapchainWidth = (int)w;
        swapchainHeight = (int)h;

        UpdateDepthTextureIfNeeded(false);

        frameAllocator.EnsurePin();

        UpdateEntityTransformBuffer();

        StaticMeshData.Update();

        foreach (var pair in transientBuffers)
        {
            pair.Value.CreateBuffers();
        }

        iteratingCommands = true;

        for(var i = 0; i < commands.Count; i++)
        {
            commandIndex = i;

            commands[i].Update(this);
        }

        iteratingCommands = false;

        FinishPasses();

        commands.Clear();
        frameAllocator.Clear();

        lastGraphicsPipeline = nint.Zero;
        lastQueuedGraphicsPipeline = nint.Zero;
        lastVertexBuffer = nint.Zero;
        lastIndexBuffer = nint.Zero;

        Array.Clear(lastVertexShaderUniformHashes);
        Array.Clear(lastFragmentShaderUniformHashes);

        foreach (var pair in transientBuffers)
        {
            pair.Value.Clear();
        }

        foreach(var shader in shaders)
        {
            shader.ClearUniformHashes();
        }

        var fences = new nint[1];

        fences[0] = SDL.SubmitGPUCommandBufferAndAcquireFence(commandBuffer);

        if (!SDL.WaitForGPUFences(device, true, fences, (uint)fences.Length))
        {
            Log.Error($"[SDL GPU] Failed to wait for GPU Fences: {SDL.GetError()}");
        }

        SDL.ReleaseGPUFence(device, fences[0]);

        for(var i = readTextureQueue.Count - 1; i >= 0; i--)
        {
            var item = readTextureQueue[i];

            readTextureQueue.RemoveAt(i);

            if (item.Item1 == null ||
                item.Item1.Disposed ||
                !TryGetTexture(item.Item1.handle, out var resource) ||
                !resource.used ||
                resource.transferBuffer == nint.Zero)
            {
                continue;
            }

            unsafe
            {
                var buffer = new byte[resource.length];

                var map = SDL.MapGPUTransferBuffer(device, resource.transferBuffer, false);

                var from = new Span<byte>((void *)map, buffer.Length);
                var to = new Span<byte>(buffer);

                from.CopyTo(to);

                SDL.UnmapGPUTransferBuffer(device, resource.transferBuffer);

                resource.transferBuffer = nint.Zero;

                item.Item2?.Invoke(buffer);
            }

            commandBuffer = nint.Zero;
        }

        if (currentRenderTarget == null)
        {
            return;
        }
        
        currentRenderTarget = null;

        Screen.Width = window.Size.X;
        Screen.Height = window.Size.Y;
    }

    private void CheckQueuedPipeline(nint newPipeline)
    {
        if (lastQueuedGraphicsPipeline == newPipeline)
        {
            return;
        }
        
        lastQueuedGraphicsPipeline = newPipeline;

        Array.Clear(lastVertexShaderUniformHashes);
        Array.Clear(lastFragmentShaderUniformHashes);
    }

    private static ulong UniformDataHash(Span<byte> data)
    {
        return xxHash64.ComputeHash(data, data.Length);
    }

    private bool ShouldPushVertexUniform(int binding, Span<byte> data)
    {
        //TODO: Figure this out
        /*
        if(binding < 0 || binding >= lastVertexShaderUniformHashes.Length)
        {
            return false;
        }

        var hash = UniformDataHash(data);

        if (lastVertexShaderUniformHashes[binding] != hash)
        {
            lastVertexShaderUniformHashes[binding] = hash;

            return true;
        }

        return false;
        */

        return true;
    }

    private bool ShouldPushFragmentUniform(int binding, Span<byte> data)
    {
        //TODO: Figure this out
        /*
        if (binding < 0 || binding >= lastFragmentShaderUniformHashes.Length)
        {
            return false;
        }

        var hash = UniformDataHash(data);

        if (lastFragmentShaderUniformHashes[binding] != hash)
        {
            lastFragmentShaderUniformHashes[binding] = hash;

            return true;
        }

        return false;
        */

        return true;
    }

    internal void UpdateDepthTextureIfNeeded(bool force)
    {
        if((!needsDepthTextureUpdate && !force) ||
            swapchainWidth == 0 ||
            swapchainHeight == 0 ||
            !DepthStencilFormat.HasValue)
        {
            return;
        }

        needsDepthTextureUpdate = false;

        if(depthTexture is SDLGPUTexture texture &&
            TryGetTexture(texture.handle, out var resource))
        {
            ReleaseTextureResource(resource);

            texture.handle = ResourceHandle<Texture>.Invalid;
        }

        depthTexture = CreateEmptyTexture(swapchainWidth, swapchainHeight, DepthStencilFormat.Value, TextureFlags.DepthStencilTarget);
    }

    public void FinishPasses()
    {
        if(copyPass != nint.Zero)
        {
            SDL.EndGPUCopyPass(copyPass);

            copyPass = nint.Zero;
        }

        if (renderPass == nint.Zero)
        {
            return;
        }
        
        SDL.EndGPURenderPass(renderPass);

        renderPass = nint.Zero;
        lastGraphicsPipeline = nint.Zero;
        lastQueuedGraphicsPipeline = nint.Zero;
        lastVertexBuffer = nint.Zero;
        lastIndexBuffer = nint.Zero;
    }

    internal void ResumeRenderPass()
    {
        FinishPasses();

        if(commandBuffer == nint.Zero)
        {
            return;
        }

        var texture = nint.Zero;
        var width = 0;
        var height = 0;

        SDLGPUTexture depthTexture;

        if (viewData.renderTarget == null)
        {
            texture = swapchainTexture;
            width = swapchainWidth;
            height = swapchainHeight;

            depthTexture = this.depthTexture as SDLGPUTexture;

            if (depthTexture == null)
            {
                UpdateDepthTextureIfNeeded(true);

                depthTexture = this.depthTexture as SDLGPUTexture;
            }
        }
        else
        {
            if (viewData.renderTarget.ColorTextureCount > 0 &&
                viewData.renderTarget.colorTextures[0].impl is SDLGPUTexture t &&
                TryGetTexture(t.handle, out var textureResource))
            {
                texture = textureResource.texture;
            }

            width = viewData.renderTarget.width;
            height = viewData.renderTarget.height;

            depthTexture = viewData.renderTarget.DepthTexture?.impl as SDLGPUTexture;
        }

        if (texture == nint.Zero ||
            (depthTexture?.Disposed ?? true) ||
            !TryGetTexture(depthTexture.handle, out var depthTextureResource))
        {
            return;
        }

        var colorTarget = new SDL.GPUColorTargetInfo()
        {
            LoadOp = SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store,
            Texture = texture,
        };

        var depthTarget = new SDL.GPUDepthStencilTargetInfo()
        {
            ClearDepth = 1,
            LoadOp = SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store,
            Texture = depthTextureResource.texture,
            StencilLoadOp = SDL.GPULoadOp.Load,
            StencilStoreOp = SDL.GPUStoreOp.Store,
        };

        renderPass = SDL.BeginGPURenderPass(commandBuffer, [colorTarget], 1, in depthTarget);

        if (renderPass == nint.Zero)
        {
            return;
        }

        var viewportData = new SDL.GPUViewport()
        {
            X = (int)(viewData.viewport.X * width),
            Y = (int)(viewData.viewport.Y * height),
            W = (int)(viewData.viewport.Z * width),
            H = (int)(viewData.viewport.W * height),
            MinDepth = 0,
            MaxDepth = 1,
        };

        SDL.SetGPUViewport(renderPass, in viewportData);
    }

    public void BeginRenderPass(RenderTarget target, CameraClearMode clear, Color clearColor, Vector4 viewport,
        in Matrix4x4 view, in Matrix4x4 projection)
    {
        viewData.renderTarget = currentRenderTarget = target;
        viewData.clearMode = clear;
        viewData.clearColor = clearColor;
        viewData.viewport = viewport;
        viewData.renderData.view = view;
        viewData.renderData.projection = projection;

        if(target != null)
        {
            Screen.Width = target.width;
            Screen.Height = target.height;
        }
        else
        {
            Screen.Width = window.Size.X;
            Screen.Height = window.Size.Y;
        }

        AddCommand(new SDLGPUBeginRenderPassCommand(target, clear, clearColor, viewport, view, projection));
    }

    public IShaderProgram CreateShaderVertexFragment(byte[] vertex, byte[] fragment,
        VertexFragmentShaderMetrics vertexMetrics, VertexFragmentShaderMetrics fragmentMetrics,
        ShaderUniformContainer vertexUniforms, ShaderUniformContainer fragmentUniforms)
    {
        unsafe
        {
            var vertexEntryPointBytes = Encoding.UTF8.GetBytes("main");
            var fragmentEntryPointBytes = Encoding.UTF8.GetBytes("main");

            var vertexShader = nint.Zero;
            var fragmentShader = nint.Zero;

            fixed (byte* entry = vertexEntryPointBytes)
            {
                fixed (byte* v = vertex)
                {
                    var info = new SDL.GPUShaderCreateInfo()
                    {
                        Code = (nint)v,
                        CodeSize = (uint)vertex.Length,
                        Entrypoint = (nint)entry,
                        Format = RenderWindow.CurrentRenderer switch
                        {
                            RendererType.Direct3D12 => SDL.GPUShaderFormat.DXIL,
                            RendererType.Metal => SDL.GPUShaderFormat.MSL,
                            _ => SDL.GPUShaderFormat.SPIRV,
                        },
                        Stage = SDL.GPUShaderStage.Vertex,
                        NumSamplers = (uint)vertexMetrics.samplerCount,
                        NumStorageBuffers = (uint)vertexMetrics.storageBufferCount,
                        NumStorageTextures = (uint)vertexMetrics.storageTextureCount,
                        NumUniformBuffers = (uint)vertexMetrics.uniformBufferCount,
                    };

                    vertexShader = SDL.CreateGPUShader(device, in info);

                    if (vertexShader == nint.Zero)
                    {
                        return null;
                    }
                }
            }

            fixed (byte* entry = fragmentEntryPointBytes)
            {
                fixed (byte* f = fragment)
                {
                    var info = new SDL.GPUShaderCreateInfo()
                    {
                        Code = (nint)f,
                        CodeSize = (uint)fragment.Length,
                        Entrypoint = (nint)entry,
                        Format = RenderWindow.CurrentRenderer switch
                        {
                            RendererType.Direct3D12 => SDL.GPUShaderFormat.DXIL,
                            RendererType.Metal => SDL.GPUShaderFormat.MSL,
                            _ => SDL.GPUShaderFormat.SPIRV,
                        },
                        Stage = SDL.GPUShaderStage.Fragment,
                        NumSamplers = (uint)fragmentMetrics.samplerCount,
                        NumStorageBuffers = (uint)fragmentMetrics.storageBufferCount,
                        NumStorageTextures = (uint)fragmentMetrics.storageTextureCount,
                        NumUniformBuffers = (uint)fragmentMetrics.uniformBufferCount,
                    };

                    fragmentShader = SDL.CreateGPUShader(device, in info);

                    if (fragmentShader == nint.Zero)
                    {
                        SDL.ReleaseGPUShader(device, vertexShader);

                        return null;
                    }
                }
            }

            var shader = new SDLGPUShaderProgram(device, vertexShader, fragmentShader, vertexUniforms, fragmentUniforms);

            shaders.Add(shader);

            return shader;
        }
    }

    public IShaderProgram CreateShaderCompute(byte[] compute, ComputeShaderMetrics metrics, ShaderUniformContainer uniforms)
    {
        unsafe
        {
            var entryPointBytes = Encoding.UTF8.GetBytes("main");

            var computeShader = nint.Zero;

            fixed (byte* e = entryPointBytes)
            {
                fixed (byte* c = compute)
                {
                    var info = new SDL.GPUComputePipelineCreateInfo()
                    {
                        Code = (nint)c,
                        CodeSize = (uint)compute.Length,
                        Entrypoint = (nint)e,
                        Format = RenderWindow.CurrentRenderer switch
                        {
                            RendererType.Direct3D12 => SDL.GPUShaderFormat.DXIL,
                            RendererType.Metal => SDL.GPUShaderFormat.MSL,
                            _ => SDL.GPUShaderFormat.SPIRV,
                        },
                        NumSamplers = (uint)metrics.samplerCount,
                        NumUniformBuffers = (uint)metrics.uniformBufferCount,
                        NumReadonlyStorageBuffers = (uint)metrics.readOnlyStorageBufferCount,
                        NumReadonlyStorageTextures = (uint)metrics.readOnlyStorageTextureCount,
                        NumReadwriteStorageBuffers = (uint)metrics.readWriteStorageBufferCount,
                        NumReadwriteStorageTextures = (uint)metrics.readWriteStorageTextureCount,
                        ThreadcountX = (uint)metrics.threadCountX,
                        ThreadcountY = (uint)metrics.threadCountY,
                        ThreadcountZ = (uint)metrics.threadCountZ,
                    };

                    computeShader = SDL.CreateGPUComputePipeline(device, in info);

                    if (computeShader == nint.Zero)
                    {
                        return null;
                    }
                }
            }

            var shader = new SDLGPUShaderProgram(device, computeShader, uniforms);

            shaders.Add(shader);

            return shader;
        }
    }

    public bool SupportsTextureFormat(TextureFormat format, TextureFlags flags)
    {
        if(!TryGetTextureFormat(format, flags, out var f))
        {
            return false;
        }

        return SDL.GPUTextureSupportsFormat(device, f, GetTextureType(flags),
            GetTextureUsage(flags));
    }

    private bool TryGetRenderPipeline(RenderState state, SDLGPUVertexLayout vertexLayout, out nint pipeline)
    {
        if (state.shader == null ||
            state.shaderInstance?.program is not SDLGPUShaderProgram { Type: ShaderType.VertexFragment } shader)
        {
            pipeline = nint.Zero;

            return false;
        }

        var hash = state.StateKey;

        if (!graphicsPipelines.TryGetValue(hash, out pipeline))
        {
            unsafe
            {
                var vertexSamplerCount = state.vertexTextures?.Length ?? 0;
                var fragmentSamplerCount = state.fragmentTextures?.Length ?? 0;

                if (vertexSamplerCount < state.shaderInstance.vertexTextureBindings.Count ||
                    fragmentSamplerCount < state.shaderInstance.fragmentTextureBindings.Count)
                {
                    pipeline = nint.Zero;

                    return false;
                }

                List<VertexAttribute> GetMissingAttributes()
                {
                    var outValue = new List<VertexAttribute>();

                    foreach (var attribute in state.shaderInstance.attributes)
                    {
                        if (vertexLayout.vertexAttributes.IndexOf(attribute) < 0)
                        {
                            outValue.Add(attribute);
                        }
                    }

                    return outValue;
                }

                if (vertexLayout.attributes.Length < state.shaderInstance.attributes.Length)
                {
                    var message = $"Failed to render: Vertex Layout is missing attributes ({vertexLayout.attributes.Length} attributes vs " +
                        $"required {state.shaderInstance.attributes.Length} shader attributes)";

                    var attributes = GetMissingAttributes();

                    if (attributes.Count > 0)
                    {
                        Log.Error($"{message}\nAdditionally, the following vertex attributes are missing: " +
                            string.Join('\n', attributes.Select(x => x.ToString().ToUpperInvariant())));
                    }
                    else
                    {
                        Log.Error(message);
                    }

                    pipeline = nint.Zero;

                    return false;
                }

                var depthStencilFormat = DepthStencilFormat;

                if (!depthStencilFormat.HasValue ||
                    !TryGetTextureFormat(depthStencilFormat.Value, TextureFlags.DepthStencilTarget,
                        out var sdlDepthFormat))
                {
                    sdlDepthFormat = SDL.GPUTextureFormat.D24Unorm;
                }

                var shaderAttributes = new List<SDL.GPUVertexAttribute>();

                for (var i = 0; i < state.shaderInstance.attributes.Length; i++)
                {
                    var attributeIndex = vertexLayout.vertexAttributes.IndexOf(state.shaderInstance.attributes[i]);

                    if (attributeIndex < 0)
                    {
                        var attributes = GetMissingAttributes();

                        if (attributes.Count > 0)
                        {
                            Log.Error("Failed to render: The following vertex attributes are missing: " +
                                string.Join('\n', attributes.Select(x => x.ToString().ToUpperInvariant())));
                        }

                        pipeline = nint.Zero;

                        return false;
                    }

                    var attribute = vertexLayout.attributes[attributeIndex];

                    attributeIndex = state.shaderInstance.attributes.IndexOf(vertexLayout.vertexAttributes[attributeIndex]);

                    shaderAttributes.Add(new()
                    {
                        BufferSlot = 0,
                        Format = attribute.Format,
                        Offset = attribute.Offset,
                        Location = (uint)attributeIndex,
                    });
                }

                var attributesSpan = CollectionsMarshal.AsSpan(shaderAttributes);

                fixed (SDL.GPUVertexAttribute* attributes = attributesSpan)
                {
                    var vertexDescription = new SDL.GPUVertexBufferDescription()
                    {
                        Pitch = (uint)vertexLayout.Stride,
                        Slot = 0,
                        InputRate = SDL.GPUVertexInputRate.Vertex,
                    };

                    var sourceBlend = state.sourceBlend switch
                    {
                        BlendMode.DstAlpha => SDL.GPUBlendFactor.DstAlpha,
                        BlendMode.DstColor => SDL.GPUBlendFactor.DstColor,
                        BlendMode.One => SDL.GPUBlendFactor.One,
                        BlendMode.OneMinusDstAlpha => SDL.GPUBlendFactor.OneMinusDstAlpha,
                        BlendMode.OneMinusDstColor => SDL.GPUBlendFactor.OneMinusDstColor,
                        BlendMode.OneMinusSrcAlpha => SDL.GPUBlendFactor.OneMinusSrcAlpha,
                        BlendMode.OneMinusSrcColor => SDL.GPUBlendFactor.OneMinusSrcColor,
                        BlendMode.SrcAlpha => SDL.GPUBlendFactor.SrcAlpha,
                        BlendMode.SrcAlphaSat => SDL.GPUBlendFactor.SrcAlphaSaturate,
                        BlendMode.SrcColor => SDL.GPUBlendFactor.SrcColor,
                        BlendMode.Zero => SDL.GPUBlendFactor.Zero,
                        BlendMode.Off => SDL.GPUBlendFactor.Invalid,
                        _ => throw new ArgumentOutOfRangeException(nameof(state.sourceBlend), "Invalid blend mode"),
                    };

                    var destinationBlend = state.destinationBlend switch
                    {
                        BlendMode.DstAlpha => SDL.GPUBlendFactor.DstAlpha,
                        BlendMode.DstColor => SDL.GPUBlendFactor.DstColor,
                        BlendMode.One => SDL.GPUBlendFactor.One,
                        BlendMode.OneMinusDstAlpha => SDL.GPUBlendFactor.OneMinusDstAlpha,
                        BlendMode.OneMinusDstColor => SDL.GPUBlendFactor.OneMinusDstColor,
                        BlendMode.OneMinusSrcAlpha => SDL.GPUBlendFactor.OneMinusSrcAlpha,
                        BlendMode.OneMinusSrcColor => SDL.GPUBlendFactor.OneMinusSrcColor,
                        BlendMode.SrcAlpha => SDL.GPUBlendFactor.SrcAlpha,
                        BlendMode.SrcAlphaSat => SDL.GPUBlendFactor.SrcAlphaSaturate,
                        BlendMode.SrcColor => SDL.GPUBlendFactor.SrcColor,
                        BlendMode.Zero => SDL.GPUBlendFactor.Zero,
                        BlendMode.Off => SDL.GPUBlendFactor.Invalid,
                        _ => throw new ArgumentOutOfRangeException(nameof(state.destinationBlend), "Invalid blend mode"),
                    };

                    var colorTargetDescriptions = new List<SDL.GPUColorTargetDescription>();

                    if(state.renderTarget == null || state.renderTarget.Disposed)
                    {
                        var colorTargetDescription = new SDL.GPUColorTargetDescription()
                        {
                            Format = SDL.GetGPUSwapchainTextureFormat(device, window.window),
                            BlendState = new()
                            {
                                EnableBlend = (byte)(state.sourceBlend != BlendMode.Off && state.destinationBlend != BlendMode.Off ? 1 : 0),
                                ColorBlendOp = SDL.GPUBlendOp.Add,
                                AlphaBlendOp = SDL.GPUBlendOp.Add,
                                SrcColorBlendfactor = sourceBlend,
                                SrcAlphaBlendfactor = sourceBlend,
                                DstColorBlendfactor = destinationBlend,
                                DstAlphaBlendfactor = destinationBlend,
                            }
                        };

                        colorTargetDescriptions.Add(colorTargetDescription);
                    }
                    else
                    {
                        foreach(var texture in state.renderTarget.colorTextures)
                        {
                            if(texture.Disposed ||
                                !TryGetTextureFormat(texture.impl.Format, state.renderTarget.flags | TextureFlags.ColorTarget, out var textureFormat))
                            {
                                continue;
                            }
                            
                            var colorTargetDescription = new SDL.GPUColorTargetDescription()
                            {
                                Format = textureFormat,
                                BlendState = new()
                                {
                                    EnableBlend = (byte)(state.sourceBlend != BlendMode.Off && state.destinationBlend != BlendMode.Off ? 1 : 0),
                                    ColorBlendOp = SDL.GPUBlendOp.Add,
                                    AlphaBlendOp = SDL.GPUBlendOp.Add,
                                    SrcColorBlendfactor = sourceBlend,
                                    SrcAlphaBlendfactor = sourceBlend,
                                    DstColorBlendfactor = destinationBlend,
                                    DstAlphaBlendfactor = destinationBlend,
                                }
                            };

                            colorTargetDescriptions.Add(colorTargetDescription);
                        }

                        if(state.renderTarget.DepthTexture is { Disposed: false } &&
                            TryGetTextureFormat(state.renderTarget.DepthTexture.impl.Format, state.renderTarget.flags | TextureFlags.DepthStencilTarget,
                                out var depthFormat))
                        {
                            sdlDepthFormat = depthFormat;
                        }
                    }

                    fixed(SDL.GPUColorTargetDescription *colorTargetDescriptionsPtr = CollectionsMarshal.AsSpan(colorTargetDescriptions))
                    {
                        SDL.GPUVertexBufferDescription[] vertexDescriptions = [vertexDescription];

                        fixed (SDL.GPUVertexBufferDescription* descriptions = vertexDescriptions)
                        {
                            var info = new SDL.GPUGraphicsPipelineCreateInfo()
                            {
                                PrimitiveType = state.primitiveType switch
                                {
                                    MeshTopology.TriangleStrip => SDL.GPUPrimitiveType.TriangleStrip,
                                    MeshTopology.Triangles => SDL.GPUPrimitiveType.TriangleList,
                                    MeshTopology.Lines => SDL.GPUPrimitiveType.LineList,
                                    MeshTopology.LineStrip => SDL.GPUPrimitiveType.LineStrip,
                                    _ => throw new ArgumentOutOfRangeException("Invalid value for primitive type", nameof(state.primitiveType)),
                                },
                                VertexShader = shader.vertex,
                                FragmentShader = shader.fragment,
                                RasterizerState = new()
                                {
                                    CullMode = state.cull switch
                                    {
                                        CullingMode.None => SDL.GPUCullMode.None,
                                        CullingMode.Front => SDL.GPUCullMode.Front,
                                        CullingMode.Back => SDL.GPUCullMode.Back,
                                        _ => throw new ArgumentOutOfRangeException("Invalid value for cull", nameof(state.cull)),
                                    },
                                    FillMode = state.wireframe ? SDL.GPUFillMode.Line : SDL.GPUFillMode.Fill,
                                    FrontFace = SDL.GPUFrontFace.Clockwise,
                                },
                                DepthStencilState = new()
                                {
                                    EnableDepthTest = (byte)(state.enableDepth ? 1 : 0),
                                    EnableDepthWrite = (byte)(state.depthWrite ? 1 : 0),
                                    CompareOp = SDL.GPUCompareOp.LessOrEqual,
                                },
                                VertexInputState = new()
                                {
                                    NumVertexBuffers = 1,
                                    NumVertexAttributes = (uint)attributesSpan.Length,
                                    VertexAttributes = (nint)attributes,
                                    VertexBufferDescriptions = (nint)descriptions,
                                },
                                TargetInfo = new()
                                {
                                    NumColorTargets = (uint)colorTargetDescriptions.Count,
                                    ColorTargetDescriptions = (nint)colorTargetDescriptionsPtr,
                                    HasDepthStencilTarget = (byte)(depthStencilFormat.HasValue ? 1 : 0),
                                    DepthStencilFormat = sdlDepthFormat,
                                },
                            };

                            pipeline = SDL.CreateGPUGraphicsPipeline(device, in info);

                            graphicsPipelines.Add(hash, pipeline);
                        }
                    }
                }
            }
        }

        return pipeline != nint.Zero;
    }

    private bool TryGetStaticMeshRenderPipeline(RenderState state, out nint pipeline)
    {
        if (state.shader == null ||
            state.staticMeshEntries == null ||
            state.shaderInstance?.program is not SDLGPUShaderProgram { Type: ShaderType.VertexFragment } shader)
        {
            pipeline = nint.Zero;

            return false;
        }

        var hash = state.StateKey;

        if (!graphicsPipelines.TryGetValue(hash, out pipeline))
        {
            unsafe
            {
                var vertexSamplerCount = state.vertexTextures?.Length ?? 0;
                var fragmentSamplerCount = state.fragmentTextures?.Length ?? 0;

                if (vertexSamplerCount < state.shaderInstance.vertexTextureBindings.Count ||
                    fragmentSamplerCount < state.shaderInstance.fragmentTextureBindings.Count)
                {
                    pipeline = nint.Zero;

                    return false;
                }

                var depthStencilFormat = DepthStencilFormat;

                if (!depthStencilFormat.HasValue ||
                    !TryGetTextureFormat(depthStencilFormat.Value, TextureFlags.DepthStencilTarget,
                        out var sdlDepthFormat))
                {
                    sdlDepthFormat = SDL.GPUTextureFormat.D24Unorm;
                }

                var shaderAttributes = new SDL.GPUVertexAttribute[state.shaderInstance.attributes.Length];
                var vertexDescriptions = new SDL.GPUVertexBufferDescription[state.shaderInstance.attributes.Length];

                for (var i = 0; i < state.shaderInstance.attributes.Length; i++)
                {
                    var attribute = state.shaderInstance.attributes[i];

                    var format = attribute switch
                    {
                        VertexAttribute.Position or
                            VertexAttribute.Normal or
                            VertexAttribute.Tangent or
                            VertexAttribute.Bitangent => SDL.GPUVertexElementFormat.Float3,
                        VertexAttribute.Color0 or
                            VertexAttribute.Color1 or
                            VertexAttribute.Color2 or
                            VertexAttribute.Color3 or
                            VertexAttribute.BlendIndices or
                            VertexAttribute.BlendWeights => SDL.GPUVertexElementFormat.Float4,
                        VertexAttribute.TexCoord0 or
                            VertexAttribute.TexCoord1 or
                            VertexAttribute.TexCoord2 or
                            VertexAttribute.TexCoord3 or
                            VertexAttribute.TexCoord4 or
                            VertexAttribute.TexCoord5 or
                            VertexAttribute.TexCoord6 or
                            VertexAttribute.TexCoord7 => SDL.GPUVertexElementFormat.Float2,
                            _ => throw new ArgumentOutOfRangeException(nameof(attribute), $"Static Mesh Render Pipeline: Attribute {attribute} not implemented!"),
                    };

                    shaderAttributes[i] = new()
                    {
                        BufferSlot = (uint)i,
                        Format = format,
                        Offset = 0,
                        Location = (uint)i,
                    };

                    vertexDescriptions[i] = new()
                    {
                        InputRate = SDL.GPUVertexInputRate.Vertex,
                        Slot = (uint)i,
                        Pitch = (uint)BufferAttributeContainer.BufferElementSize(attribute),
                    };
                }

                fixed (SDL.GPUVertexAttribute* attributes = shaderAttributes)
                {
                    var sourceBlend = state.sourceBlend switch
                    {
                        BlendMode.DstAlpha => SDL.GPUBlendFactor.DstAlpha,
                        BlendMode.DstColor => SDL.GPUBlendFactor.DstColor,
                        BlendMode.One => SDL.GPUBlendFactor.One,
                        BlendMode.OneMinusDstAlpha => SDL.GPUBlendFactor.OneMinusDstAlpha,
                        BlendMode.OneMinusDstColor => SDL.GPUBlendFactor.OneMinusDstColor,
                        BlendMode.OneMinusSrcAlpha => SDL.GPUBlendFactor.OneMinusSrcAlpha,
                        BlendMode.OneMinusSrcColor => SDL.GPUBlendFactor.OneMinusSrcColor,
                        BlendMode.SrcAlpha => SDL.GPUBlendFactor.SrcAlpha,
                        BlendMode.SrcAlphaSat => SDL.GPUBlendFactor.SrcAlphaSaturate,
                        BlendMode.SrcColor => SDL.GPUBlendFactor.SrcColor,
                        BlendMode.Zero => SDL.GPUBlendFactor.Zero,
                        BlendMode.Off => SDL.GPUBlendFactor.Invalid,
                        _ => throw new ArgumentOutOfRangeException(nameof(state.sourceBlend), "Invalid blend mode"),
                    };

                    var destinationBlend = state.destinationBlend switch
                    {
                        BlendMode.DstAlpha => SDL.GPUBlendFactor.DstAlpha,
                        BlendMode.DstColor => SDL.GPUBlendFactor.DstColor,
                        BlendMode.One => SDL.GPUBlendFactor.One,
                        BlendMode.OneMinusDstAlpha => SDL.GPUBlendFactor.OneMinusDstAlpha,
                        BlendMode.OneMinusDstColor => SDL.GPUBlendFactor.OneMinusDstColor,
                        BlendMode.OneMinusSrcAlpha => SDL.GPUBlendFactor.OneMinusSrcAlpha,
                        BlendMode.OneMinusSrcColor => SDL.GPUBlendFactor.OneMinusSrcColor,
                        BlendMode.SrcAlpha => SDL.GPUBlendFactor.SrcAlpha,
                        BlendMode.SrcAlphaSat => SDL.GPUBlendFactor.SrcAlphaSaturate,
                        BlendMode.SrcColor => SDL.GPUBlendFactor.SrcColor,
                        BlendMode.Zero => SDL.GPUBlendFactor.Zero,
                        BlendMode.Off => SDL.GPUBlendFactor.Invalid,
                        _ => throw new ArgumentOutOfRangeException(nameof(state.destinationBlend), "Invalid blend mode"),
                    };

                    var colorTargetDescriptions = new List<SDL.GPUColorTargetDescription>();

                    if (state.renderTarget == null || state.renderTarget.Disposed)
                    {
                        var colorTargetDescription = new SDL.GPUColorTargetDescription()
                        {
                            Format = SDL.GetGPUSwapchainTextureFormat(device, window.window),
                            BlendState = new()
                            {
                                EnableBlend = (byte)(state.sourceBlend != BlendMode.Off && state.destinationBlend != BlendMode.Off ? 1 : 0),
                                ColorBlendOp = SDL.GPUBlendOp.Add,
                                AlphaBlendOp = SDL.GPUBlendOp.Add,
                                SrcColorBlendfactor = sourceBlend,
                                SrcAlphaBlendfactor = sourceBlend,
                                DstColorBlendfactor = destinationBlend,
                                DstAlphaBlendfactor = destinationBlend,
                            }
                        };

                        colorTargetDescriptions.Add(colorTargetDescription);
                    }
                    else
                    {
                        foreach (var texture in state.renderTarget.colorTextures)
                        {
                            if (texture.Disposed ||
                                !TryGetTextureFormat(texture.impl.Format, state.renderTarget.flags | TextureFlags.ColorTarget, out var textureFormat))
                            {
                                continue;
                            }

                            var colorTargetDescription = new SDL.GPUColorTargetDescription()
                            {
                                Format = textureFormat,
                                BlendState = new()
                                {
                                    EnableBlend = (byte)(state.sourceBlend != BlendMode.Off && state.destinationBlend != BlendMode.Off ? 1 : 0),
                                    ColorBlendOp = SDL.GPUBlendOp.Add,
                                    AlphaBlendOp = SDL.GPUBlendOp.Add,
                                    SrcColorBlendfactor = sourceBlend,
                                    SrcAlphaBlendfactor = sourceBlend,
                                    DstColorBlendfactor = destinationBlend,
                                    DstAlphaBlendfactor = destinationBlend,
                                }
                            };

                            colorTargetDescriptions.Add(colorTargetDescription);
                        }

                        if (state.renderTarget.DepthTexture is { Disposed: false } &&
                            TryGetTextureFormat(state.renderTarget.DepthTexture.impl.Format, state.renderTarget.flags | TextureFlags.DepthStencilTarget,
                                out var depthFormat))
                        {
                            sdlDepthFormat = depthFormat;
                        }
                    }

                    fixed (SDL.GPUColorTargetDescription* colorTargetDescriptionsPtr = CollectionsMarshal.AsSpan(colorTargetDescriptions))
                    {
                        fixed (SDL.GPUVertexBufferDescription* descriptions = vertexDescriptions)
                        {
                            var info = new SDL.GPUGraphicsPipelineCreateInfo()
                            {
                                PrimitiveType = state.primitiveType switch
                                {
                                    MeshTopology.TriangleStrip => SDL.GPUPrimitiveType.TriangleStrip,
                                    MeshTopology.Triangles => SDL.GPUPrimitiveType.TriangleList,
                                    MeshTopology.Lines => SDL.GPUPrimitiveType.LineList,
                                    MeshTopology.LineStrip => SDL.GPUPrimitiveType.LineStrip,
                                    _ => throw new ArgumentOutOfRangeException("Invalid value for primitive type", nameof(state.primitiveType)),
                                },
                                VertexShader = shader.vertex,
                                FragmentShader = shader.fragment,
                                RasterizerState = new()
                                {
                                    CullMode = state.cull switch
                                    {
                                        CullingMode.None => SDL.GPUCullMode.None,
                                        CullingMode.Front => SDL.GPUCullMode.Front,
                                        CullingMode.Back => SDL.GPUCullMode.Back,
                                        _ => throw new ArgumentOutOfRangeException("Invalid value for cull", nameof(state.cull)),
                                    },
                                    FillMode = state.wireframe ? SDL.GPUFillMode.Line : SDL.GPUFillMode.Fill,
                                    FrontFace = SDL.GPUFrontFace.Clockwise,
                                },
                                DepthStencilState = new()
                                {
                                    EnableDepthTest = (byte)(state.enableDepth ? 1 : 0),
                                    EnableDepthWrite = (byte)(state.depthWrite ? 1 : 0),
                                    CompareOp = SDL.GPUCompareOp.LessOrEqual,
                                },
                                VertexInputState = new()
                                {
                                    NumVertexBuffers = (uint)vertexDescriptions.Length,
                                    NumVertexAttributes = (uint)shaderAttributes.Length,
                                    VertexAttributes = (nint)attributes,
                                    VertexBufferDescriptions = (nint)descriptions,
                                },
                                TargetInfo = new()
                                {
                                    NumColorTargets = (uint)colorTargetDescriptions.Count,
                                    ColorTargetDescriptions = (nint)colorTargetDescriptionsPtr,
                                    HasDepthStencilTarget = (byte)(depthStencilFormat.HasValue ? 1 : 0),
                                    DepthStencilFormat = sdlDepthFormat,
                                },
                            };

                            pipeline = SDL.CreateGPUGraphicsPipeline(device, in info);

                            graphicsPipelines.Add(hash, pipeline);
                        }
                    }
                }
            }
        }

        return pipeline != nint.Zero;
    }

    private void GetUniformData(in RenderState state, SDLGPUShaderProgram shader, ref StapleShaderUniform[] vertexUniformData,
        ref StapleShaderUniform[] fragmentUniformData)
    {
        if((vertexUniformData?.Length ?? 0) != shader.vertexMappings.Count)
        {
            Array.Resize(ref vertexUniformData, shader.vertexMappings.Count);
        }

        if ((fragmentUniformData?.Length ?? 0) != shader.fragmentMappings.Count)
        {
            Array.Resize(ref fragmentUniformData, shader.fragmentMappings.Count);
        }

        var counter = 0;

        foreach (var pair in shader.vertexMappings)
        {
            if (pair.Key.name == StapleRenderDataUniformName)
            {
                if (pair.Value.Length != Marshal.SizeOf<StapleRenderData>())
                {
                    Log.Error($"[Rendering] Warning: {StapleRenderDataUniformName} shader uniform is of invalid size {pair.Value.Length}: "
                        + $"Should be {Marshal.SizeOf<StapleRenderData>()}!");

                    continue;
                }

                unsafe
                {
                    viewData.renderData.world = state.world;
                    viewData.renderData.instanceOffset = state.instanceOffset > 0 ? state.instanceOffset : 0;

                    fixed (void* ptr = &viewData.renderData)
                    {
                        var source = new Span<byte>(ptr, Marshal.SizeOf<StapleRenderData>());
                        var target = new Span<byte>(pair.Value);

                        source.CopyTo(target);
                    }
                }
            }

            if (!ShouldPushVertexUniform(pair.Key.binding, pair.Value))
            {
                continue;
            }

            unsafe
            {
                var position = frameAllocator.position;

                frameAllocator.Allocate(pair.Value.Length);

                fixed (void* source = pair.Value)
                {
                    fixed (void* target = frameAllocator.buffer)
                    {
                        var p = (byte*)target;

                        p += position;

                        Buffer.MemoryCopy(source, p, pair.Value.Length, pair.Value.Length);
                    }
                }

                ref var uniformEntry = ref vertexUniformData[counter++];

                uniformEntry.binding = (byte)pair.Key.binding;
                uniformEntry.position = position;
                uniformEntry.size = pair.Value.Length;
            }
        }

        counter = 0;

        foreach (var pair in shader.fragmentMappings)
        {
            if (pair.Key.name == StapleFragmentDataUniformName)
            {
                if (pair.Value.Length != Marshal.SizeOf<StapleFragmentRenderData>())
                {
                    Log.Error($"[Rendering] Warning: {StapleFragmentDataUniformName} shader uniform is of invalid size {pair.Value.Length}: "
                        + $"Should be {Marshal.SizeOf<StapleFragmentRenderData>()}!");

                    continue;
                }

                unsafe
                {
                    viewData.fragmentData.time = Time.time;

                    fixed (void* ptr = &viewData.fragmentData)
                    {
                        var source = new Span<byte>(ptr, Marshal.SizeOf<StapleFragmentRenderData>());
                        var target = new Span<byte>(pair.Value);

                        source.CopyTo(target);
                    }
                }
            }

            if (!ShouldPushFragmentUniform(pair.Key.binding, pair.Value))
            {
                continue;
            }

            unsafe
            {
                var position = frameAllocator.position;

                frameAllocator.Allocate(pair.Value.Length);

                fixed (void* source = pair.Value)
                {
                    fixed (void* target = frameAllocator.buffer)
                    {
                        var p = (byte*)target;

                        p += position;

                        Buffer.MemoryCopy(source, p, pair.Value.Length, pair.Value.Length);
                    }
                }

                ref var uniformEntry = ref fragmentUniformData[counter++];

                uniformEntry.binding = (byte)pair.Key.binding;
                uniformEntry.position = position;
                uniformEntry.size = pair.Value.Length;
            }
        }
    }

    public void Render(RenderState state)
    {
        state.renderTarget = currentRenderTarget;

        var vertex = (SDLGPUVertexBuffer)state.vertexBuffer;
        var vertexLayout = (SDLGPUVertexLayout)vertex?.layout;
        var pipeline = nint.Zero;

        if (state.shader == null ||
            state.shaderInstance?.program is not SDLGPUShaderProgram { Type: ShaderType.VertexFragment } shader)
        {
            return;
        }

        if(state.staticMeshEntries != null)
        {
            if(!TryGetStaticMeshRenderPipeline(state, out pipeline))
            {
                return;
            }
        }
        else if(!TryGetRenderPipeline(state, vertexLayout, out pipeline))
        {
            return;
        }

        CheckQueuedPipeline(pipeline);

        StapleShaderUniform[] vertexUniformData = null;
        StapleShaderUniform[] fragmentUniformData = null;

        GetUniformData(in state, shader, ref vertexUniformData, ref fragmentUniformData);

        AddCommand(new SDLGPURenderCommand(state, pipeline, state.vertexTextures, state.fragmentTextures,
            vertexUniformData, fragmentUniformData, state.shaderInstance.attributes));
    }

    public void RenderTransient<T>(Span<T> vertices, VertexLayout layout, Span<ushort> indices, RenderState state)
        where T : unmanaged
    {
        state.renderTarget = currentRenderTarget;

        if (layout is not SDLGPUVertexLayout vertexLayout ||
            state.shaderInstance?.program is not SDLGPUShaderProgram { Type: ShaderType.VertexFragment } shader ||
            !TryGetRenderPipeline(state, vertexLayout, out var pipeline))
        {
            return;
        }

        var size = Marshal.SizeOf<T>();

        if (size % layout.Stride != 0)
        {
            return;
        }

        CheckQueuedPipeline(pipeline);

        if (!transientBuffers.TryGetValue(layout, out var entry))
        {
            entry = new()
            {
                backend = this,
            };

            transientBuffers.Add(layout, entry);
        }

        var vertexArray = new byte[size * vertices.Length];

        unsafe
        {
            fixed (void* ptr = vertexArray)
            {
                var target = new Span<T>(ptr, vertices.Length);

                vertices.CopyTo(target);
            }
        }

        entry.vertices.AddRange(vertexArray);

        entry.indices.AddRange(indices);

        state.startVertex = entry.startVertex;
        state.startIndex = entry.startIndex;
        state.indexCount = indices.Length;

        entry.startVertex += vertices.Length;
        entry.startIndex += indices.Length;

        var vertexUniformData = new StapleShaderUniform[shader.vertexMappings.Count];
        var fragmentUniformData = new StapleShaderUniform[shader.fragmentMappings.Count];

        var counter = 0;

        foreach (var pair in shader.vertexMappings)
        {
            if (pair.Key.name == StapleRenderDataUniformName)
            {
                if (pair.Value.Length != Marshal.SizeOf<StapleRenderData>())
                {
                    Log.Error($"[Rendering] Warning: {StapleRenderDataUniformName} shader uniform is of invalid size {pair.Value.Length}: "
                        + $"Should be {Marshal.SizeOf<StapleRenderData>()}!");

                    continue;
                }

                unsafe
                {
                    viewData.renderData.world = state.world;
                    viewData.renderData.instanceOffset = state.instanceOffset > 0 ? state.instanceOffset : 0;

                    fixed (void* ptr = &viewData.renderData)
                    {
                        var source = new Span<byte>(ptr, Marshal.SizeOf<StapleRenderData>());
                        var target = new Span<byte>(pair.Value);

                        source.CopyTo(target);
                    }
                }
            }

            if (!ShouldPushVertexUniform(pair.Key.binding, pair.Value))
            {
                continue;
            }

            unsafe
            {
                var position = frameAllocator.position;

                frameAllocator.Allocate(pair.Value.Length);

                fixed (void* source = pair.Value)
                {
                    fixed (void* target = frameAllocator.buffer)
                    {
                        var p = (byte*)target;

                        p += position;

                        Buffer.MemoryCopy(source, p, pair.Value.Length, pair.Value.Length);
                    }
                }

                ref var uniformEntry = ref vertexUniformData[counter++];

                uniformEntry.binding = (byte)pair.Key.binding;
                uniformEntry.position = position;
                uniformEntry.size = pair.Value.Length;
            }
        }

        counter = 0;

        foreach (var pair in shader.fragmentMappings)
        {
            if (pair.Key.name == StapleFragmentDataUniformName)
            {
                if (pair.Value.Length != Marshal.SizeOf<StapleFragmentRenderData>())
                {
                    Log.Error($"[Rendering] Warning: {StapleFragmentDataUniformName} shader uniform is of invalid size {pair.Value.Length}: "
                        + $"Should be {Marshal.SizeOf<StapleFragmentRenderData>()}!");

                    continue;
                }

                unsafe
                {
                    viewData.fragmentData.time = Time.time;

                    fixed (void* ptr = &viewData.fragmentData)
                    {
                        var source = new Span<byte>(ptr, Marshal.SizeOf<StapleFragmentRenderData>());
                        var target = new Span<byte>(pair.Value);

                        source.CopyTo(target);
                    }
                }
            }

            if (!ShouldPushFragmentUniform(pair.Key.binding, pair.Value))
            {
                continue;
            }

            unsafe
            {
                var position = frameAllocator.position;

                frameAllocator.Allocate(pair.Value.Length);

                fixed (void* source = pair.Value)
                {
                    fixed (void* target = frameAllocator.buffer)
                    {
                        var p = (byte*)target;

                        p += position;

                        Buffer.MemoryCopy(source, p, pair.Value.Length, pair.Value.Length);
                    }
                }

                ref var uniformEntry = ref fragmentUniformData[counter++];

                uniformEntry.binding = (byte)pair.Key.binding;
                uniformEntry.position = position;
                uniformEntry.size = pair.Value.Length;
            }
        }

        AddCommand(new SDLGPURenderTransientCommand(state, pipeline, state.vertexTextures, state.fragmentTextures, vertexUniformData,
            fragmentUniformData, entry));
    }

    public void RenderTransient<T>(Span<T> vertices, VertexLayout layout, Span<uint> indices, RenderState state)
        where T : unmanaged
    {
        state.renderTarget = currentRenderTarget;

        if (layout is not SDLGPUVertexLayout vertexLayout ||
            state.shaderInstance?.program is not SDLGPUShaderProgram { Type: ShaderType.VertexFragment } shader ||
            !TryGetRenderPipeline(state, vertexLayout, out var pipeline))
        {
            return;
        }

        var size = Marshal.SizeOf<T>();

        if (size % layout.Stride != 0)
        {
            return;
        }

        CheckQueuedPipeline(pipeline);

        if (!transientBuffers.TryGetValue(layout, out var entry))
        {
            entry = new()
            {
                backend = this,
            };

            transientBuffers.Add(layout, entry);
        }

        var vertexArray = new byte[size * vertices.Length];

        unsafe
        {
            fixed (void* ptr = vertexArray)
            {
                var target = new Span<T>(ptr, vertices.Length);

                vertices.CopyTo(target);
            }
        }

        entry.vertices.AddRange(vertexArray);

        entry.uintIndices.AddRange(indices);

        state.startVertex = entry.startVertex;
        state.startIndex = entry.startIndexUInt;
        state.indexCount = indices.Length;

        entry.startVertex += vertices.Length;
        entry.startIndexUInt += indices.Length;

        var vertexUniformData = new StapleShaderUniform[shader.vertexMappings.Count];
        var fragmentUniformData = new StapleShaderUniform[shader.fragmentMappings.Count];

        var counter = 0;

        foreach (var pair in shader.vertexMappings)
        {
            if (pair.Key.name == StapleRenderDataUniformName)
            {
                if (pair.Value.Length != Marshal.SizeOf<StapleRenderData>())
                {
                    Log.Error($"[Rendering] Warning: {StapleRenderDataUniformName} shader uniform is of invalid size {pair.Value.Length}: "
                        + $"Should be {Marshal.SizeOf<StapleRenderData>()}!");

                    continue;
                }

                unsafe
                {
                    viewData.renderData.world = state.world;
                    viewData.renderData.instanceOffset = state.instanceOffset > 0 ? state.instanceOffset : 0;

                    fixed (void* ptr = &viewData.renderData)
                    {
                        var source = new Span<byte>(ptr, Marshal.SizeOf<StapleRenderData>());
                        var target = new Span<byte>(pair.Value);

                        source.CopyTo(target);
                    }
                }
            }

            if(!ShouldPushVertexUniform(pair.Key.binding, pair.Value))
            {
                continue;
            }

            unsafe
            {
                var position = frameAllocator.position;

                frameAllocator.Allocate(pair.Value.Length);

                fixed (void* source = pair.Value)
                {
                    fixed (void* target = frameAllocator.buffer)
                    {
                        var p = (byte*)target;

                        p += position;

                        Buffer.MemoryCopy(source, p, pair.Value.Length, pair.Value.Length);
                    }
                }

                ref var uniformEntry = ref vertexUniformData[counter++];

                uniformEntry.binding = (byte)pair.Key.binding;
                uniformEntry.position = position;
                uniformEntry.size = pair.Value.Length;
            }
        }

        counter = 0;

        foreach (var pair in shader.fragmentMappings)
        {
            if (pair.Key.name == StapleFragmentDataUniformName)
            {
                if (pair.Value.Length != Marshal.SizeOf<StapleFragmentRenderData>())
                {
                    Log.Error($"[Rendering] Warning: {StapleFragmentDataUniformName} shader uniform is of invalid size {pair.Value.Length}: "
                        + $"Should be {Marshal.SizeOf<StapleFragmentRenderData>()}!");

                    continue;
                }

                unsafe
                {
                    viewData.fragmentData.time = Time.time;

                    fixed (void* ptr = &viewData.fragmentData)
                    {
                        var source = new Span<byte>(ptr, Marshal.SizeOf<StapleFragmentRenderData>());
                        var target = new Span<byte>(pair.Value);

                        source.CopyTo(target);
                    }
                }
            }

            if (!ShouldPushFragmentUniform(pair.Key.binding, pair.Value))
            {
                continue;
            }

            unsafe
            {
                var position = frameAllocator.position;

                frameAllocator.Allocate(pair.Value.Length);

                fixed (void* source = pair.Value)
                {
                    fixed (void* target = frameAllocator.buffer)
                    {
                        var p = (byte*)target;

                        p += position;

                        Buffer.MemoryCopy(source, p, pair.Value.Length, pair.Value.Length);
                    }
                }

                ref var uniformEntry = ref fragmentUniformData[counter++];

                uniformEntry.binding = (byte)pair.Key.binding;
                uniformEntry.position = position;
                uniformEntry.size = pair.Value.Length;
            }
        }

        AddCommand(new SDLGPURenderTransientUIntCommand(state, pipeline, state.vertexTextures, state.fragmentTextures, vertexUniformData,
            fragmentUniformData, entry));
    }
}
