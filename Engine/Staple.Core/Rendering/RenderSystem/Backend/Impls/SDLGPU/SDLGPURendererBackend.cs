using SDL3;
using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace Staple.Internal;

internal class SDLGPURendererBackend : IRendererBackend
{
    private nint device;
    private SDL3RenderWindow window;

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
    }

    public void UpdateViewport(int width, int height)
    {
    }

    public void Destroy()
    {
        if(device != nint.Zero)
        {
            SDL.SDL_DestroyGPUDevice(device);

            device = nint.Zero;
        }
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

        var usageFlags = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX;

        if(flags.HasFlag(RenderBufferFlags.GraphicsRead))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeRead))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeWrite))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE;
        }

        var createInfo = new SDL.SDL_GPUBufferCreateInfo()
        {
            size = (uint)data.Length,
            usage = usageFlags,
        };

        var buffer = SDL.SDL_CreateGPUBuffer(device, in createInfo);

        if(buffer == nint.Zero)
        {
            return null;
        }

        var outValue = new SDLGPUVertexBuffer(device, buffer, layout, () => BeginCommand() as SDLGPURenderCommand);

        outValue.Update(data);

        return outValue;
    }

    public VertexBuffer CreateVertexBuffer<T>(Span<T> data, VertexLayout layout, RenderBufferFlags flags) where T: unmanaged
    {
        if (layout == null || data.Length == 0)
        {
            return null;
        }

        var usageFlags = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_VERTEX;

        if (flags.HasFlag(RenderBufferFlags.GraphicsRead))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeRead))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeWrite))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE;
        }

        var createInfo = new SDL.SDL_GPUBufferCreateInfo()
        {
            size = (uint)(data.Length * Marshal.SizeOf<T>()),
            usage = usageFlags,
        };

        var buffer = SDL.SDL_CreateGPUBuffer(device, in createInfo);

        if (buffer == nint.Zero)
        {
            return null;
        }

        var outValue = new SDLGPUVertexBuffer(device, buffer, layout, () => BeginCommand() as SDLGPURenderCommand);

        outValue.Update(data);

        return outValue;
    }

    public IndexBuffer CreateIndexBuffer(Span<ushort> data, RenderBufferFlags flags)
    {
        var usageFlags = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX;

        if (flags.HasFlag(RenderBufferFlags.GraphicsRead))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeRead))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeWrite))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE;
        }

        var createInfo = new SDL.SDL_GPUBufferCreateInfo()
        {
            size = (uint)(data.Length * sizeof(ushort)),
            usage = usageFlags,
        };

        var buffer = SDL.SDL_CreateGPUBuffer(device, in createInfo);

        if (buffer == nint.Zero)
        {
            return null;
        }

        var outValue = new SDLGPUIndexBuffer(device, buffer, () => BeginCommand() as SDLGPURenderCommand);

        outValue.Update(data);

        return outValue;
    }

    public IndexBuffer CreateIndexBuffer(Span<uint> data, RenderBufferFlags flags)
    {
        var usageFlags = SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_INDEX;

        if (flags.HasFlag(RenderBufferFlags.GraphicsRead))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_GRAPHICS_STORAGE_READ;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeRead))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_READ;
        }

        if (flags.HasFlag(RenderBufferFlags.ComputeWrite))
        {
            usageFlags |= SDL.SDL_GPUBufferUsageFlags.SDL_GPU_BUFFERUSAGE_COMPUTE_STORAGE_WRITE;
        }

        var createInfo = new SDL.SDL_GPUBufferCreateInfo()
        {
            size = (uint)(data.Length * sizeof(uint)),
            usage = usageFlags,
        };

        var buffer = SDL.SDL_CreateGPUBuffer(device, in createInfo);

        if (buffer == nint.Zero)
        {
            return null;
        }

        var outValue = new SDLGPUIndexBuffer(device, buffer, () => BeginCommand() as SDLGPURenderCommand);

        outValue.Update(data);

        return outValue;
    }

    public VertexLayoutBuilder CreateVertexLayoutBuilder()
    {
        return new SDLGPUVertexLayoutBuilder();
    }

    public IShaderProgram CreateShaderVertexFragment(byte[] vertex, byte[] fragment)
    {
        unsafe
        {
            var entryPointBytes = Encoding.UTF8.GetBytes("main");

            var vertexShader = nint.Zero;
            var fragmentShader = nint.Zero;

            fixed (byte* e = entryPointBytes)
            {
                fixed (byte* v = vertex)
                {
                    var info = new SDL.SDL_GPUShaderCreateInfo()
                    {
                        code = v,
                        code_size = (uint)vertex.Length,
                        entrypoint = e,
                        format = SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
                        stage = SDL.SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_VERTEX,
                    };

                    vertexShader = SDL.SDL_CreateGPUShader(device, in info);

                    if(vertexShader == nint.Zero)
                    {
                        return null;
                    }
                }

                fixed (byte* f = fragment)
                {
                    var info = new SDL.SDL_GPUShaderCreateInfo()
                    {
                        code = f,
                        code_size = (uint)fragment.Length,
                        entrypoint = e,
                        format = SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
                        stage = SDL.SDL_GPUShaderStage.SDL_GPU_SHADERSTAGE_FRAGMENT,
                    };

                    fragmentShader = SDL.SDL_CreateGPUShader(device, in info);

                    if (fragmentShader == nint.Zero)
                    {
                        SDL.SDL_ReleaseGPUShader(device, vertexShader);

                        return null;
                    }
                }
            }

            return new SDLGPUShaderProgram(device, vertexShader, fragmentShader);
        }
    }

    public IShaderProgram CreateShaderCompute(byte[] compute)
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
                        format = SDL.SDL_GPUShaderFormat.SDL_GPU_SHADERFORMAT_SPIRV,
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

    public void Render(IRenderPass pass, RenderState state)
    {
        if(pass is not SDLGPURenderPass renderPass ||
            state.program is not SDLGPUShaderProgram shader ||
            shader.Type != ShaderType.VertexFragment ||
            state.vertexLayout is not SDLGPUVertexLayout vertexLayout ||
            state.vertexBuffer is not SDLGPUVertexBuffer vertex ||
            state.indexBuffer is not SDLGPUIndexBuffer index)
        {
            return;
        }

        unsafe
        {
            fixed(SDL.SDL_GPUVertexAttribute *attributes = vertexLayout.attributes)
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

                fixed(SDL.SDL_GPUVertexBufferDescription *descriptions = vertexDescriptions)
                {
                    var info = new SDL.SDL_GPUGraphicsPipelineCreateInfo()
                    {
                        primitive_type = state.primitiveType switch
                        {
                            MeshTopology.TriangleStrip => SDL.SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLESTRIP,
                            MeshTopology.Triangles => SDL.SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
                            MeshTopology.Lines => SDL.SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINELIST,
                            MeshTopology.LineStrip => SDL.SDL_GPUPrimitiveType.SDL_GPU_PRIMITIVETYPE_LINESTRIP,
                            _ => throw new ArgumentOutOfRangeException(nameof(state.primitiveType), "Invalid value for primitive type"),
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
                                _ => throw new ArgumentOutOfRangeException(nameof(state.cull), "Invalid value for cull"),
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

                    var pipeline = SDL.SDL_CreateGPUGraphicsPipeline(device, in info);

                    if(pipeline == nint.Zero)
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

                    SDL.SDL_BindGPUVertexBuffers(renderPass.renderPass, 0, [vertexBinding], 1);

                    SDL.SDL_BindGPUIndexBuffer(renderPass.renderPass, in indexBinding, index.Is32Bit ?
                        SDL.SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_32BIT :
                        SDL.SDL_GPUIndexElementSize.SDL_GPU_INDEXELEMENTSIZE_16BIT);

                    SDL.SDL_DrawGPUIndexedPrimitives(renderPass.renderPass, (uint)state.indexCount, 1,
                        (uint)state.startIndex, state.startVertex, 0);
                }
            }
        }
    }
}
