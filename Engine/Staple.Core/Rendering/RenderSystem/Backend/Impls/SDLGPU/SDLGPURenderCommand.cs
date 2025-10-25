using SDL3;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Staple.Internal;

internal class SDLGPURenderCommand(nint commandBuffer, nint window) : IRenderCommand
{
    internal nint commandBuffer = commandBuffer;

    private readonly nint window = window;

    public void Submit()
    {
        if (commandBuffer == nint.Zero)
        {
            return;
        }

        SDL.SDL_SubmitGPUCommandBuffer(commandBuffer);

        commandBuffer = nint.Zero;
    }

    public void Discard()
    {
        if (commandBuffer == nint.Zero)
        {
            return;
        }

        SDL.SDL_CancelGPUCommandBuffer(commandBuffer);

        commandBuffer = nint.Zero;
    }

    public IRenderPass BeginRenderPass(RenderTarget target, CameraClearMode clear, Color clearColor, Vector4 viewport,
        Matrix4x4 view, Matrix4x4 projection)
    {
        if(commandBuffer == nint.Zero)
        {
            return null;
        }

        var texture = nint.Zero;
        var width = 0;
        var height = 0;

        if (target == null)
        {
            if (SDL.SDL_WaitAndAcquireGPUSwapchainTexture(commandBuffer, window, out texture, out var w, out var h) == false)
            {
                return null;
            }

            width = (int)w;
            height = (int)h;
        }
        else
        {
            //TODO: texture

            width = target.width;
            height = target.height;

            return null;
        }

        if(texture == nint.Zero)
        {
            return null;
        }

        var colorTarget = new SDL.SDL_GPUColorTargetInfo()
        {
            clear_color = new()
            {
                r = clearColor.r,
                g = clearColor.g,
                b = clearColor.b,
                a = clearColor.a,
            },
            load_op = clear switch
            {
                CameraClearMode.None or CameraClearMode.Depth => SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                _ => SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            },
            store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            texture = texture,
        };

        var renderPass = SDL.SDL_BeginGPURenderPass(commandBuffer, [colorTarget], 1, in Unsafe.NullRef<SDL.SDL_GPUDepthStencilTargetInfo>());

        if(renderPass == nint.Zero)
        {
            return null;
        }

        var viewportData = new SDL.SDL_GPUViewport()
        {
            x = (int)(viewport.X * width),
            y = (int)(viewport.Y * height),
            w = (int)(viewport.Z * width),
            h = (int)(viewport.W * height),
            min_depth = 0,
            max_depth = 1,
        };

        SDL.SDL_SetGPUViewport(renderPass, in viewportData);

        return new SDLGPURenderPass(commandBuffer, renderPass, view, projection);
    }
}
