namespace Staple.Internal;

internal interface IRenderCommand
{
    void Submit();

    void Discard();

    IRenderPass BeginRenderPass(RenderTarget target, CameraClearMode clear, Color clearColor);
}
