using SDL;
using System.Numerics;

namespace Staple.Internal;

internal unsafe class SDLGPUBeginRenderPassCommand(SDLGPURendererBackend backend, RenderTarget target, CameraClearMode clearMode,
    Color clearColor, Vector4 viewport, in Matrix4x4 view, in Matrix4x4 projection) : IRenderCommand
{
    public Matrix4x4 view = view;
    public Matrix4x4 projection = projection;

    public void Update()
    {
        if (backend.commandBuffer == null)
        {
            return;
        }

        backend.FinishPasses();

        backend.viewData.renderTarget = target;
        backend.viewData.clearMode = clearMode;
        backend.viewData.clearColor = clearColor;
        backend.viewData.viewport = viewport;
        backend.viewData.renderData.view = view;
        backend.viewData.renderData.projection = projection;

        SDL_GPUTexture *texture = null;
        var width = 0;
        var height = 0;

        SDLGPUTexture depthTexture = null;

        if (target == null || target.Disposed)
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
            if (target.ColorTextureCount > 0 &&
                target.colorTextures[0].impl is SDLGPUTexture t &&
                backend.TryGetTexture(t.handle, out var textureResource))
            {
                texture = textureResource.texture;
            }

            width = target.width;
            height = target.height;

            depthTexture = target.DepthTexture?.impl as SDLGPUTexture;
        }

        if (texture == null ||
            (depthTexture?.Disposed ?? true) ||
            !backend.TryGetTexture(depthTexture.handle, out var depthTextureResource))
        {
            return;
        }

        var colorTarget = new SDL_GPUColorTargetInfo()
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
                CameraClearMode.None or CameraClearMode.Depth => SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                _ => SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            },
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            texture = texture,
        };

        var depthTarget = new SDL_GPUDepthStencilTargetInfo()
        {
            clear_depth = 1,
            load_op = clearMode switch
            {
                CameraClearMode.None => SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                _ => SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            },
            store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
            texture = depthTextureResource.texture,
            stencil_load_op = clearMode switch
            {
                CameraClearMode.None => SDL_GPULoadOp.SDL_GPU_LOADOP_LOAD,
                _ => SDL_GPULoadOp.SDL_GPU_LOADOP_CLEAR,
            },
            stencil_store_op = SDL_GPUStoreOp.SDL_GPU_STOREOP_STORE,
        };

        backend.renderPass = SDL3.SDL_BeginGPURenderPass(backend.commandBuffer, &colorTarget, 1, &depthTarget);

        if (backend.renderPass == null)
        {
            return;
        }

        var viewportData = new SDL_GPUViewport()
        {
            x = (int)(viewport.X * width),
            y = (int)(viewport.Y * height),
            w = (int)(viewport.Z * width),
            h = (int)(viewport.W * height),
            min_depth = 0,
            max_depth = 1,
        };

        SDL3.SDL_SetGPUViewport(backend.renderPass, &viewportData);
    }
}
