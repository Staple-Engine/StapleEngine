using SDL3;
using System.Numerics;

namespace Staple.Internal;

internal class SDLGPUBeginRenderPassCommand(RenderTarget target, CameraClearMode clearMode, Color clearColor, Vector4 viewport,
        in Matrix4x4 view, in Matrix4x4 projection) : IRenderCommand
{
    public RenderTarget target = target;
    public CameraClearMode clearMode = clearMode;
    public Color clearColor = clearColor;
    public Vector4 viewport = viewport;
    public Matrix4x4 view = view;
    public Matrix4x4 projection = projection;

    public void Update(IRendererBackend rendererBackend)
    {
        if(rendererBackend is not SDLGPURendererBackend backend ||
            backend.commandBuffer == nint.Zero)
        {
            return;
        }

        backend.viewData.renderTarget = target;
        backend.viewData.clearMode = clearMode;
        backend.viewData.clearColor = clearColor;
        backend.viewData.viewport = viewport;
        backend.viewData.renderData.view = view;
        backend.viewData.renderData.projection = projection;

        backend.FinishPasses();

        var texture = nint.Zero;
        var width = 0;
        var height = 0;

        SDLGPUTexture depthTexture = null;

        if (target == null)
        {
            texture = backend.swapchainTexture;
            width = backend.swapchainWidth;
            height = backend.swapchainHeight;

            depthTexture = backend.depthTexture as SDLGPUTexture;

            if (depthTexture == null)
            {
                backend.UpdateDepthTextureIfNeeded(true);

                depthTexture = backend.depthTexture as SDLGPUTexture;
            }
        }
        else
        {
            //TODO: texture

            width = target.width;
            height = target.height;

            return;
        }

        if (texture == nint.Zero ||
            (depthTexture?.Disposed ?? true) ||
            backend.TryGetTexture(depthTexture.handle, out var depthTextureResource) == false)
        {
            return;
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
            load_op = clearMode switch
            {
                CameraClearMode.None or CameraClearMode.Depth => SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                _ => SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            },
            store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            texture = texture,
        };

        var depthTarget = new SDL.SDL_GPUDepthStencilTargetInfo()
        {
            clear_depth = 1,
            load_op = clearMode switch
            {
                CameraClearMode.None => SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                _ => SDL.SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            },
            store_op = SDL.SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            texture = depthTextureResource.texture,
        };

        backend.renderPass = SDL.SDL_BeginGPURenderPass(backend.commandBuffer, [colorTarget], 1, in depthTarget);

        if (backend.renderPass == nint.Zero)
        {
            return;
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

        SDL.SDL_SetGPUViewport(backend.renderPass, in viewportData);
    }
}
