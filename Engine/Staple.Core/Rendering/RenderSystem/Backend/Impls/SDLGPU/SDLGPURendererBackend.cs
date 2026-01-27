using Evergine.Bindings.Vulkan;
using SDL;
using Standart.Hash.xxHash;
using Staple.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Internal;

internal unsafe partial class SDLGPURendererBackend : IRendererBackend, IWorldChangeReceiver
{
    private const uint VulkanVersionMajor = 1;
    private const uint VulkanVersionMinor = 2;
    private const uint VulkanVersionPatch = 0;

    private static readonly StringID StapleRenderDataUniformName = "StapleRenderData";
    private static readonly StringID StapleFragmentDataUniformName = "StapleFragmentRenderData";

    internal static readonly int RenderDataByteSize = Marshal.SizeOf<StapleRenderData>();
    internal static readonly int FragmentRenderDataByteSize = Marshal.SizeOf<StapleFragmentRenderData>();
    internal static readonly int Matrix4x4ByteSize = Marshal.SizeOf<Matrix4x4>();
    internal static readonly int IndirectDrawCommandSize = Marshal.SizeOf<SDL_GPUIndexedIndirectDrawCommand>();

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
        public bool useWorldMatrix;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct StapleFragmentRenderData
    {
        public float time;
    }

    internal unsafe class BufferResource
    {
        public SDL_GPUBuffer *buffer;
        public SDL_GPUTransferBuffer *transferBuffer;

        public RenderBufferFlags flags;

        public int length;

        public bool used = false;
    }

    internal unsafe class TextureResource
    {
        public SDL_GPUTexture *texture;
        public SDL_GPUTransferBuffer *transferBuffer;
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

    internal unsafe class TransientEntry
    {
        public readonly List<byte> vertices = [];

        public readonly List<ushort> indices = [];

        public readonly List<uint> uintIndices = [];

        public SDL_GPUBuffer *vertexBuffer;

        public SDL_GPUBuffer *indexBuffer;

        public SDL_GPUBuffer *uintIndexBuffer;

        public int startVertex;

        public int startIndex;

        public int startIndexUInt;

        public SDLGPURendererBackend backend;

        public void Clear()
        {
            if(vertexBuffer != null)
            {
                SDL3.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);

                vertexBuffer = null;
            }

            if (indexBuffer != null)
            {
                SDL3.SDL_ReleaseGPUBuffer(backend.device, indexBuffer);

                indexBuffer = null;
            }

            if (uintIndexBuffer != null)
            {
                SDL3.SDL_ReleaseGPUBuffer(backend.device, uintIndexBuffer);

                uintIndexBuffer = null;
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
                var createInfo = new SDL_GPUBufferCreateInfo()
                {
                    size = (uint)vertices.Count,
                    usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX,
                };

                vertexBuffer = SDL3.SDL_CreateGPUBuffer(backend.device, &createInfo);

                if (vertexBuffer == null)
                {
                    return;
                }

                var transferBuffer = backend.GetTransferBuffer(false, vertices.Count);

                if (transferBuffer == null)
                {
                    SDL3.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);

                    vertexBuffer = null;

                    return;
                }

                if (!backend.BeginCopyPass())
                {
                    SDL3.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);

                    vertexBuffer = null;

                    return;
                }

                var mapData = SDL3.SDL_MapGPUTransferBuffer(backend.device, transferBuffer, true);

                var from = CollectionsMarshal.AsSpan(vertices);

                var to = new Span<byte>((void*)mapData, vertices.Count);

                from.CopyTo(to);

                SDL3.SDL_UnmapGPUTransferBuffer(backend.device, transferBuffer);

                var location = new SDL_GPUTransferBufferLocation()
                {
                    transfer_buffer = transferBuffer,
                };

                var region = new SDL_GPUBufferRegion()
                {
                    buffer = vertexBuffer,
                    size = (uint)vertices.Count,
                };

                SDL3.SDL_UploadToGPUBuffer(backend.copyPass, &location, &region, false);
            }

            if(indices.Count > 0)
            {
                var createInfo = new SDL_GPUBufferCreateInfo()
                {
                    size = (uint)(indices.Count * sizeof(ushort)),
                    usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX,
                };

                indexBuffer = SDL3.SDL_CreateGPUBuffer(backend.device, &createInfo);

                if (indexBuffer == null)
                {
                    return;
                }

                var transferBuffer = backend.GetTransferBuffer(false, indices.Count * sizeof(ushort));

                if (transferBuffer == null)
                {
                    SDL3.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);
                    SDL3.SDL_ReleaseGPUBuffer(backend.device, indexBuffer);

                    vertexBuffer = null;
                    indexBuffer = null;

                    return;
                }

                if (!backend.BeginCopyPass())
                {
                    SDL3.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);
                    SDL3.SDL_ReleaseGPUBuffer(backend.device, indexBuffer);

                    vertexBuffer = null;
                    indexBuffer = null;

