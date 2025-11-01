using SDL3;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace Staple.Internal;

internal partial class SDLGPURendererBackend : IRendererBackend
{
    [StructLayout(LayoutKind.Sequential, Pack = 0)]
    internal struct StapleRenderData
    {
        public Matrix4x4 world;
        public Matrix4x4 view;
        public Matrix4x4 projection;
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
    }

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

    private readonly BufferResource[] vertexBuffers = new BufferResource[ushort.MaxValue - 1];
    private readonly BufferResource[] indexBuffers = new BufferResource[ushort.MaxValue - 1];
    private readonly TextureResource[] textures = new TextureResource[ushort.MaxValue - 1];

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
        foreach(var command in commands)
        {
            command.Update(this);
        }

        FinishPasses();

        commands.Clear();

        if (commandBuffer != nint.Zero)
        {
            SDL.SDL_SubmitGPUCommandBuffer(commandBuffer);

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

        depthTexture?.Destroy();

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

            depthTexture = depthTexture as SDLGPUTexture;

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
            load_op = SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
            store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            texture = depthTextureResource.texture,
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
        commands.Add(new SDLGPUBeginRenderPassCommand(target, clear, clearColor, viewport, view, projection));
    }

    public IShaderProgram CreateShaderVertexFragment(byte[] vertex, byte[] fragment,
        VertexFragmentShaderMetrics vertexMetrics, VertexFragmentShaderMetrics fragmentMetrics,
        VertexAttribute[] vertexAttributes)
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

            return new SDLGPUShaderProgram(device, vertexShader, fragmentShader, vertexAttributes);
        }
    }

    public IShaderProgram CreateShaderCompute(byte[] compute, ComputeShaderMetrics metrics)
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

            return new SDLGPUShaderProgram(device, computeShader);
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

    public void Render(RenderState state)
    {
        if(state.program is not SDLGPUShaderProgram shader ||
            shader.Type != ShaderType.VertexFragment ||
            state.vertexBuffer is not SDLGPUVertexBuffer vertex ||
            vertex.layout is not SDLGPUVertexLayout vertexLayout ||
            state.indexBuffer is not SDLGPUIndexBuffer index)
        {
            return;
        }

        var samplerCount = state.textures?.Length ?? 0;

        for (var i = 0; i < samplerCount; i++)
        {
            if (state.textures[i]?.impl is not SDLGPUTexture texture ||
                texture.Disposed ||
                TryGetTexture(texture.handle, out _) == false)
            {
                return;
            }
        }

        var samplers = samplerCount > 0 ? new SDL.SDL_GPUTextureSamplerBinding[samplerCount] : null;

        if(samplers != null)
        {
            for (var i = 0; i < samplers.Length; i++)
            {
                if (state.textures[i]?.impl is not SDLGPUTexture texture ||
                    texture.Disposed ||
                    TryGetTexture(texture.handle, out var resource) == false)
                {
                    return;
                }

                samplers[i].texture = resource.texture;
                samplers[i].sampler = GetSampler(texture.flags);
            }
        }

        var depthStencilFormat = DepthStencilFormat;

        if(depthStencilFormat.HasValue == false ||
            TryGetTextureFormat(depthStencilFormat.Value, TextureFlags.DepthStencilTarget,
            out var sdlDepthFormat) == false)
        {
            sdlDepthFormat = SDL.SDL_GPUTextureFormat.SDL_GPU_TEXTUREFORMAT_D24_UNORM;
        }

        var hash = state.StateKey;

        if(graphicsPipelines.TryGetValue(hash, out var pipeline) == false)
        {
            unsafe
            {
                var shaderAttributes = new SDL.SDL_GPUVertexAttribute[vertexLayout.attributes.Length];

                for(var i = 0; i < vertexLayout.attributes.Length; i++)
                {
                    var attribute = vertexLayout.attributes[i];

                    var attributeIndex = shader.vertexAttributes.IndexOf(vertexLayout.vertexAttributes[i]);

                    if(attributeIndex < 0)
                    {
                        Log.Error($"Failed to render: vertex attribute {shader.vertexAttributes[i]} was not declared in the vertex layout!");

                        return;
                    }

                    shaderAttributes[i] = new()
                    {
                        buffer_slot = 0,
                        format = attribute.format,
                        offset = attribute.offset,
                        location = (uint)attributeIndex,
                    };
                }

                fixed (SDL.SDL_GPUVertexAttribute* attributes = shaderAttributes)
                {
                    var vertexDescription = new SDL.SDL_GPUVertexBufferDescription()
                    {
                        pitch = (uint)vertexLayout.Stride,
                        slot = 0,
                        input_rate = SDL.SDL_GPUVertexInputRate.SDL_GPU_VERTEXINPUTRATE_VERTEX
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
                                num_vertex_attributes = (uint)vertexLayout.attributes.Length,
                                vertex_attributes = attributes,
                                vertex_buffer_descriptions = descriptions,
                            },
                            target_info = new()
                            {
                                num_color_targets = 1,
                                color_target_descriptions = &colorTargetDescription,
                                has_depth_stencil_target = depthStencilFormat.HasValue,
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
            return;
        }

        commands.Add(new SDLGPURenderCommand(state, pipeline, samplers));
    }
}
