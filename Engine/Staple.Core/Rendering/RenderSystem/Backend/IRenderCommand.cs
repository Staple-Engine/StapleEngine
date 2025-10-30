namespace Staple.Internal;

internal interface IRenderCommand
{
    void Update(IRendererBackend rendererBackend);
}
