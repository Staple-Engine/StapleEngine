using SDL3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Internal;

internal partial class SDLGPURendererBackend : IRendererBackend
{
    internal static string StapleRenderDataUniformName = "StapleRenderData";
    internal static string StapleFragmentDataUniformName = "StapleFragmentRenderData";

    #region Classes
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct StapleRenderData
    {
        public Matrix4x4 world;
        public Matrix4x4 view;
        public Matrix4x4 projection;
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
            SDL.SDL_WaitForGPUIdle(backend.device);

            if(vertexBuffer != nint.Zero)
            {
                SDL.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);

                vertexBuffer = nint.Zero;
            }

            if (indexBuffer != nint.Zero)
            {
                SDL.SDL_ReleaseGPUBuffer(backend.device, indexBuffer);

                indexBuffer = nint.Zero;
            }

            if (uintIndexBuffer != nint.Zero)
            {
                SDL.SDL_ReleaseGPUBuffer(backend.device, uintIndexBuffer);

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
                var usageFlags = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX;

                var createInfo = new SDL.SDL_GPUBufferCreateInfo()
                {
                    size = (uint)vertices.Count,
                    usage = usageFlags,
                };

                vertexBuffer = SDL.SDL_CreateGPUBuffer(backend.device, in createInfo);

                if (vertexBuffer == nint.Zero)
                {
                    return;
                }

                var transferInfo = new SDL.SDL_GPUTransferBufferCreateInfo()
                {
                    size = (uint)vertices.Count,
                    usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
                };

                var transferBuffer = SDL.SDL_CreateGPUTransferBuffer(backend.device, in transferInfo);

                if (transferBuffer == nint.Zero)
                {
                    SDL.SDL_WaitForGPUIdle(backend.device);
                    SDL.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);

                    vertexBuffer = nint.Zero;

                    return;
                }

                if (backend.renderPass != nint.Zero)
                {
                    backend.FinishPasses();
                }

                if (backend.copyPass == nint.Zero)
                {
                    backend.copyPass = SDL.SDL_BeginGPUCopyPass(backend.commandBuffer);
                }

                if (backend.copyPass == nint.Zero)
                {
                    return;
                }

                var mapData = SDL.SDL_MapGPUTransferBuffer(backend.device, transferBuffer, false);

                var from = CollectionsMarshal.AsSpan(vertices);

                unsafe
                {
                    var to = new Span<byte>((void*)mapData, vertices.Count);

                    from.CopyTo(to);
                }

                SDL.SDL_UnmapGPUTransferBuffer(backend.device, transferBuffer);

                var location = new SDL.SDL_GPUTransferBufferLocation()
                {
                    transfer_buffer = transferBuffer,
                    offset = 0,
                };

                var region = new SDL.SDL_GPUBufferRegion()
                {
                    buffer = vertexBuffer,
                    size = (uint)vertices.Count,
                };

                SDL.SDL_UploadToGPUBuffer(backend.copyPass, in location, in region, false);

