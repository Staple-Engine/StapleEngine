using SDL3;

namespace Staple.Internal;

internal class SDLGPURenderPass(nint renderPass) : IRenderPass
{
    public nint renderPass = renderPass;

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
