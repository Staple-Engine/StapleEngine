using SDL3;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple.Internal;

internal class SDLGPURenderPass(nint commandBuffer, nint renderPass, Matrix4x4 view, Matrix4x4 projection) : IRenderPass
{
    public struct StapleRenderData
    {
        public Matrix4x4 world;
        public Matrix4x4 view;
        public Matrix4x4 projection;
        public float time;
    }

    public nint renderPass = renderPass;

    public readonly nint commandBuffer = commandBuffer;

    public StapleRenderData renderData = new()
    {
        projection = projection,
        view = view,
        time = Time.time,
    };

    private nint lastGraphicsPipeline;
    private bool appliedUniforms;

    public void BindPipeline(nint pipeline)
    {
        //if (pipeline != lastGraphicsPipeline)
        {
            lastGraphicsPipeline = pipeline;

            SDL.SDL_BindGPUGraphicsPipeline(renderPass, pipeline);
        }
    }

    public void ApplyBuiltinUniforms(in Matrix4x4 world)
    {
        if(appliedUniforms ||
            world == renderData.world)
        {
            return;
        }

        appliedUniforms = true;
        renderData.world = world;

        unsafe
        {
            fixed(void* ptr = &renderData)
            {
                SDL.SDL_PushGPUVertexUniformData(commandBuffer, 0, (nint)ptr, (uint)Marshal.SizeOf<StapleRenderData>());
            }
        }
    }

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