                SDL.SDL_ReleaseGPUTransferBuffer(backend.device, transferBuffer);
            }

            if(indices.Count > 0)
            {
                var usageFlags = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX;

                var createInfo = new SDL.SDL_GPUBufferCreateInfo()
                {
                    size = (uint)(indices.Count * sizeof(ushort)),
                    usage = usageFlags,
                };

                indexBuffer = SDL.SDL_CreateGPUBuffer(backend.device, in createInfo);

                if (indexBuffer == nint.Zero)
                {
                    return;
                }

                var transferInfo = new SDL.SDL_GPUTransferBufferCreateInfo()
                {
                    size = (uint)(indices.Count * sizeof(ushort)),
                    usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
                };

                var transferBuffer = SDL.SDL_CreateGPUTransferBuffer(backend.device, in transferInfo);

                if (transferBuffer == nint.Zero)
                {
                    SDL.SDL_WaitForGPUIdle(backend.device);
                    SDL.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);
                    SDL.SDL_ReleaseGPUBuffer(backend.device, indexBuffer);

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
                    backend.copyPass = SDL.SDL_BeginGPUCopyPass(backend.commandBuffer);
                }

                if (backend.copyPass == nint.Zero)
                {
                    return;
                }

                var mapData = SDL.SDL_MapGPUTransferBuffer(backend.device, transferBuffer, false);

                var from = CollectionsMarshal.AsSpan(indices);

                unsafe
                {
                    var to = new Span<ushort>((void*)mapData, indices.Count);

                    from.CopyTo(to);
                }

                SDL.SDL_UnmapGPUTransferBuffer(backend.device, transferBuffer);

                var location = new SDL.SDL_GPUTransferBufferLocation()
                {
                    transfer_buffer = transferBuffer,
                    offset = 0,
                };

                var region = new SDL.SDL_GPUBufferRegion()
                {
                    buffer = indexBuffer,
                    size = (uint)(indices.Count * sizeof(ushort)),
                };

                SDL.SDL_UploadToGPUBuffer(backend.copyPass, in location, in region, false);

                SDL.SDL_ReleaseGPUTransferBuffer(backend.device, transferBuffer);
            }

            if (uintIndices.Count > 0)
            {
                var usageFlags = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX;

                var createInfo = new SDL.SDL_GPUBufferCreateInfo()
                {
                    size = (uint)(uintIndices.Count * sizeof(uint)),
                    usage = usageFlags,
                };

                uintIndexBuffer = SDL.SDL_CreateGPUBuffer(backend.device, in createInfo);

                if (uintIndexBuffer == nint.Zero)
                {
                    return;
                }

                var transferInfo = new SDL.SDL_GPUTransferBufferCreateInfo()
                {
                    size = (uint)(uintIndices.Count * sizeof(uint)),
                    usage = SDL.SDL_GPUTransferBufferUsage.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
                };

                var transferBuffer = SDL.SDL_CreateGPUTransferBuffer(backend.device, in transferInfo);

                if (transferBuffer == nint.Zero)
                {
                    SDL.SDL_WaitForGPUIdle(backend.device);
                    SDL.SDL_ReleaseGPUBuffer(backend.device, vertexBuffer);
                    SDL.SDL_ReleaseGPUBuffer(backend.device, uintIndexBuffer);

                    if (indexBuffer != nint.Zero)
                    {
                        SDL.SDL_ReleaseGPUBuffer(backend.device, indexBuffer);
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
                    backend.copyPass = SDL.SDL_BeginGPUCopyPass(backend.commandBuffer);
                }

                if (backend.copyPass == nint.Zero)
                {
                    return;
                }

                var mapData = SDL.SDL_MapGPUTransferBuffer(backend.device, transferBuffer, false);

                var from = CollectionsMarshal.AsSpan(uintIndices);

                unsafe
                {
                    var to = new Span<uint>((void*)mapData, uintIndices.Count);

                    from.CopyTo(to);
                }

                SDL.SDL_UnmapGPUTransferBuffer(backend.device, transferBuffer);

                var location = new SDL.SDL_GPUTransferBufferLocation()
                {
                    transfer_buffer = transferBuffer,
                    offset = 0,
                };

                var region = new SDL.SDL_GPUBufferRegion()
                {
                    buffer = uintIndexBuffer,
                    size = (uint)(uintIndices.Count * sizeof(uint)),
                };

                SDL.SDL_UploadToGPUBuffer(backend.copyPass, in location, in region, false);

                SDL.SDL_ReleaseGPUTransferBuffer(backend.device, transferBuffer);
            }
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

    private SDL3RenderWindow window;
    private readonly Dictionary<int, nint> graphicsPipelines = [];
    private readonly Dictionary<TextureFlags, nint> textureSamplers = [];
    private bool needsDepthTextureUpdate = false;
    private readonly List<IRenderCommand> commands = [];
    private readonly List<(SDLGPUTexture, Action<byte[]>)> readTextureQueue = [];

    private readonly BufferResource[] vertexBuffers = new BufferResource[ushort.MaxValue - 1];
    private readonly BufferResource[] indexBuffers = new BufferResource[ushort.MaxValue - 1];
    private readonly Dictionary<VertexLayout, TransientEntry> transientBuffers = [];
    private readonly TextureResource[] textures = new TextureResource[ushort.MaxValue - 1];

    private bool iteratingCommands = false;
    private int commandIndex;
    #endregion

    public bool SupportsTripleBuffering => SDL.SDL_WindowSupportsGPUPresentMode(device, window.window,
        SDL.SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX);

    public bool SupportsHDRColorSpace => SDL.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2084) ||
        SDL.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR);

    public bool SupportsLinearColorSpace => SDL.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR);

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

    public bool Initialize(RendererType renderer, bool debug, IRenderWindow window, RenderModeFlags renderFlags)
    {
        if(window is not SDL3RenderWindow w)
        {
            return false;
        }

#if DEBUG
        SDL.SDL_SetLogPriority((int)SDL.SDL_LogCategory.SDL_LOG_CATEGORY_GPU, SDL.SDL_LogPriority.SDL_LOG_PRIORITY_VERBOSE);
#endif

        this.window = w;

        SDL.SDL_GPUShaderFormat shaderFormats = 0;

        switch(renderer)
        {
            case RendererType.Vulkan:

                shaderFormats |= SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV;

                break;

            case RendererType.Direct3D12:

                shaderFormats |= SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXBC |
                    SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL;

                break;

            case RendererType.Metal:

                shaderFormats |= SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL |
                    SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_METALLIB;

                break;
        }

        device = SDL.SDL_CreateGPUDevice(shaderFormats, debug, renderer switch
        {
            RendererType.Metal => "metal",
            RendererType.Direct3D12 => "direct3d12",
            RendererType.Vulkan => "vulkan",
            _ => "vulkan",
        });

        if(device == nint.Zero)
        {
            return false;
        }

        if(DepthStencilFormat.HasValue == false)
        {
            SDL.SDL_DestroyGPUDevice(device);

            device = nint.Zero;

            return false;
        }

        if(SDL.SDL_ClaimWindowForGPUDevice(device, w.window) == false)
        {
            SDL.SDL_DestroyGPUDevice(device);

            device = nint.Zero;

            return false;
        }

        UpdateRenderMode(renderFlags);

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

        var swapchainComposition = SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR;
        var presentMode = SDL.SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_IMMEDIATE;

        if(flags.HasFlag(RenderModeFlags.Vsync))
        {
            presentMode = SDL.SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_VSYNC;
        }

        if(flags.HasFlag(RenderModeFlags.TripleBuffering) && SupportsTripleBuffering)
        {
            presentMode = SDL.SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX;
        }

        if(flags.HasFlag(RenderModeFlags.HDR10) && SupportsHDRColorSpace)
        {
            if(flags.HasFlag(RenderModeFlags.sRGB))
            {
                swapchainComposition = SDL.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
                    SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR) ?
                    SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR :

                    SDL.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
                    SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR) ?
                    SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR :
                    SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR;
            }
            else
            {
                swapchainComposition =
                    SDL.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
                    SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2084) ?
                    SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2084 :
                    SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR;
            }
        }
        else
        {
            if (flags.HasFlag(RenderModeFlags.sRGB) && SupportsLinearColorSpace)
            {
                swapchainComposition = SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR;
            }
        }

        SDL.SDL_SetGPUSwapchainParameters(device, window.window, swapchainComposition, presentMode);

        SDL.SDL_GetWindowSizeInPixels(window.window, out var w, out var h);

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
        if(device != nint.Zero)
        {
            SDL.SDL_WaitForGPUIdle(device);

            foreach(var pair in graphicsPipelines)
            {
                SDL.SDL_ReleaseGPUGraphicsPipeline(device, pair.Value);
            }

            graphicsPipelines.Clear();

            foreach(var pair in textureSamplers)
            {
                SDL.SDL_ReleaseGPUSampler(device, pair.Value);
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

            depthTexture?.Destroy();

            depthTexture = null;

            needsDepthTextureUpdate = true;

            SDL.SDL_ReleaseWindowFromGPUDevice(device, window.window);

            SDL.SDL_DestroyGPUDevice(device);

            device = nint.Zero;
        }
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

        commandBuffer = SDL.SDL_AcquireGPUCommandBuffer(device);

        if (commandBuffer == nint.Zero)
        {
            return;
        }

        if (SDL.SDL_WaitAndAcquireGPUSwapchainTexture(commandBuffer, window.window, out swapchainTexture,
            out var w, out var h) == false)
        {
            SDL.SDL_CancelGPUCommandBuffer(commandBuffer);

            commandBuffer = nint.Zero;

            return;
        }

        swapchainWidth = (int)w;
        swapchainHeight = (int)h;

        UpdateDepthTextureIfNeeded(false);
    }

    public void EndFrame()
    {
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

        foreach(var pair in transientBuffers)
        {
            pair.Value.Clear();
        }

        if (commandBuffer != nint.Zero)
        {
            var fences = new nint[SDL.SDL_SubmitGPUCommandBufferAndAcquireFence(commandBuffer)];

            if(SDL.SDL_WaitForGPUFences(device, true, fences.AsSpan(), (uint)fences.Length) == false)
            {
                Log.Error($"[SDL GPU] Failed to wait for GPU Fences: {SDL.SDL_GetError()}");
            }

            SDL.SDL_ReleaseGPUFence(device, fences[0]);

            for(var i = readTextureQueue.Count - 1; i >= 0; i--)
            {
                var item = readTextureQueue[i];

                readTextureQueue.RemoveAt(i);

                if (item.Item1 == null ||
                    item.Item1.Disposed ||
                    TryGetTexture(item.Item1.handle, out var resource) == false ||
                    resource.used == false ||
                    resource.transferBuffer == nint.Zero)
                {
                    continue;
                }

                unsafe
                {
                    var buffer = new byte[resource.length];

                    var map = SDL.SDL_MapGPUTransferBuffer(device, resource.transferBuffer, false);

                    var from = new Span<byte>((void *)map, buffer.Length);
                    var to = new Span<byte>(buffer);

                    from.CopyTo(to);

                    SDL.SDL_UnmapGPUTransferBuffer(device, resource.transferBuffer);

                    resource.transferBuffer = nint.Zero;

                    item.Item2?.Invoke(buffer);
                }
            }

            commandBuffer = nint.Zero;
        }
    }

    internal void UpdateDepthTextureIfNeeded(bool force)
    {
        if((needsDepthTextureUpdate == false && force == false) ||
            swapchainWidth == 0 ||
            swapchainHeight == 0 ||
            DepthStencilFormat.HasValue == false)
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
            SDL.SDL_EndGPUCopyPass(copyPass);

            copyPass = nint.Zero;
        }

        if(renderPass != nint.Zero)
        {
            SDL.SDL_EndGPURenderPass(renderPass);

            renderPass = nint.Zero;
        }
    }

    internal void ResumeRenderPass()
    {
        FinishPasses();

        var texture = nint.Zero;
        var width = 0;
        var height = 0;

        SDLGPUTexture depthTexture = null;

        if (viewData.renderTarget == null)
        {
            texture = swapchainTexture;
            width = swapchainWidth;
            height = swapchainHeight;

            depthTexture = this.depthTexture as SDLGPUTexture;

            if (depthTexture == null)
            {
                UpdateDepthTextureIfNeeded(true);

                depthTexture = depthTexture as SDLGPUTexture;
            }
        }
        else
        {
            //TODO: texture

            width = viewData.renderTarget.width;
            height = viewData.renderTarget.height;

            return;
        }

        if (texture == nint.Zero ||
            (depthTexture?.Disposed ?? true) ||
            TryGetTexture(depthTexture.handle, out var depthTextureResource) == false)
        {
            return;
        }

        var colorTarget = new SDL.SDL_GPUColorTargetInfo()
        {
            load_op = SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
            store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            texture = texture,
        };

        var depthTarget = new SDL.SDL_GPUDepthStencilTargetInfo()
        {
            clear_depth = 1,
            load_op = viewData.clearMode switch
            {
                CameraClearMode.None => SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                _ => SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            },
            store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            texture = depthTextureResource.texture,
            stencil_load_op = viewData.clearMode switch
            {
                CameraClearMode.None => SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                _ => SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            },
            stencil_store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };

        renderPass = SDL.SDL_BeginGPURenderPass(commandBuffer, [colorTarget], 1, in depthTarget);

        if (renderPass == nint.Zero)
        {
            return;
        }

        var viewportData = new SDL.SDL_GPUViewport()
        {
            x = (int)(viewData.viewport.X * width),
            y = (int)(viewData.viewport.Y * height),
            w = (int)(viewData.viewport.Z * width),
            h = (int)(viewData.viewport.W * height),
            min_depth = 0,
            max_depth = 1,
        };

        SDL.SDL_SetGPUViewport(renderPass, in viewportData);
    }

    public void BeginRenderPass(RenderTarget target, CameraClearMode clear, Color clearColor, Vector4 viewport,
        in Matrix4x4 view, in Matrix4x4 projection)
    {
        viewData.renderTarget = target;
        viewData.clearMode = clear;
        viewData.clearColor = clearColor;
        viewData.viewport = viewport;
        viewData.renderData.view = view;
        viewData.renderData.projection = projection;

        AddCommand(new SDLGPUBeginRenderPassCommand(target, clear, clearColor, viewport, view, projection));
    }

    public IShaderProgram CreateShaderVertexFragment(byte[] vertex, byte[] fragment,
        VertexFragmentShaderMetrics vertexMetrics, VertexFragmentShaderMetrics fragmentMetrics,
        VertexAttribute[] vertexAttributes, ShaderUniformContainer vertexUniforms, ShaderUniformContainer fragmentUniforms)
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
                    var info = new SDL.SDL_GPUShaderCreateInfo()
                    {
                        code = v,
                        code_size = (uint)vertex.Length,
                        entrypoint = entry,
                        format = RenderWindow.CurrentRenderer switch
                        {
                            RendererType.Direct3D12 => SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL,
                            RendererType.Metal => SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
                            _ => SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
                        },
                        stage = SDL.SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX,
                        num_samplers = (uint)vertexMetrics.samplerCount,
                        num_storage_buffers = (uint)vertexMetrics.storageBufferCount,
                        num_storage_textures = (uint)vertexMetrics.storageTextureCount,
                        num_uniform_buffers = (uint)vertexMetrics.uniformBufferCount,
                    };

                    vertexShader = SDL.SDL_CreateGPUShader(device, in info);

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
                    var info = new SDL.SDL_GPUShaderCreateInfo()
                    {
                        code = f,
                        code_size = (uint)fragment.Length,
                        entrypoint = entry,
                        format = RenderWindow.CurrentRenderer switch
                        {
                            RendererType.Direct3D12 => SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL,
                            RendererType.Metal => SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
                            _ => SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
                        },
                        stage = SDL.SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
                        num_samplers = (uint)fragmentMetrics.samplerCount,
                        num_storage_buffers = (uint)fragmentMetrics.storageBufferCount,
                        num_storage_textures = (uint)fragmentMetrics.storageTextureCount,
                        num_uniform_buffers = (uint)fragmentMetrics.uniformBufferCount,
                    };

                    fragmentShader = SDL.SDL_CreateGPUShader(device, in info);

                    if (fragmentShader == nint.Zero)
                    {
                        SDL.SDL_ReleaseGPUShader(device, vertexShader);

                        return null;
                    }
                }
            }

            return new SDLGPUShaderProgram(device, vertexShader, fragmentShader, vertexAttributes, vertexUniforms, fragmentUniforms);
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
                    var info = new SDL.SDL_GPUComputePipelineCreateInfo()
                    {
                        code = c,
                        code_size = (uint)compute.Length,
                        entrypoint = e,
                        format = RenderWindow.CurrentRenderer switch
                        {
                            RendererType.Direct3D12 => SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_DXIL,
                            RendererType.Metal => SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_MSL,
                            _ => SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
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

                    computeShader = SDL.SDL_CreateGPUComputePipeline(device, in info);

                    if (computeShader == nint.Zero)
                    {
                        return null;
                    }
                }
            }

            var uniformValues = new Dictionary<byte, byte[]>();

            foreach (var item in uniforms.uniforms)
            {
                uniformValues.Add((byte)item.binding, new byte[item.size]);
            }

            return new SDLGPUShaderProgram(device, computeShader, uniforms, uniformValues);
        }
    }

    public bool SupportsTextureFormat(TextureFormat format, TextureFlags flags)
    {
        if(TryGetTextureFormat(format, flags, out var f) == false)
        {
            return false;
        }

        return SDL.SDL_GPUTextureSupportsFormat(device, f, GetTextureType(flags),
            GetTextureUsage(flags));
    }

    private bool TryGetRenderPipeline(RenderState state, SDLGPUVertexLayout vertexLayout,
        out SDL.SDL_GPUTextureSamplerBinding[] samplers, out nint pipeline)
    {
        if (state.shader == null ||
            state.shaderVariant == null ||
            state.shader.instances.TryGetValue(state.shaderVariant, out var instance) == false ||
            instance.program is not SDLGPUShaderProgram shader ||
            state.shader == null ||
            shader.Type != ShaderType.VertexFragment)
        {
            samplers = default;
            pipeline = nint.Zero;

            return false;
        }

        var samplerCount = state.textures?.Length ?? 0;

        for (var i = 0; i < samplerCount; i++)
        {
            if (state.textures[i]?.impl is not SDLGPUTexture texture ||
                texture.Disposed ||
                TryGetTexture(texture.handle, out _) == false)
            {
                samplers = default;
                pipeline = nint.Zero;

                return false;
            }
        }

        samplers = samplerCount > 0 ? new SDL.SDL_GPUTextureSamplerBinding[samplerCount] : null;

        if (samplers != null)
        {
            for (var i = 0; i < samplers.Length; i++)
            {
                if (state.textures[i]?.impl is not SDLGPUTexture texture ||
                    texture.Disposed ||
                    TryGetTexture(texture.handle, out var resource) == false)
                {
                    samplers = default;
                    pipeline = nint.Zero;

                    return false;
                }

                samplers[i].texture = resource.texture;
                samplers[i].sampler = GetSampler(texture.flags);
            }
        }

        var depthStencilFormat = DepthStencilFormat;

        if (depthStencilFormat.HasValue == false ||
            TryGetTextureFormat(depthStencilFormat.Value, TextureFlags.DepthStencilTarget,
            out var sdlDepthFormat) == false)
        {
            sdlDepthFormat = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM;
        }

        var hash = state.StateKey;

        if (graphicsPipelines.TryGetValue(hash, out pipeline) == false)
        {
            unsafe
            {
                var shaderAttributes = new List<SDL.SDL_GPUVertexAttribute>();

                for (var i = 0; i < vertexLayout.attributes.Length; i++)
                {
                    var attribute = vertexLayout.attributes[i];

                    var attributeIndex = shader.vertexAttributes.IndexOf(vertexLayout.vertexAttributes[i]);

                    if (attributeIndex < 0)
                    {
                        Log.Error($"Failed to render: vertex attribute {shader.vertexAttributes[i]} was not declared in the vertex layout!");

                        return false;
                    }

                    shaderAttributes.Add(new()
                    {
                        buffer_slot = 0,
                        format = attribute.format,
                        offset = attribute.offset,
                        location = (uint)attributeIndex,
                    });
                }

                var attributesSpan = CollectionsMarshal.AsSpan(shaderAttributes);

                fixed (SDL.SDL_GPUVertexAttribute* attributes = attributesSpan)
                {
                    var vertexDescription = new SDL.SDL_GPUVertexBufferDescription()
                    {
                        pitch = (uint)vertexLayout.Stride,
                        slot = 0,
                        input_rate = SDL.SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    };

                    var sourceBlend = state.sourceBlend switch
                    {
                        BlendMode.DstAlpha => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_ALPHA,
                        BlendMode.DstColor => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_COLOR,
                        BlendMode.One => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                        BlendMode.OneMinusDstAlpha => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
                        BlendMode.OneMinusDstColor => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,
                        BlendMode.OneMinusSrcAlpha => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                        BlendMode.OneMinusSrcColor => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
                        BlendMode.SrcAlpha => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
                        BlendMode.SrcAlphaSat => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA_SATURATE,
                        BlendMode.SrcColor => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_COLOR,
                        BlendMode.Zero => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
                        BlendMode.Off => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_INVALID,
                        _ => throw new ArgumentOutOfRangeException(nameof(state.sourceBlend), "Invalid blend mode"),
                    };

                    var destinationBlend = state.destinationBlend switch
                    {
                        BlendMode.DstAlpha => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_ALPHA,
                        BlendMode.DstColor => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_DST_COLOR,
                        BlendMode.One => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE,
                        BlendMode.OneMinusDstAlpha => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_ALPHA,
                        BlendMode.OneMinusDstColor => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_DST_COLOR,
                        BlendMode.OneMinusSrcAlpha => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                        BlendMode.OneMinusSrcColor => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
                        BlendMode.SrcAlpha => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA,
                        BlendMode.SrcAlphaSat => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_ALPHA_SATURATE,
                        BlendMode.SrcColor => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_SRC_COLOR,
                        BlendMode.Zero => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_ZERO,
                        BlendMode.Off => SDL.SDL_GPUBlendFactor.SDL_GPU_BLENDFACTOR_INVALID,
                        _ => throw new ArgumentOutOfRangeException(nameof(state.destinationBlend), "Invalid blend mode"),
                    };

                    var colorTargetDescription = new SDL.SDL_GPUColorTargetDescription()
                    {
                        format = SDL.SDL_GetGPUSwapchainTextureFormat(device, window.window),
                        blend_state = new()
                        {
                            enable_blend = state.sourceBlend != BlendMode.Off && state.destinationBlend != BlendMode.Off,
                            color_blend_op = SDL.SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                            alpha_blend_op = SDL.SDL_GPUBlendOp.SDL_GPU_BLENDOP_ADD,
                            src_color_blendfactor = sourceBlend,
                            src_alpha_blendfactor = sourceBlend,
                            dst_color_blendfactor = destinationBlend,
                            dst_alpha_blendfactor = destinationBlend,
                        }
                    };

                    SDL.SDL_GPUVertexBufferDescription[] vertexDescriptions = [vertexDescription];

                    fixed (SDL.SDL_GPUVertexBufferDescription* descriptions = vertexDescriptions)
                    {
                        var info = new SDL.SDL_GPUGraphicsPipelineCreateInfo()
                        {
                            primitive_type = state.primitiveType switch
                            {
                                MeshTopology.TriangleStrip => SDL.SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLESTRIP,
                                MeshTopology.Triangles => SDL.SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
                                MeshTopology.Lines => SDL.SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINELIST,
                                MeshTopology.LineStrip => SDL.SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINESTRIP,
                                _ => throw new ArgumentOutOfRangeException("Invalid value for primitive type", nameof(state.primitiveType)),
                            },
                            vertex_shader = shader.vertex,
                            fragment_shader = shader.fragment,
                            rasterizer_state = new()
                            {
                                cull_mode = state.cull switch
                                {
                                    CullingMode.None => SDL.SDL_GPUCullMode.SDL_GPU_CULLMODE_NONE,
                                    CullingMode.Front => SDL.SDL_GPUCullMode.SDL_GPU_CULLMODE_FRONT,
                                    CullingMode.Back => SDL.SDL_GPUCullMode.SDL_GPU_CULLMODE_BACK,
                                    _ => throw new ArgumentOutOfRangeException("Invalid value for cull", nameof(state.cull)),
                                },
                                fill_mode = state.wireframe ? SDL.SDL_GPUFillMode.SDL_GPU_FILLMODE_LINE : SDL.SDL_GPUFillMode.SDL_GPU_FILLMODE_FILL,
                                front_face = SDL.SDL_GPUFrontFace.SDL_GPU_FRONTFACE_CLOCKWISE,
                            },
                            depth_stencil_state = new()
                            {
                                enable_depth_test = state.enableDepth,
                                enable_depth_write = state.depthWrite,
                                compare_op = SDL.SDL_GPUCompareOp.SDL_GPU_COMPAREOP_LESS_OR_EQUAL,
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
                                num_color_targets = 1,
                                color_target_descriptions = &colorTargetDescription,
                                has_depth_stencil_target = state.enableDepth && depthStencilFormat.HasValue,
                                depth_stencil_format = sdlDepthFormat,
                            }
                        };

                        pipeline = SDL.SDL_CreateGPUGraphicsPipeline(device, in info);

                        graphicsPipelines.Add(hash, pipeline);
                    }
                }
            }
        }

        if (pipeline == nint.Zero)
        {
            return false;
        }

        return true;
    }

    public void Render(RenderState state)
    {
        if(state.shader == null ||
            state.shaderVariant == null ||
            state.shader.instances.TryGetValue(state.shaderVariant, out var instance) == false ||
            instance.program is not SDLGPUShaderProgram shader ||
            state.shader == null ||
            shader.Type != ShaderType.VertexFragment ||
            state.vertexBuffer is not SDLGPUVertexBuffer vertex ||
            vertex.layout is not SDLGPUVertexLayout vertexLayout ||
            state.indexBuffer is not SDLGPUIndexBuffer index ||
            TryGetRenderPipeline(state, vertexLayout, out var samplers, out var pipeline) == false)
        {
            return;
        }

        var vertexUniformData = new Dictionary<byte, byte[]>();
        var fragmentUniformData = new Dictionary<byte, byte[]>();

        foreach (var pair in shader.vertexUniforms)
        {
            if(pair.Key.name == StapleRenderDataUniformName)
            {
                if(pair.Value.Length != Marshal.SizeOf<StapleRenderData>())
                {
                    Log.Error($"[Rendering] Warning: {StapleRenderDataUniformName} shader uniform is of invalid size {pair.Value.Length}: "
                        + $"Should be {Marshal.SizeOf<StapleRenderData>()}!");

                    continue;
                }

                unsafe
                {
                    viewData.renderData.world = state.world;

                    fixed (void* ptr = &viewData.renderData)
                    {
                        var source = new Span<byte>(ptr, Marshal.SizeOf<StapleRenderData>());
                        var target = new Span<byte>(pair.Value);

                        source.CopyTo(target);
                    }
                }
            }

            vertexUniformData.AddOrSetKey((byte)pair.Key.binding, pair.Value.ToArray());
        }

        foreach (var pair in shader.fragmentUniforms)
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

            fragmentUniformData.AddOrSetKey((byte)pair.Key.binding, pair.Value.ToArray());
        }

        AddCommand(new SDLGPURenderCommand(state, pipeline, samplers, vertexUniformData, fragmentUniformData, shader));
    }

    public void RenderTransient<T>(Span<T> vertices, VertexLayout layout, Span<ushort> indices, RenderState state)
        where T : unmanaged
    {
        if (layout is not SDLGPUVertexLayout vertexLayout ||
            state.shaderVariant == null ||
            state.shader == null ||
            state.shader.instances.TryGetValue(state.shaderVariant, out var instance) == false ||
            instance.program is not SDLGPUShaderProgram shader ||
            shader.Type != ShaderType.VertexFragment ||
            TryGetRenderPipeline(state, vertexLayout, out var samplers, out var pipeline) == false)
        {
            return;
        }

        var size = Marshal.SizeOf<T>();

        if (size % layout.Stride != 0)
        {
            return;
        }

        if (transientBuffers.TryGetValue(layout, out var entry) == false)
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

        var vertexUniformData = new Dictionary<byte, byte[]>();
        var fragmentUniformData = new Dictionary<byte, byte[]>();

        foreach (var pair in shader.vertexUniforms)
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

                    fixed (void* ptr = &viewData.renderData)
                    {
                        var source = new Span<byte>(ptr, Marshal.SizeOf<StapleRenderData>());
                        var target = new Span<byte>(pair.Value);

                        source.CopyTo(target);
                    }
                }
            }
            else if(pair.Key.name == StapleFragmentDataUniformName)
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

            vertexUniformData.AddOrSetKey((byte)pair.Key.binding, pair.Value.ToArray());
        }

        foreach (var pair in shader.fragmentUniforms)
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

            fragmentUniformData.AddOrSetKey((byte)pair.Key.binding, pair.Value.ToArray());
        }

        AddCommand(new SDLGPURenderTransientCommand(state, pipeline, samplers, vertexUniformData, fragmentUniformData, shader, entry));
    }

    public void RenderTransient<T>(Span<T> vertices, VertexLayout layout, Span<uint> indices, RenderState state)
        where T : unmanaged
    {
        if (layout is not SDLGPUVertexLayout vertexLayout ||
            state.shaderVariant == null ||
            state.shader == null ||
            state.shader.instances.TryGetValue(state.shaderVariant, out var instance) == false ||
            instance.program is not SDLGPUShaderProgram shader ||
            shader.Type != ShaderType.VertexFragment ||
            TryGetRenderPipeline(state, vertexLayout, out var samplers, out var pipeline) == false)
        {
            return;
        }

        var size = Marshal.SizeOf<T>();

        if (size % layout.Stride != 0)
        {
            return;
        }

        if (transientBuffers.TryGetValue(layout, out var entry) == false)
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

        var vertexUniformData = new Dictionary<byte, byte[]>();
        var fragmentUniformData = new Dictionary<byte, byte[]>();

        foreach (var pair in shader.vertexUniforms)
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

                    fixed (void* ptr = &viewData.renderData)
                    {
                        var source = new Span<byte>(ptr, Marshal.SizeOf<StapleRenderData>());
                        var target = new Span<byte>(pair.Value);

                        source.CopyTo(target);
                    }
                }
            }

            vertexUniformData.AddOrSetKey((byte)pair.Key.binding, pair.Value.ToArray());
        }

        foreach (var pair in shader.fragmentUniforms)
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

            fragmentUniformData.AddOrSetKey((byte)pair.Key.binding, pair.Value.ToArray());
        }

        AddCommand(new SDLGPURenderTransientUIntCommand(state, pipeline, samplers, vertexUniformData, fragmentUniformData, shader, entry));
    }
}
