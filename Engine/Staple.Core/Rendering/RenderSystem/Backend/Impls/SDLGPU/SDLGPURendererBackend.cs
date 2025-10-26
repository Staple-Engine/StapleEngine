using SDL3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Internal;

internal class SDLGPURendererBackend : IRendererBackend
{
    private nint device;
    private SDL3RenderWindow window;
    private readonly Dictionary<int, nint> graphicsPipelines = [];
    private readonly Dictionary<TextureFlags, nint> textureSamplers = [];
    private Vector2Int renderSize;

    public struct StapleRenderData
    {
        public Matrix4x4 world;
        public Matrix4x4 view;
        public Matrix4x4 projection;
        public float time;
    }

    public bool SupportsTripleBuffering => SDL.SDL_WindowSupportsGPUPresentMode(device, window.window,
        SDL.SDL_GPUPresentMode.SDL_GPU_PRESENTMODE_MAILBOX);

    public bool SupportsHDRColorSpace => SDL.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR10_ST2084) ||
        SDL.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_HDR_EXTENDED_LINEAR);

    public bool SupportsLinearColorSpace => SDL.SDL_WindowSupportsGPUSwapchainComposition(device, window.window,
            SDL.SDL_GPUSwapchainComposition.SDL_GPU_SWAPCHAINCOMPOSITION_SDR_LINEAR);

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

            SDL.SDL_DestroyGPUDevice(device);

            device = nint.Zero;
        }
    }

    public void BeginFrame()
    {
    }

    public void EndFrame()
    {
    }

    public IRenderCommand BeginCommand()
    {
        var commandBuffer = SDL.SDL_AcquireGPUCommandBuffer(device);

        if(commandBuffer == nint.Zero || window.window == nint.Zero)
        {
            return null;
        }

        return new SDLGPURenderCommand(commandBuffer, window.window);
    }

    public VertexBuffer CreateVertexBuffer(Span<byte> data, VertexLayout layout, RenderBufferFlags flags)
    {
        if(layout == null || data.Length == 0)
        {
            return null;
        }

        var outValue = new SDLGPUVertexBuffer(device, flags, layout, () => BeginCommand() as SDLGPURenderCommand);

        outValue.Update(data);

        return outValue.Valid ? outValue : null;
    }

    public VertexBuffer CreateVertexBuffer<T>(Span<T> data, VertexLayout layout, RenderBufferFlags flags) where T: unmanaged
    {
        if (layout == null || data.Length == 0)
        {
            return null;
        }

        var outValue = new SDLGPUVertexBuffer(device, flags, layout, () => BeginCommand() as SDLGPURenderCommand);

        outValue.Update(data);

        return outValue.Valid ? outValue : null;
    }

    public IndexBuffer CreateIndexBuffer(Span<ushort> data, RenderBufferFlags flags)
    {
        var outValue = new SDLGPUIndexBuffer(device, flags, () => BeginCommand() as SDLGPURenderCommand);

        outValue.Update(data);

        return outValue.Valid ? outValue : null;
    }

    public IndexBuffer CreateIndexBuffer(Span<uint> data, RenderBufferFlags flags)
    {
        var outValue = new SDLGPUIndexBuffer(device, flags, () => BeginCommand() as SDLGPURenderCommand);

        outValue.Update(data);

        return outValue.Valid ? outValue : null;
    }

    public VertexLayoutBuilder CreateVertexLayoutBuilder()
    {
        return new SDLGPUVertexLayoutBuilder();
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

    public nint GetSampler(TextureFlags flags)
    {
        var cleanFlags = TextureFlags.None;

        SDL.SDL_GPUSamplerAddressMode GetAddressModeU()
        {
            if(flags.HasFlag(TextureFlags.RepeatU))
            {
                cleanFlags |= TextureFlags.RepeatU;

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT;
            }

            if(flags.HasFlag(TextureFlags.MirrorU))
            {
                cleanFlags |= TextureFlags.MirrorU;

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT;
            }

            if(flags.HasFlag(TextureFlags.ClampU))
            {
                cleanFlags |= TextureFlags.ClampU;

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            }

            return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
        }

        SDL.SDL_GPUSamplerAddressMode GetAddressModeV()
        {
            if (flags.HasFlag(TextureFlags.RepeatV))
            {
                cleanFlags |= TextureFlags.RepeatV;

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT;
            }

            if (flags.HasFlag(TextureFlags.MirrorV))
            {
                cleanFlags |= TextureFlags.MirrorV;

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT;
            }

            if (flags.HasFlag(TextureFlags.ClampV))
            {
                cleanFlags |= TextureFlags.ClampV;

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            }

            return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
        }

        SDL.SDL_GPUSamplerAddressMode GetAddressModeW()
        {
            if (flags.HasFlag(TextureFlags.RepeatW))
            {
                cleanFlags |= TextureFlags.RepeatW;

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_REPEAT;
            }

            if (flags.HasFlag(TextureFlags.MirrorW))
            {
                cleanFlags |= TextureFlags.MirrorW;

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_MIRRORED_REPEAT;
            }

            if (flags.HasFlag(TextureFlags.ClampW))
            {
                cleanFlags |= TextureFlags.ClampW;

                return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
            }

            return SDL.SDL_GPUSamplerAddressMode.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE;
        }

        var anisotropy = false;

        if(flags.HasFlag(TextureFlags.AnisotropicFilter))
        {
            cleanFlags |= TextureFlags.AnisotropicFilter;

            anisotropy = true;
        }

        var uMode = GetAddressModeU();
        var vMode = GetAddressModeV();
        var wMode = GetAddressModeW();

        if (textureSamplers.TryGetValue(cleanFlags, out var sampler) == false)
        {
            var magFilter = flags.HasFlag(TextureFlags.LinearFilter) ? SDL.SDL_GPUFilter.SDL_GPU_FILTER_LINEAR :
                SDL.SDL_GPUFilter.SDL_GPU_FILTER_NEAREST;

            var mipmapMode = flags.HasFlag(TextureFlags.LinearFilter) ? SDL.SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR :
                SDL.SDL_GPUSamplerMipmapMode.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST;

            var info = new SDL.SDL_GPUSamplerCreateInfo()
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

            sampler = SDL.SDL_CreateGPUSampler(device, in info);

            if (sampler != nint.Zero)
            {
                textureSamplers.Add(cleanFlags, sampler);
            }
        }

        return sampler;
    }

    public void Render(IRenderPass pass, RenderState state)
    {
        if(pass is not SDLGPURenderPass renderPass ||
            state.program is not SDLGPUShaderProgram shader ||
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
                texture.Disposed)
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
                    texture.Disposed)
                {
                    return;
                }

                samplers[i].texture = texture.texture;
                samplers[i].sampler = GetSampler(texture.flags);
            }
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
                            }
                        };

                        pipeline = SDL.SDL_CreateGPUGraphicsPipeline(device, in info);
                    }
                }
            }
        }

        if (pipeline == nint.Zero)
        {
            return;
        }

        SDL.SDL_BindGPUGraphicsPipeline(renderPass.renderPass, pipeline);

        var vertexBinding = new SDL.SDL_GPUBufferBinding()
        {
            buffer = vertex.buffer,
        };

        var indexBinding = new SDL.SDL_GPUBufferBinding()
        {
            buffer = index.buffer,
        };

        var scissor = new SDL.SDL_Rect();

        if(state.scissor != default)
        {
            scissor = new()
            {
                x = state.scissor.left,
                y = state.scissor.top,
                w = state.scissor.Width,
                h = state.scissor.Height,
            };
        }
        else
        {
            scissor = new()
            {
                w = renderSize.X,
                h = renderSize.Y,
            };
        }

        SDL.SDL_SetGPUScissor(renderPass.renderPass, in scissor);

        SDL.SDL_BindGPUVertexBuffers(renderPass.renderPass, 0, [vertexBinding], 1);

        SDL.SDL_BindGPUIndexBuffer(renderPass.renderPass, in indexBinding, index.Is32Bit ?
            SDL.SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT :
            SDL.SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT);

        if(samplers != null)
        {
            SDL.SDL_BindGPUFragmentSamplers(renderPass.renderPass, 0, samplers.AsSpan(), (uint)samplers.Length);
        }

        unsafe
        {
            var renderData = new StapleRenderData()
            {
                projection = renderPass.projection,
                view = renderPass.view,
                time = Time.time,
                world = state.world,
            };

            void* ptr = &renderData;

            SDL.SDL_PushGPUVertexUniformData(renderPass.commandBuffer, 0, (nint)ptr, (uint)Marshal.SizeOf<StapleRenderData>());
        }

        SDL.SDL_DrawGPUIndexedPrimitives(renderPass.renderPass, (uint)state.indexCount, 1,
            (uint)state.startIndex, state.startVertex, 0);
    }

    public ITexture CreateTextureAssetTexture(SerializableTexture asset, TextureFlags flags)
    {
        var format = asset.metadata.Format;

        if (SDLGPUTexture.TryGetTextureFormat(format, flags, out var textureFormat) == false)
        {
            return null;
        }

        var info = new SDL.SDL_GPUTextureCreateInfo()
        {
            format = textureFormat,
            width = (uint)asset.width,
            height = (uint)asset.height,
            type = SDLGPUTexture.GetTextureType(flags),
            usage = SDLGPUTexture.GetTextureUsage(flags),
            layer_count_or_depth = 1,
            num_levels = 1,
        };

        var texture = SDL.SDL_CreateGPUTexture(device, in info);

        if (texture == nint.Zero)
        {
            return null;
        }

        var outValue = new SDLGPUTexture(device, texture, asset.width, asset.height, format, flags,
            () => (SDLGPURenderCommand)BeginCommand());

        outValue.Update(asset.data);

        return outValue;
    }

    public ITexture CreatePixelTexture(byte[] data, int width, int height, TextureFormat format, TextureFlags flags)
    {
        if (SDLGPUTexture.TryGetTextureFormat(format, flags, out var textureFormat) == false)
        {
            return null;
        }

        var info = new SDL.SDL_GPUTextureCreateInfo()
        {
            format = textureFormat,
            width = (uint)width,
            height = (uint)height,
            type = SDLGPUTexture.GetTextureType(flags),
            usage = SDLGPUTexture.GetTextureUsage(flags),
            layer_count_or_depth = 1,
            num_levels = 1, //TODO: Support multiple levels
        };

        var texture = SDL.SDL_CreateGPUTexture(device, in info);

        if (texture == nint.Zero)
        {
            return null;
        }

        var outValue = new SDLGPUTexture(device, texture, width, height, format, flags, () => (SDLGPURenderCommand)BeginCommand());

        outValue.Update(data);

        return outValue;
    }

    public ITexture CreateEmptyTexture(int width, int height, TextureFormat format, TextureFlags flags)
    {
        if(SDLGPUTexture.TryGetTextureFormat(format, flags, out var textureFormat) == false)
        {
            return null;
        }

        var info = new SDL.SDL_GPUTextureCreateInfo()
        {
            format = textureFormat,
            width = (uint)width,
            height = (uint)height,
            type = SDLGPUTexture.GetTextureType(flags),
            usage = SDLGPUTexture.GetTextureUsage(flags),
            layer_count_or_depth = 1,
            num_levels = 1,
        };

        var texture = SDL.SDL_CreateGPUTexture(device, in info);

        if(texture == nint.Zero)
        {
            return null;
        }

        return new SDLGPUTexture(device, texture, width, height, format, flags, () => (SDLGPURenderCommand)BeginCommand());
    }
}