                    return;
                }

                var mapData = SDL3.SDL_MapGPUTransferBuffer(backend.device, transferBuffer, true);

                var from = CollectionsMarshal.AsSpan(indices);

                var to = new Span<ushort>((void*)mapData, indices.Count);

                from.CopyTo(to);

                SDL3.SDL_UnmapGPUTransferBuffer(backend.device, transferBuffer);

                var location = new SDL_GPUTransferBufferLocation()
                {
                    transfer_buffer = transferBuffer,
                };

                var region = new SDL_GPUBufferRegion()
                {
                    buffer = indexBuffer,
                    size = (uint)(indices.Count * sizeof(ushort)),
                };

                SDL3.SDL_UploadToGPUBuffer(backend.copyPass, &location, &region, false);
            }

            if (uintIndices.Count <= 0)
            {
                return;
            }
            
            {
                var createInfo = new SDL_GPUBufferCreateInfo()
                {
                    size = (uint)(uintIndices.Count * sizeof(uint)),
                    usage = SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX,
                };

                uintIndexBuffer = SDL3.SDL_CreateGPUBuffer(backend.device, &createInfo);

                if (uintIndexBuffer == null)
                {
                    return;
                }

                var transferBuffer = backend.GetTransferBuffer(false, uintIndices.Count * sizeof(uint));

                if (transferBuffer == null)
                {
                    SDL3.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);
                    SDL3.SDL_ReleaseGPUBuffer(backend.device, uintIndexBuffer);

                    if (indexBuffer != null)
                    {
                        SDL3.SDL_ReleaseGPUBuffer(backend.device, indexBuffer);
                    }

                    vertexBuffer = null;
                    indexBuffer = null;
                    uintIndexBuffer = null;

                    return;
                }

                if (!backend.BeginCopyPass())
                {
                    SDL3.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);
                    SDL3.SDL_ReleaseGPUBuffer(backend.device, uintIndexBuffer);

                    if (indexBuffer != null)
                    {
                        SDL3.SDL_ReleaseGPUBuffer(backend.device, indexBuffer);
                    }

                    vertexBuffer = null;
                    indexBuffer = null;
                    uintIndexBuffer = null;

                    return;
                }

                var mapData = SDL3.SDL_MapGPUTransferBuffer(backend.device, transferBuffer, true);

                var from = CollectionsMarshal.AsSpan(uintIndices);

                var to = new Span<uint>((void*)mapData, uintIndices.Count);

                from.CopyTo(to);

                SDL3.SDL_UnmapGPUTransferBuffer(backend.device, transferBuffer);

                var location = new SDL_GPUTransferBufferLocation()
                {
                    transfer_buffer = transferBuffer,
                };

                var region = new SDL_GPUBufferRegion()
                {
                    buffer = uintIndexBuffer,
                    size = (uint)(uintIndices.Count * sizeof(uint)),
                };

                SDL3.SDL_UploadToGPUBuffer(backend.copyPass, &location, &region, false);
            }
        }
    }
    #endregion

    #region Fields
    internal Vector2Int renderSize;

    internal SDL_GPUDevice *device;
    internal SDL_GPUCommandBuffer *commandBuffer;
    internal SDL_GPURenderPass *renderPass;
    internal SDL_GPUCopyPass *copyPass;

    internal SDL_GPUTexture *swapchainTexture;
    internal int swapchainWidth;
    internal int swapchainHeight;
    internal ITexture depthTexture;

    internal readonly ViewData viewData = new();
    internal readonly MemoryAllocator frameAllocator = new();
    internal readonly MemoryAllocator<StapleShaderUniform> shaderUniformFrameAllocator = new();
    internal readonly MemoryAllocator<SDL_GPUTextureSamplerBinding> textureSampleBindingFrameAllocator = new();
    internal readonly SDL_GPUBuffer *[] bufferStaging = new SDL_GPUBuffer*[64];

    private SDL3RenderWindow window;
    private readonly Dictionary<int, NativePointerWrapper<SDL_GPUGraphicsPipeline>> graphicsPipelines = [];
    private readonly Dictionary<TextureFlags, NativePointerWrapper<SDL_GPUSampler>> textureSamplers = [];
    private bool needsDepthTextureUpdate;
    private readonly List<IRenderCommand> commands = [];
    private readonly List<(SDLGPUTexture, Action<byte[]>)> readTextureQueue = [];

    private readonly BufferResource[] vertexBuffers = new BufferResource[ushort.MaxValue - 1];
    private readonly BufferResource[] indexBuffers = new BufferResource[ushort.MaxValue - 1];
    private readonly Dictionary<VertexLayout, TransientEntry> transientBuffers = [];
    private readonly TextureResource[] textures = new TextureResource[ushort.MaxValue - 1];
    private readonly List<SDLGPUShaderProgram> shaders = [];
    private readonly Dictionary<TransferBufferCacheKey, NativePointerWrapper<SDL_GPUTransferBuffer>> cachedtransferBuffers = [];
    private readonly ulong[] lastVertexShaderUniformHashes = new ulong[20];
    private readonly ulong[] lastFragmentShaderUniformHashes = new ulong[20];
    private RenderTarget currentRenderTarget;

    private SDL_GPUIndexedIndirectDrawCommand[] indirectCommands = new SDL_GPUIndexedIndirectDrawCommand[1024];
    private uint[] indirectEntityIndices = new uint[1024];
    internal SDL_GPUBuffer *indirectCommandBuffer = null;
    private int indirectCommandBufferLength;
    private int indirectCommandPosition;
    private int indirectCommandInstance;
    private bool needsIndirectBufferUpdate = true;

    internal static SDL_GPUBuffer*[] staticMeshVertexBuffers = new SDL_GPUBuffer*[18];
    internal static SDL_GPUBuffer *staticMeshIndexBuffer = null;
    internal static int[] staticMeshVertexBuffersLength = new int[18];
    internal static int[] staticMeshVertexBuffersElementSize = new int[18];
    internal static int staticMeshIndexBufferLength;
    internal static SDL_GPUBuffer *entityTransformsBuffer = null;
    internal static int entityTransformsBufferLength;
    internal static SDL_GPUBuffer *entityTransformIndexBuffer = null;
    internal static int entityTransformIndexBufferLength;

    private bool iteratingCommands;
    private int commandIndex;
    private SDL_GPUFence *fence;
    #endregion

    #region Command Support Fields
    internal static SDL_GPUGraphicsPipeline *lastGraphicsPipeline;
    internal static SDL_GPUGraphicsPipeline* lastQueuedGraphicsPipeline;
    internal static SDL_GPUBuffer *lastVertexBuffer;
    internal static SDL_GPUBuffer *lastIndexBuffer;
    internal static nint[] singleBuffer = new nint[1];
    #endregion

    public bool SupportsTripleBuffering => SDL3.SDL_WindowSupportsGPUPresentMode(device, window.window,
        SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX);

    public bool SupportsHDRColorSpace => SDL3.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2084) ||
        SDL3.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR);

    public bool SupportsLinearColorSpace => SDL3.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR);

    public TextureFormat SwapchainFormat
    {
        get
        {
            if(device == null)
            {
                return TextureFormat.RGBA8;
            }

            var format = SDL3.SDL_GetGPUSwapchainTextureFormat(device, window.window);

            if(TryGetStapleTextureFormat(format, out var stapleFormat) == false)
            {
                return TextureFormat.RGBA8;
            }

            return stapleFormat;
        }
    }

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
            Log.Error($"Missing window for SDL GPU: {window}");

            return false;
        }

#if DEBUG
        SDL3.SDL_SetLogPriority((int)SDL_LogCategory.SDL_LOG_CATEGORY_GPU, SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE);
