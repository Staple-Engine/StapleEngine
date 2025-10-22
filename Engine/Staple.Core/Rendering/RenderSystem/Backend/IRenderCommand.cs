using System.Numerics;

namespace Staple.Internal;

internal interface IRenderCommand
{
    void Submit();

    void Discard();

    IRenderPass BeginRenderPass(RenderTarget target, CameraClearMode clear, Color clearColor,
        Vector4 viewport, Matrix4x4 view, Matrix4x4 projection);
}
