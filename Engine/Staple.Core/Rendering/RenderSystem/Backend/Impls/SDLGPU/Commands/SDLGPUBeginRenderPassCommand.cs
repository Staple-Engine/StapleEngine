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

        backend.FinishPasses();

        backend.viewData.renderTarget = target;
        backend.viewData.clearMode = clearMode;
        backend.viewData.clearColor = clearColor;
        backend.viewData.viewport = viewport;
        backend.viewData.renderData.view = view;
        backend.viewData.renderData.projection = projection;

        var texture = nint.Zero;
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
            if(target.ColorTextureCount > 0 &&
                target.colorTextures[0].impl is SDLGPUTexture t &&
                backend.TryGetTexture(t.handle, out var textureResource))
            {
                texture = textureResource.texture;
            }

            width = target.width;
            height = target.height;

            depthTexture = target.DepthTexture?.impl as SDLGPUTexture;
        }

        if (texture == nint.Zero ||
            (depthTexture?.Disposed ?? true) ||
            backend.TryGetTexture(depthTexture.handle, out var depthTextureResource) == false)
        {
            return;
        }

        var colorTarget = new SDL.GPUColorTargetInfo()
        {
            ClearColor = new()
            {
                R = clearColor.r,
                G = clearColor.g,
                B = clearColor.b,
                A = clearColor.a,
            },
            LoadOp = clearMode switch
            {
                CameraClearMode.None or CameraClearMode.Depth => SDL.GPULoadOp.Load,
                _ => SDL.GPULoadOp.Clear,
            },
            StoreOp = SDL.GPUStoreOp.Store,
            Texture = texture,
        };

        var depthTarget = new SDL.GPUDepthStencilTargetInfo()
        {
            ClearDepth = 1,
            LoadOp = clearMode switch
            {
                CameraClearMode.None => SDL.GPULoadOp.Load,
                _ => SDL.GPULoadOp.Clear,
            },
            StoreOp = SDL.GPUStoreOp.Store,
            Texture = depthTextureResource.texture,
            StencilLoadOp = clearMode switch
            {
                CameraClearMode.None => SDL.GPULoadOp.Load,
                _ => SDL.GPULoadOp.Clear,
            },
            StencilStoreOp = SDL.GPUStoreOp.Store,
        };

        backend.renderPass = SDL.BeginGPURenderPass(backend.commandBuffer, [colorTarget], 1, in depthTarget);

        if (backend.renderPass == nint.Zero)
        {
            return;
        }

        var viewportData = new SDL.GPUViewport()
        {
            X = (int)(viewport.X * width),
            Y = (int)(viewport.Y * height),
            W = (int)(viewport.Z * width),
            H = (int)(viewport.W * height),
            MinDepth = 0,
            MaxDepth = 1,
        };

        SDL.SetGPUViewport(backend.renderPass, in viewportData);
    }
}