#endif

        this.window = w;

        var version = SDL3.SDL_GetVersion();

        Log.Debug($"SDL version {SDL3.SDL_VERSIONNUM_MAJOR(version)}.{SDL3.SDL_VERSIONNUM_MINOR(version)}.{SDL3.SDL_VERSIONNUM_MICRO(version)}");

        var props = SDL3.SDL_CreateProperties();

        SDL3.SDL_SetBooleanProperty(props, SDL3.SDL_PROP_GPU_DEVICE_CREATE_DEBUGMODE_BOOLEAN, debug);

        var createOptions = new SDL_GPUVulkanOptions()
        {
            vulkan_api_version = MakeVulkanVersion(VulkanVersionMajor, VulkanVersionMinor, VulkanVersionPatch),
        };

        var deviceExtensions = new[]
        {
            "VK_KHR_shader_draw_parameters",
        };

        var count = deviceExtensions.Length;
        var size = nint.Size * count;
        var unmanagedPointer = Marshal.AllocHGlobal(size);

        var span = new Span<byte>((void*)unmanagedPointer, size);

        span.Clear();

        var stringPointers = new List<nint>();

        for(var i = 0; i < deviceExtensions.Length; i++)
        {
            var stringData = Encoding.UTF8.GetBytes(deviceExtensions[i]);

            var ptr = Marshal.AllocHGlobal(stringData.Length + 1);

            var from = new Span<byte>(stringData);
            var to = new Span<byte>((void*)ptr, stringData.Length + 1);

            to.Clear();

            from.CopyTo(to);

            stringPointers.Add(ptr);

            Marshal.WriteIntPtr(unmanagedPointer, i * nint.Size, ptr);
        }

        createOptions.device_extension_count = (uint)deviceExtensions.Length;

        createOptions.device_extension_names = (byte **)unmanagedPointer;

        var drawParametersStruct = new VkPhysicalDeviceShaderDrawParametersFeatures()
        {
            sType = VkStructureType.VK_STRUCTURE_TYPE_PHYSICAL_DEVICE_SHADER_DRAW_PARAMETERS_FEATURES,
            shaderDrawParameters = true,
        };

        void* drawPtr = &drawParametersStruct;

        createOptions.feature_list = (nint)drawPtr;

        var physicalDeviceFeatures = new VkPhysicalDeviceFeatures()
        {
            robustBufferAccess = true,
        };

        void *physicalDevicePtr = &physicalDeviceFeatures;

        createOptions.vulkan_10_physical_device_features = (nint)physicalDevicePtr;

        switch (renderer)
        {
            case RendererType.Vulkan:

                SDL3.SDL_SetBooleanProperty(props, SDL3.SDL_PROP_GPU_DEVICE_CREATE_SHADERS_SPIRV_BOOLEAN, true);

                void *ptr = &createOptions;

                SDL3.SDL_SetPointerProperty(props, SDL3.SDL_PROP_GPU_DEVICE_CREATE_VULKAN_OPTIONS_POINTER, (nint)ptr);

                break;

            case RendererType.Direct3D12:

                SDL3.SDL_SetBooleanProperty(props, SDL3.SDL_PROP_GPU_DEVICE_CREATE_SHADERS_DXBC_BOOLEAN, true);
                SDL3.SDL_SetBooleanProperty(props, SDL3.SDL_PROP_GPU_DEVICE_CREATE_SHADERS_DXIL_BOOLEAN, true);

                break;

            case RendererType.Metal:

                SDL3.SDL_SetBooleanProperty(props, SDL3.SDL_PROP_GPU_DEVICE_CREATE_SHADERS_MSL_BOOLEAN, true);
                SDL3.SDL_SetBooleanProperty(props, SDL3.SDL_PROP_GPU_DEVICE_CREATE_SHADERS_METALLIB_BOOLEAN, true);

                break;
        }

        SDL3.SDL_SetStringProperty(props, SDL3.SDL_PROP_GPU_DEVICE_CREATE_NAME_STRING, renderer switch
        {
            RendererType.Metal => "metal",
            RendererType.Direct3D12 => "direct3d12",
            _ => "vulkan",
        });

        device = SDL3.SDL_CreateGPUDeviceWithProperties(props);

        SDL3.SDL_DestroyProperties(props);

        Marshal.FreeHGlobal(unmanagedPointer);

        foreach(var ptr in stringPointers)
        {
            Marshal.FreeHGlobal(ptr);
        }

        if (device == null)
        {
            Log.Error($"Failed to create device: {SDL3.SDL_GetError()}");

            return false;
        }

        if(!DepthStencilFormat.HasValue || !SDL3.SDL_ClaimWindowForGPUDevice(device, w.window))
        {
            Log.Error($"Failed to get depth stencil format or claim window for GPU: {SDL3.SDL_GetError()}");

            SDL3.SDL_DestroyGPUDevice(device);

            device = null;

            return false;
        }

        UpdateRenderMode(renderFlags);

        for(var i = 0; i < staticMeshVertexBuffersElementSize.Length; i++)
        {
            staticMeshVertexBuffersElementSize[i] = BufferAttributeContainer.BufferElementSize(i);
        }

        World.AddChangeReceiver(this);

        return true;
    }

    public void UpdateRenderMode(RenderModeFlags flags)
    {
        if (device == null ||
            window == null ||
            window.window == null)
        {
            return;
        }

        var swapchainComposition = SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR;
        var presentMode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE;

        if(flags.HasFlag(RenderModeFlags.Vsync))
        {
            presentMode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC;
        }

        if(flags.HasFlag(RenderModeFlags.TripleBuffering) && SupportsTripleBuffering)
        {
            presentMode = SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX;
        }

        if(flags.HasFlag(RenderModeFlags.HDR10) && SupportsHDRColorSpace)
        {
            if(flags.HasFlag(RenderModeFlags.sRGB))
            {
                swapchainComposition = SDL3.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
                    SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR) ?
                    SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR :

                    SDL3.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
                    SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR) ?
                    SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR :
                    SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR;
            }
            else
            {
                swapchainComposition =
                    SDL3.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
                    SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2084) ?
                    SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2084 :
                    SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR;
            }
        }
        else
        {
            if (flags.HasFlag(RenderModeFlags.sRGB) && SupportsLinearColorSpace)
            {
                swapchainComposition = SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR;
            }
        }

        SDL3.SDL_SetGPUSwapchainParameters(device, window.window, swapchainComposition, presentMode);

        var w = 0;
        var h = 0;

        SDL3.SDL_GetWindowSizeInPixels(window.window, &w, &h);

        renderSize.X = w;
        renderSize.Y = h;

        needsDepthTextureUpdate = true;
    }

    public void UpdateViewport(int width, int height)
    {
        renderSize.X = width;
        renderSize.Y = height;

        needsDepthTextureUpdate = true;
    }

    public void Destroy()
    {
        if (device == null)
        {
            return;
        }

        World.RemoveChangeReceiver(this);

        foreach (var pair in graphicsPipelines)
        {
            SDL3.SDL_ReleaseGPUGraphicsPipeline(device, pair.Value.ptr);
        }

        graphicsPipelines.Clear();

        foreach(var pair in textureSamplers)
        {
            SDL3.SDL_ReleaseGPUSampler(device, pair.Value.ptr);
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

            if (buffer == null)
            {
                continue;
            }
            
            SDL3.SDL_ReleaseGPUBuffer(device, buffer);

            buffer = null;
        }

        if(staticMeshIndexBuffer != null)
        {
            SDL3.SDL_ReleaseGPUBuffer(device, staticMeshIndexBuffer);

            staticMeshIndexBuffer = null;
        }

        depthTexture?.Destroy();

        depthTexture = null;

        needsDepthTextureUpdate = true;

        SDL3.SDL_ReleaseWindowFromGPUDevice(device, window.window);

        SDL3.SDL_DestroyGPUDevice(device);

        device = null;
    }

    public void WorldChanged()
    {
        needsIndirectBufferUpdate = true;
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
        if(window.window == null)
        {
            return;
        }

        frameAllocator.Clear();
        shaderUniformFrameAllocator.Clear();
        textureSampleBindingFrameAllocator.Clear();
    }

    public void EndFrame()
    {
        commandBuffer = SDL3.SDL_AcquireGPUCommandBuffer(device);

        if (commandBuffer == null)
        {
            return;
        }

        uint w = 0;
        uint h = 0;

        unsafe
        {
            fixed(SDL_GPUTexture **t = &swapchainTexture)
            {
                if (!SDL3.SDL_WaitAndAcquireGPUSwapchainTexture(commandBuffer, window.window, t, &w, &h))
                {
                    SDL3.SDL_CancelGPUCommandBuffer(commandBuffer);

                    commandBuffer = null;

                    return;
                }
            }
        }

        swapchainWidth = (int)w;
        swapchainHeight = (int)h;

        UpdateDepthTextureIfNeeded(false);

        frameAllocator.EnsurePin();

        UpdateIndirectCommandBuffer();

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

            commands[i].Update();
        }

        iteratingCommands = false;

        FinishPasses();

        indirectCommandPosition = indirectCommandInstance = 0;

        commands.Clear();
        frameAllocator.Clear();
        shaderUniformFrameAllocator.Clear();
        textureSampleBindingFrameAllocator.Clear();

        lastGraphicsPipeline = null;
        lastQueuedGraphicsPipeline = null;
        lastVertexBuffer = null;
        lastIndexBuffer = null;

        Array.Clear(lastVertexShaderUniformHashes);
        Array.Clear(lastFragmentShaderUniformHashes);

        foreach (var pair in transientBuffers)
        {
            pair.Value.Clear();
        }

        fence = SDL3.SDL_SubmitGPUCommandBufferAndAcquireFence(commandBuffer);

        fixed(SDL_GPUFence **f = &fence)
        {
            if (!SDL3.SDL_WaitForGPUFences(device, true, f, 1))
            {
                Log.Error($"[SDL GPU] Failed to wait for GPU Fences: {SDL3.SDL_GetError()}");
            }
        }

        SDL3.SDL_ReleaseGPUFence(device, fence);

        for(var i = readTextureQueue.Count - 1; i >= 0; i--)
        {
            var item = readTextureQueue[i];

            readTextureQueue.RemoveAt(i);

            if (item.Item1 == null ||
                item.Item1.Disposed ||
                !TryGetTexture(item.Item1.handle, out var resource) ||
                !resource.used ||
                resource.transferBuffer == null)
            {
                continue;
            }

            unsafe
            {
                var buffer = GlobalAllocator<byte>.Instance.Rent(resource.length);

                var map = SDL3.SDL_MapGPUTransferBuffer(device, resource.transferBuffer, true);

                var from = new Span<byte>((void *)map, buffer.Length);
                var to = new Span<byte>(buffer);

                from.CopyTo(to);

                SDL3.SDL_UnmapGPUTransferBuffer(device, resource.transferBuffer);

                resource.transferBuffer = null;

                item.Item2?.Invoke(buffer);

                GlobalAllocator<byte>.Instance.Return(buffer);
            }

            commandBuffer = null;
        }

        if (currentRenderTarget == null)
        {
            return;
        }
        
        currentRenderTarget = null;

        Screen.Width = window.Size.X;
        Screen.Height = window.Size.Y;
    }

    private void CheckQueuedGraphicsPipeline(SDL_GPUGraphicsPipeline *newPipeline)
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
    }

    private bool ShouldPushFragmentUniform(int binding, Span<byte> data)
    {
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
        if(copyPass != null)
        {
            SDL3.SDL_EndGPUCopyPass(copyPass);

            copyPass = null;
        }

        if (renderPass == null)
        {
            return;
        }
        
        SDL3.SDL_EndGPURenderPass(renderPass);

        renderPass = null;
        lastGraphicsPipeline = null;
        lastQueuedGraphicsPipeline = null;
        lastVertexBuffer = null;
        lastIndexBuffer = null;
    }

    internal void ResumeRenderPass()
    {
        FinishPasses();

        if (commandBuffer == null)
        {
            return;
        }

        SDL_GPUTexture* texture = null;
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

        if (texture == null ||
            (depthTexture?.Disposed ?? true) ||
            !TryGetTexture(depthTexture.handle, out var depthTextureResource))
        {
            return;
        }

        var colorTarget = new SDL_GPUColorTargetInfo()
        {
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            texture = texture,
        };

        var depthTarget = new SDL_GPUDepthStencilTargetInfo()
        {
            clear_depth = 1,
            load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            texture = depthTextureResource.texture,
            stencil_load_op = SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
            stencil_store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };

        renderPass = SDL3.SDL_BeginGPURenderPass(commandBuffer, &colorTarget, 1, &depthTarget);

        if (renderPass == null)
        {
            return;
        }

        var viewportData = new SDL_GPUViewport()
        {
            x = (int)(viewData.viewport.X * width),
            y = (int)(viewData.viewport.Y * height),
            w = (int)(viewData.viewport.Z * width),
            h = (int)(viewData.viewport.W * height),
            min_depth = 0,
            max_depth = 1,
        };

        SDL3.SDL_SetGPUViewport(renderPass, &viewportData);
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

        AddCommand(new SDLGPUBeginRenderPassCommand(this, target, clear, clearColor, viewport, view, projection));
    }

    public IShaderProgram CreateShaderVertexFragment(byte[] vertex, byte[] fragment,
        VertexFragmentShaderMetrics vertexMetrics, VertexFragmentShaderMetrics fragmentMetrics)
    {
        unsafe
        {
            var vertexEntryPointBytes = Encoding.UTF8.GetBytes("main");
            var fragmentEntryPointBytes = Encoding.UTF8.GetBytes("main");

            SDL_GPUShader *vertexShader = null;
            SDL_GPUShader *fragmentShader = null;

            fixed (byte* entry = vertexEntryPointBytes)
            {
                fixed (byte* v = vertex)
                {
                    var info = new SDL_GPUShaderCreateInfo()
                    {
                        code = v,
                        code_size = (uint)vertex.Length,
                        entrypoint = entry,
                        format = RenderWindow.CurrentRenderer switch
                        {
                            RendererType.Direct3D12 => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL,
                            RendererType.Metal => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
                            _ => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
                        },
                        stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX,
                        num_samplers = (uint)vertexMetrics.samplerCount,
                        num_storage_buffers = (uint)vertexMetrics.storageBufferCount,
                        num_storage_textures = (uint)vertexMetrics.storageTextureCount,
                        num_uniform_buffers = (uint)vertexMetrics.uniformBufferCount,
                    };

                    vertexShader = SDL3.SDL_CreateGPUShader(device, &info);

                    if (vertexShader == null)
                    {
                        return null;
                    }
                }
            }

            fixed (byte* entry = fragmentEntryPointBytes)
            {
                fixed (byte* f = fragment)
                {
                    var info = new SDL_GPUShaderCreateInfo()
                    {
                        code = f,
                        code_size = (uint)fragment.Length,
                        entrypoint = entry,
                        format = RenderWindow.CurrentRenderer switch
                        {
                            RendererType.Direct3D12 => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL,
                            RendererType.Metal => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
                            _ => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
                        },
                        stage = SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
                        num_samplers = (uint)fragmentMetrics.samplerCount,
                        num_storage_buffers = (uint)fragmentMetrics.storageBufferCount,
                        num_storage_textures = (uint)fragmentMetrics.storageTextureCount,
                        num_uniform_buffers = (uint)fragmentMetrics.uniformBufferCount,
                    };

                    fragmentShader = SDL3.SDL_CreateGPUShader(device, &info);

                    if (fragmentShader == null)
                    {
                        SDL3.SDL_ReleaseGPUShader(device, vertexShader);

                        return null;
                    }
                }
            }

            var shader = new SDLGPUShaderProgram(device, vertexShader, fragmentShader);

            shaders.Add(shader);

            return shader;
        }
    }

    public IShaderProgram CreateShaderCompute(byte[] compute, ComputeShaderMetrics metrics)
    {
        unsafe
        {
            var entryPointBytes = Encoding.UTF8.GetBytes("main");

            SDL_GPUComputePipeline *computeShader;

            fixed (byte* e = entryPointBytes)
            {
                fixed (byte* c = compute)
                {
                    var info = new SDL_GPUComputePipelineCreateInfo()
                    {
                        code = c,
                        code_size = (uint)compute.Length,
                        entrypoint = e,
                        format = RenderWindow.CurrentRenderer switch
                        {
                            RendererType.Direct3D12 => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL,
                            RendererType.Metal => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
                            _ => SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
                        },
                        num_samplers = (uint)metrics.samplerCount,
                        num_uniform_buffers = (uint)metrics.uniformBufferCount,
                        num_readonly_storage_buffers = (uint)metrics.readOnlyStorageBufferCount,
                        num_readonly_storage_textures = (uint)metrics.readOnlyStorageTextureCount,
                        num_readwrite_storage_buffers = (uint)metrics.readWriteStorageBufferCount,
                        num_readwrite_storage_textures = (uint)metrics.readWriteStorageTextureCount,
                        threadcount_x = (uint)metrics.threadCountX,
                        threadcount_y = (uint)metrics.threadCountY,
                        threadcount_z = (uint)metrics.threadCountZ,
                    };

                    computeShader = SDL3.SDL_CreateGPUComputePipeline(device, &info);

                    if (computeShader == null)
                    {
                        return null;
                    }
                }
            }

            var shader = new SDLGPUShaderProgram(device, computeShader);

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

        return SDL3.SDL_GPUTextureSupportsFormat(device, f, GetTextureType(flags), GetTextureUsage(flags));
    }

    private bool TryGetRenderPipeline(RenderState state, SDLGPUVertexLayout vertexLayout, out SDL_GPUGraphicsPipeline *pipeline)
    {
        if (state.shader == null ||
            state.shaderInstance?.program is not SDLGPUShaderProgram { Type: ShaderType.VertexFragment } shader)
        {
            pipeline = null;

            return false;
        }

        var hash = state.StateKey;

        if (!graphicsPipelines.TryGetValue(hash, out var p))
        {
            unsafe
            {
                var vertexSamplerCount = state.vertexTextures?.Length ?? 0;
                var fragmentSamplerCount = state.fragmentTextures?.Length ?? 0;

                if (vertexSamplerCount < state.shaderInstance.vertexTextureBindings.Count ||
                    fragmentSamplerCount < state.shaderInstance.fragmentTextureBindings.Count)
                {
                    pipeline = null;

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

                    pipeline = null;

                    return false;
                }

                var depthStencilFormat = DepthStencilFormat;

                if (!depthStencilFormat.HasValue ||
                    !TryGetTextureFormat(depthStencilFormat.Value, TextureFlags.DepthStencilTarget,
                        out var sdlDepthFormat))
                {
                    sdlDepthFormat = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM;
                }

                var shaderAttributes = GlobalAllocator<SDL_GPUVertexAttribute>.Instance.Rent(state.shaderInstance.attributes.Length);

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

                        pipeline = null;

                        return false;
                    }

                    var attribute = vertexLayout.attributes[attributeIndex];

                    attributeIndex = state.shaderInstance.attributes.IndexOf(vertexLayout.vertexAttributes[attributeIndex]);

                    ref var currentAttribute = ref shaderAttributes[i];

                    currentAttribute.buffer_slot = 0;
                    currentAttribute.format = attribute.format;
                    currentAttribute.offset = attribute.offset;
                    currentAttribute.location = (uint)attributeIndex;
                }

                var attributesSpan = shaderAttributes.AsSpan();

                fixed (SDL_GPUVertexAttribute* attributes = attributesSpan)
                {
                    var vertexDescription = new SDL_GPUVertexBufferDescription()
                    {
                        pitch = (uint)vertexLayout.Stride,
                        slot = 0,
                        input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    };

                    var sourceBlend = state.sourceBlend switch
                    {
                        BlendMode.DstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_ALPHA,
                        BlendMode.DstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_COLOR,
                        BlendMode.One => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                        BlendMode.OneMinusDstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
                        BlendMode.OneMinusDstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,
                        BlendMode.OneMinusSrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                        BlendMode.OneMinusSrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
                        BlendMode.SrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
                        BlendMode.SrcAlphaSat => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA_SATURATE,
                        BlendMode.SrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_COLOR,
                        BlendMode.Zero => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
                        BlendMode.Off => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_INVALID,
                        _ => throw new ArgumentOutOfRangeException(nameof(state.sourceBlend), "Invalid blend mode"),
                    };

                    var destinationBlend = state.destinationBlend switch
                    {
                        BlendMode.DstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_ALPHA,
                        BlendMode.DstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_COLOR,
                        BlendMode.One => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                        BlendMode.OneMinusDstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
                        BlendMode.OneMinusDstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,
                        BlendMode.OneMinusSrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                        BlendMode.OneMinusSrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
                        BlendMode.SrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
                        BlendMode.SrcAlphaSat => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA_SATURATE,
                        BlendMode.SrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_COLOR,
                        BlendMode.Zero => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
                        BlendMode.Off => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_INVALID,
                        _ => throw new ArgumentOutOfRangeException(nameof(state.destinationBlend), "Invalid blend mode"),
                    };

                    var colorTargetDescriptions = new List<SDL_GPUColorTargetDescription>();

                    if(state.renderTarget == null || state.renderTarget.Disposed)
                    {
                        var colorTargetDescription = new SDL_GPUColorTargetDescription()
                        {
                            format = SDL3.SDL_GetGPUSwapchainTextureFormat(device, window.window),
                            blend_state = new()
                            {
                                enable_blend = state.sourceBlend != BlendMode.Off && state.destinationBlend != BlendMode.Off,
                                color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                                alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                                src_color_blendfactor = sourceBlend,
                                src_alpha_blendfactor = sourceBlend,
                                dst_color_blendfactor = destinationBlend,
                                dst_alpha_blendfactor = destinationBlend,
                            }
                        };

                        colorTargetDescriptions.Add(colorTargetDescription);
                    }
                    else
                    {
                        foreach(var texture in state.renderTarget.colorTextures)
                        {
                            if(texture.Disposed ||
                                !TryGetTextureFormat(texture.impl.Format, state.renderTarget.flags | TextureFlags.ColorTarget,
                                out var textureFormat))
                            {
                                continue;
                            }
                            
                            var colorTargetDescription = new SDL_GPUColorTargetDescription()
                            {
                                format = textureFormat,
                                blend_state = new()
                                {
                                    enable_blend = state.sourceBlend != BlendMode.Off && state.destinationBlend != BlendMode.Off,
                                    color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                                    alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                                    src_color_blendfactor = sourceBlend,
                                    src_alpha_blendfactor = sourceBlend,
                                    dst_color_blendfactor = destinationBlend,
                                    dst_alpha_blendfactor = destinationBlend,
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

                    fixed(SDL_GPUColorTargetDescription *colorTargetDescriptionsPtr = CollectionsMarshal.AsSpan(colorTargetDescriptions))
                    {
                        SDL_GPUVertexBufferDescription[] vertexDescriptions = [vertexDescription];

                        fixed (SDL_GPUVertexBufferDescription* descriptions = vertexDescriptions)
                        {
                            var info = new SDL_GPUGraphicsPipelineCreateInfo()
                            {
                                primitive_type = state.primitiveType switch
                                {
                                    MeshTopology.TriangleStrip => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLESTRIP,
                                    MeshTopology.Triangles => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
                                    MeshTopology.Lines => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINELIST,
                                    MeshTopology.LineStrip => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINESTRIP,
                                    _ => throw new ArgumentOutOfRangeException("Invalid value for primitive type", nameof(state.primitiveType)),
                                },
                                vertex_shader = shader.vertex,
                                fragment_shader = shader.fragment,
                                rasterizer_state = new()
                                {
                                    cull_mode = state.cull switch
                                    {
                                        CullingMode.None => SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE,
                                        CullingMode.Front => SDL_GPUCullMode.SDL_GPU_CULLMODE_FRONT,
                                        CullingMode.Back => SDL_GPUCullMode.SDL_GPU_CULLMODE_BACK,
                                        _ => throw new ArgumentOutOfRangeException("Invalid value for cull", nameof(state.cull)),
                                    },
                                    fill_mode = state.wireframe ? SDL_GPUFillMode.SDL_GPU_FILLMODE_LINE :
                                        SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL,
                                    front_face = SDL_GPUFrontFace.SDL_GPU_FRONTFACE_CLOCKWISE,
                                },
                                depth_stencil_state = new()
                                {
                                    enable_depth_test = state.enableDepth,
                                    enable_depth_write = state.depthWrite,
                                    compare_op = SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS_OR_EQUAL,
                                },
                                vertex_input_state = new()
                                {
                                    num_vertex_buffers = 1,
                                    num_vertex_attributes = (uint)attributesSpan.Length,
                                    vertex_attributes = attributes,
                                    vertex_buffer_descriptions = descriptions,
                                },
                                target_info = new()
                                {
                                    num_color_targets = (uint)colorTargetDescriptions.Count,
                                    color_target_descriptions = colorTargetDescriptionsPtr,
                                    has_depth_stencil_target = depthStencilFormat.HasValue,
                                    depth_stencil_format = sdlDepthFormat,
                                },
                            };

                            pipeline = SDL3.SDL_CreateGPUGraphicsPipeline(device, &info);

                            if(pipeline == null)
                            {
                                pipeline = null;

                                return false;
                            }

                            if (pipeline == null)
                            {
                                pipeline = null;

                                return false;
                            }

                            p = new(pipeline);

                            graphicsPipelines.Add(hash, p);
                        }
                    }

                    GlobalAllocator<SDL_GPUVertexAttribute>.Instance.Return(shaderAttributes);
                }
            }
        }

        pipeline = p.ptr;

        return pipeline != null;
    }

    private bool TryGetStaticMeshRenderPipeline(RenderState state, out SDL_GPUGraphicsPipeline *pipeline)
    {
        if (state.shader == null ||
            state.shaderInstance?.program is not SDLGPUShaderProgram { Type: ShaderType.VertexFragment } shader)
        {
            pipeline = null;

            return false;
        }

        var hash = state.StateKey;

        if (!graphicsPipelines.TryGetValue(hash, out var p))
        {
            unsafe
            {
                var vertexSamplerCount = state.vertexTextures?.Length ?? 0;
                var fragmentSamplerCount = state.fragmentTextures?.Length ?? 0;

                if (vertexSamplerCount < state.shaderInstance.vertexTextureBindings.Count ||
                    fragmentSamplerCount < state.shaderInstance.fragmentTextureBindings.Count)
                {
                    pipeline = null;

                    return false;
                }

                var depthStencilFormat = DepthStencilFormat;

                if (!depthStencilFormat.HasValue ||
                    !TryGetTextureFormat(depthStencilFormat.Value, TextureFlags.DepthStencilTarget,
                        out var sdlDepthFormat))
                {
                    sdlDepthFormat = SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM;
                }

                var shaderAttributes = GlobalAllocator<SDL_GPUVertexAttribute>.Instance.Rent(state.shaderInstance.attributes.Length);
                var vertexDescriptions = GlobalAllocator<SDL_GPUVertexBufferDescription>.Instance.Rent(state.shaderInstance.attributes.Length);

                for (var i = 0; i < state.shaderInstance.attributes.Length; i++)
                {
                    var attribute = state.shaderInstance.attributes[i];

                    var format = attribute switch
                    {
                        VertexAttribute.Position or
                            VertexAttribute.Normal or
                            VertexAttribute.Tangent or
                            VertexAttribute.Bitangent => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
                        VertexAttribute.Color0 or
                            VertexAttribute.Color1 or
                            VertexAttribute.Color2 or
                            VertexAttribute.Color3 or
                            VertexAttribute.BlendIndices or
                            VertexAttribute.BlendWeights => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
                        VertexAttribute.TexCoord0 or
                            VertexAttribute.TexCoord1 or
                            VertexAttribute.TexCoord2 or
                            VertexAttribute.TexCoord3 or
                            VertexAttribute.TexCoord4 or
                            VertexAttribute.TexCoord5 or
                            VertexAttribute.TexCoord6 or
                            VertexAttribute.TexCoord7 => SDL_GPUVertexElementFormat.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
                            _ => throw new ArgumentOutOfRangeException(nameof(attribute), $"Static Mesh Render Pipeline: Attribute {attribute} not implemented!"),
                    };

                    shaderAttributes[i] = new()
                    {
                        buffer_slot = (uint)i,
                        format = format,
                        offset = 0,
                        location = (uint)i,
                    };

                    vertexDescriptions[i] = new()
                    {
                        input_rate = SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                        slot = (uint)i,
                        pitch = (uint)BufferAttributeContainer.BufferElementSize(attribute),
                    };
                }

                fixed (SDL_GPUVertexAttribute* attributes = shaderAttributes)
                {
                    var sourceBlend = state.sourceBlend switch
                    {
                        BlendMode.DstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_ALPHA,
                        BlendMode.DstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_COLOR,
                        BlendMode.One => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                        BlendMode.OneMinusDstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
                        BlendMode.OneMinusDstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,
                        BlendMode.OneMinusSrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                        BlendMode.OneMinusSrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
                        BlendMode.SrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
                        BlendMode.SrcAlphaSat => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA_SATURATE,
                        BlendMode.SrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_COLOR,
                        BlendMode.Zero => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
                        BlendMode.Off => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_INVALID,
                        _ => throw new ArgumentOutOfRangeException(nameof(state.sourceBlend), "Invalid blend mode"),
                    };

                    var destinationBlend = state.destinationBlend switch
                    {
                        BlendMode.DstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_ALPHA,
                        BlendMode.DstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_COLOR,
                        BlendMode.One => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                        BlendMode.OneMinusDstAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
                        BlendMode.OneMinusDstColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,
                        BlendMode.OneMinusSrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                        BlendMode.OneMinusSrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
                        BlendMode.SrcAlpha => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
                        BlendMode.SrcAlphaSat => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA_SATURATE,
                        BlendMode.SrcColor => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_COLOR,
                        BlendMode.Zero => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
                        BlendMode.Off => SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_INVALID,
                        _ => throw new ArgumentOutOfRangeException(nameof(state.destinationBlend), "Invalid blend mode"),
                    };

                    var colorTargetDescriptions = new List<SDL_GPUColorTargetDescription>();

                    if (state.renderTarget == null || state.renderTarget.Disposed)
                    {
                        var colorTargetDescription = new SDL_GPUColorTargetDescription()
                        {
                            format = SDL3.SDL_GetGPUSwapchainTextureFormat(device, window.window),
                            blend_state = new()
                            {
                                enable_blend = state.sourceBlend != BlendMode.Off && state.destinationBlend != BlendMode.Off,
                                color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                                alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                                src_color_blendfactor = sourceBlend,
                                src_alpha_blendfactor = sourceBlend,
                                dst_color_blendfactor = destinationBlend,
                                dst_alpha_blendfactor = destinationBlend,
                            }
                        };

                        colorTargetDescriptions.Add(colorTargetDescription);
                    }
                    else
                    {
                        foreach (var texture in state.renderTarget.colorTextures)
                        {
                            if (texture.Disposed ||
                                !TryGetTextureFormat(texture.impl.Format, state.renderTarget.flags | TextureFlags.ColorTarget,
                                out var textureFormat))
                            {
                                continue;
                            }

                            var colorTargetDescription = new SDL_GPUColorTargetDescription()
                            {
                                format = textureFormat,
                                blend_state = new()
                                {
                                    enable_blend = state.sourceBlend != BlendMode.Off && state.destinationBlend != BlendMode.Off,
                                    color_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                                    alpha_blend_op = SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                                    src_color_blendfactor = sourceBlend,
                                    src_alpha_blendfactor = sourceBlend,
                                    dst_color_blendfactor = destinationBlend,
                                    dst_alpha_blendfactor = destinationBlend,
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

                    fixed (SDL_GPUColorTargetDescription* colorTargetDescriptionsPtr = CollectionsMarshal.AsSpan(colorTargetDescriptions))
                    {
                        fixed (SDL_GPUVertexBufferDescription* descriptions = vertexDescriptions)
                        {
                            var info = new SDL_GPUGraphicsPipelineCreateInfo()
                            {
                                primitive_type = state.primitiveType switch
                                {
                                    MeshTopology.TriangleStrip => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLESTRIP,
                                    MeshTopology.Triangles => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
                                    MeshTopology.Lines => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINELIST,
                                    MeshTopology.LineStrip => SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINESTRIP,
                                    _ => throw new ArgumentOutOfRangeException("Invalid value for primitive type", nameof(state.primitiveType)),
                                },
                                vertex_shader = shader.vertex,
                                fragment_shader = shader.fragment,
                                rasterizer_state = new()
                                {
                                    cull_mode = state.cull switch
                                    {
                                        CullingMode.None => SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE,
                                        CullingMode.Front => SDL_GPUCullMode.SDL_GPU_CULLMODE_FRONT,
                                        CullingMode.Back => SDL_GPUCullMode.SDL_GPU_CULLMODE_BACK,
                                        _ => throw new ArgumentOutOfRangeException("Invalid value for cull", nameof(state.cull)),
                                    },
                                    fill_mode = state.wireframe ? SDL_GPUFillMode.SDL_GPU_FILLMODE_LINE :
                                        SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL,
                                    front_face = SDL_GPUFrontFace.SDL_GPU_FRONTFACE_CLOCKWISE,
                                },
                                depth_stencil_state = new()
                                {
                                    enable_depth_test = state.enableDepth,
                                    enable_depth_write = state.depthWrite,
                                    compare_op = SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS_OR_EQUAL,
                                },
                                vertex_input_state = new()
                                {
                                    num_vertex_buffers = (uint)vertexDescriptions.Length,
                                    num_vertex_attributes = (uint)shaderAttributes.Length,
                                    vertex_attributes = attributes,
                                    vertex_buffer_descriptions = descriptions,
                                },
                                target_info = new()
                                {
                                    num_color_targets = (uint)colorTargetDescriptions.Count,
                                    color_target_descriptions = colorTargetDescriptionsPtr,
                                    has_depth_stencil_target = depthStencilFormat.HasValue,
                                    depth_stencil_format = sdlDepthFormat,
                                },
                            };

                            pipeline = SDL3.SDL_CreateGPUGraphicsPipeline(device, &info);

                            if (pipeline == null)
                            {
                                pipeline = null;

                                return false;
                            }

                            p = new(pipeline);

                            graphicsPipelines.Add(hash, p);
                        }
                    }

                    GlobalAllocator<SDL_GPUVertexAttribute>.Instance.Return(shaderAttributes);
                    GlobalAllocator<SDL_GPUVertexBufferDescription>.Instance.Return(vertexDescriptions);
                }
            }
        }

        pipeline = p.ptr;

        return pipeline != null;
    }

    private void GetUniformData(in RenderState state, bool useWorldMatrix, SDLGPUShaderProgram shader, out (int, int) vertexUniformData,
        out (int, int) fragmentUniformData)
    {
        var position = shaderUniformFrameAllocator.position;

        var vertexUniformSpan = shaderUniformFrameAllocator.Allocate(state.shaderInstance.vertexMappings.Count);

        vertexUniformData = (position, vertexUniformSpan.Length);

        position = shaderUniformFrameAllocator.position;

        var fragmentUniformSpan = shaderUniformFrameAllocator.Allocate(state.shaderInstance.fragmentMappings.Count);

        fragmentUniformData = (position, fragmentUniformSpan.Length);

        var counter = 0;

        foreach (var entry in state.shaderInstance.vertexUniformContainers)
        {
            var length = entry.Value.buffer.Length;

            if (entry.Key == StapleRenderDataUniformName)
            {
                if (length != RenderDataByteSize)
                {
                    Log.Error($"[Rendering] Warning: {StapleRenderDataUniformName} shader uniform is of invalid size {length}: "
                        + $"Should be {RenderDataByteSize}!");

                    continue;
                }

                viewData.renderData.world = state.world;
                viewData.renderData.useWorldMatrix = useWorldMatrix;

                unsafe
                {
                    fixed (void* ptr = &viewData.renderData)
                    {
                        var source = new Span<byte>(ptr, RenderDataByteSize);
                        var target = new Span<byte>(entry.Value.buffer);

                        source.CopyTo(target);
                    }
                }
            }

            ref var uniformEntry = ref vertexUniformSpan[counter++];

            if (!ShouldPushVertexUniform(entry.Value.binding, entry.Value.buffer))
            {
                uniformEntry.used = false;

                continue;
            }

            unsafe
            {
                position = frameAllocator.position;

                frameAllocator.Allocate(length);

                fixed (void* source = entry.Value.buffer)
                {
                    fixed (void* target = frameAllocator.buffer)
                    {
                        var p = (byte*)target;

                        p += position;

                        Buffer.MemoryCopy(source, p, length, length);
                    }
                }

                uniformEntry.binding = (byte)entry.Value.binding;
                uniformEntry.position = position;
                uniformEntry.size = length;
                uniformEntry.used = true;
            }
        }

        counter = 0;

        foreach (var entry in state.shaderInstance.fragmentUniformContainers)
        {
            var length = entry.Value.buffer.Length;

            if (entry.Key == StapleFragmentDataUniformName)
            {
                if (length != FragmentRenderDataByteSize)
                {
                    Log.Error($"[Rendering] Warning: {StapleFragmentDataUniformName} shader uniform is of invalid size {length}: "
                        + $"Should be {FragmentRenderDataByteSize}!");

                    continue;
                }

                unsafe
                {
                    viewData.fragmentData.time = Time.time;

                    fixed (void* ptr = &viewData.fragmentData)
                    {
                        var source = new Span<byte>(ptr, FragmentRenderDataByteSize);
                        var target = new Span<byte>(entry.Value.buffer);

                        source.CopyTo(target);
                    }
                }
            }

            ref var uniformEntry = ref fragmentUniformSpan[counter++];

            if (!ShouldPushFragmentUniform(entry.Value.binding, entry.Value.buffer))
            {
                uniformEntry.used = false;

                continue;
            }

            unsafe
            {
                position = frameAllocator.position;

                frameAllocator.Allocate(entry.Value.buffer.Length);

                fixed (void* source = entry.Value.buffer)
                {
                    fixed (void* target = frameAllocator.buffer)
                    {
                        var p = (byte*)target;

                        p += position;

                        Buffer.MemoryCopy(source, p, length, length);
                    }
                }

                uniformEntry.binding = (byte)entry.Value.binding;
                uniformEntry.position = position;
                uniformEntry.size = length;
                uniformEntry.used = true;
            }
        }
    }

    public void Render(RenderState state)
    {
        state.renderTarget = currentRenderTarget;

        var vertex = (SDLGPUVertexBuffer)state.vertexBuffer;
        var vertexLayout = (SDLGPUVertexLayout)vertex?.layout;
        SDL_GPUGraphicsPipeline *pipeline;

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

        CheckQueuedGraphicsPipeline(pipeline);

        GetUniformData(in state, true, shader, out var vertexUniformData, out var fragmentUniformData);

        AddCommand(new SDLGPURenderCommand(this, state, pipeline, state.vertexTextures, state.fragmentTextures,
            state.shaderInstance.entityTransformsBufferBinding, vertexUniformData, fragmentUniformData, state.shaderInstance.attributes));
    }

    public void RenderStatic(RenderState state, Span<MultidrawEntry> entries)
    {
        if(entries.Length == 0 ||
            state.shader == null ||
            state.shaderInstance?.program is not SDLGPUShaderProgram { Type: ShaderType.VertexFragment } shader ||
            !TryGetStaticMeshRenderPipeline(state, out var pipeline))
        {
            return;
        }

        state.renderTarget = currentRenderTarget;

        CheckQueuedGraphicsPipeline(pipeline);

        GetUniformData(in state, false, shader, out var vertexUniformData, out var fragmentUniformData);

        var commandCount = 0;

        foreach(var entry in entries)
        {
            if(entry.transforms.Count == 0)
            {
                continue;
            }

            commandCount++;
        }

        if(indirectCommandPosition + commandCount > indirectCommands.Length)
        {
            Array.Resize(ref indirectCommands, indirectCommandPosition + commandCount);
        }

        var instanceCount = 0;

        foreach(var entry in entries)
        {
            instanceCount += entry.transforms.Count;
        }

        if (indirectCommandInstance + instanceCount > indirectEntityIndices.Length)
        {
            Array.Resize(ref indirectEntityIndices, indirectCommandInstance + instanceCount);
        }

        var commandIndex = 0;

        for(var i = 0; i < entries.Length; i++)
        {
            ref var entry = ref entries[i];

            if(entry.transforms.Count == 0)
            {
                continue;
            }

            ref var command = ref indirectCommands[indirectCommandPosition + commandIndex++];

            var indices = (uint)entry.entries.indicesEntry.length;
            var instances = (uint)entry.transforms.Count;
            var firstInstance = (uint)indirectCommandInstance;
            var firstIndex = (uint)entry.entries.indicesEntry.start;

            if(!needsIndirectBufferUpdate)
            {
                needsIndirectBufferUpdate |= command.num_indices != indices ||
                    command.num_instances != instances ||
                    command.first_instance != firstInstance ||
                    command.first_index != firstIndex;
            }

            command.num_indices = indices;
            command.num_instances = instances;
            command.first_instance = firstInstance;
            command.first_index = firstIndex;

            for (var j = 0; j < entry.transforms.Count; j++)
            {
                if(indirectCommandInstance >= indirectEntityIndices.Length)
                {
                    continue;
                }

                ref var entityIndex = ref indirectEntityIndices[indirectCommandInstance++];

                var index = (uint)(entry.transforms[j].Entity.Identifier.ID - 1);

                if (!needsIndirectBufferUpdate)
                {
                    needsIndirectBufferUpdate |= entityIndex != index;
                }

                entityIndex = index;
            }
        }

        AddCommand(new SDLGPURenderStaticCommand(this, state, pipeline, state.vertexTextures, state.fragmentTextures,
            state.shaderInstance.entityTransformsBufferBinding, vertexUniformData, fragmentUniformData, state.shaderInstance.attributes,
            indirectCommandPosition, commandIndex));

        indirectCommandPosition += commandIndex;
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

        CheckQueuedGraphicsPipeline(pipeline);

        if (!transientBuffers.TryGetValue(layout, out var entry))
        {
            entry = new()
            {
                backend = this,
            };

            transientBuffers.Add(layout, entry);
        }

        var vertexArray = GlobalAllocator<byte>.Instance.Rent(size * vertices.Length);

        unsafe
        {
            fixed (void* ptr = vertexArray)
            {
                var target = new Span<T>(ptr, vertices.Length);

                vertices.CopyTo(target);
            }
        }

        entry.vertices.AddRange(vertexArray);

        GlobalAllocator<byte>.Instance.Return(vertexArray);

        entry.indices.AddRange(indices);

        state.startVertex = entry.startVertex;
        state.startIndex = entry.startIndex;
        state.indexCount = indices.Length;

        entry.startVertex += vertices.Length;
        entry.startIndex += indices.Length;

        GetUniformData(in state, true, shader, out var vertexUniformData, out var fragmentUniformData);

        AddCommand(new SDLGPURenderTransientCommand(this, state, pipeline, state.vertexTextures, state.fragmentTextures,
            state.shaderInstance.entityTransformsBufferBinding, vertexUniformData, fragmentUniformData, entry));
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

        CheckQueuedGraphicsPipeline(pipeline);

        if (!transientBuffers.TryGetValue(layout, out var entry))
        {
            entry = new()
            {
                backend = this,
            };

            transientBuffers.Add(layout, entry);
        }

        var vertexArray = GlobalAllocator<byte>.Instance.Rent(size * vertices.Length);

        unsafe
        {
            fixed (void* ptr = vertexArray)
            {
                var target = new Span<T>(ptr, vertices.Length);

                vertices.CopyTo(target);
            }
        }

        entry.vertices.AddRange(vertexArray);

        GlobalAllocator<byte>.Instance.Return(vertexArray);

        entry.uintIndices.AddRange(indices);

        state.startVertex = entry.startVertex;
        state.startIndex = entry.startIndexUInt;
        state.indexCount = indices.Length;

        entry.startVertex += vertices.Length;
        entry.startIndexUInt += indices.Length;

        GetUniformData(in state, true, shader, out var vertexUniformData, out var fragmentUniformData);

        AddCommand(new SDLGPURenderTransientUIntCommand(this, state, pipeline, state.vertexTextures, state.fragmentTextures,
            state.shaderInstance.entityTransformsBufferBinding, vertexUniformData, fragmentUniformData, entry));
    }

    internal bool BeginCopyPass()
    {
        unsafe
        {
            if (renderPass != null)
            {
                FinishPasses();
            }

            if (copyPass == null)
            {
                copyPass = SDL3.SDL_BeginGPUCopyPass(commandBuffer);
            }

            return copyPass != null;
        }
    }
}
