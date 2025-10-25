using SDL3;
using System.Numerics;

namespace Staple.Internal;

internal class SDLGPURenderPass(nint commandBuffer, nint renderPass, Matrix4x4 view, Matrix4x4 projection) : IRenderPass
{
    public nint renderPass = renderPass;

    public readonly nint commandBuffer = commandBuffer;
    public readonly Matrix4x4 view = view;
    public readonly Matrix4x4 projection = projection;

    public void Finish()
    {
        if(renderPass == nint.Zero)
        {
            return;
        }

        SDL.SDL_EndGPURenderPass(renderPass);

        renderPass = nint.Zero;
    }
}
