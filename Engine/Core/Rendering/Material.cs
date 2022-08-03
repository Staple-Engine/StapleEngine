using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class Material
    {
        internal readonly bgfx.ShaderHandle vertexShader;
        internal readonly bgfx.ShaderHandle fragmentShader;
        internal readonly bgfx.ProgramHandle program;

        internal bgfx.UniformHandle ColorHandle { get; private set; }

        public Color Color = Color.White;

        internal bgfx.UniformHandle MainTextureHandle { get; private set; }

        public Texture mainTexture;

        private bool destroyed = false;

        internal Material(bgfx.ShaderHandle vertexShader, bgfx.ShaderHandle fragmentShader, bgfx.ProgramHandle program)
        {
            this.vertexShader = vertexShader;
            this.fragmentShader = fragmentShader;
            this.program = program;

            ColorHandle = bgfx.create_uniform("u_color", bgfx.UniformType.Vec4, 1);
            MainTextureHandle = bgfx.create_uniform("s_texColor", bgfx.UniformType.Sampler, 1);
        }

        internal void Destroy()
        {
            if(destroyed)
            {
                return;
            }

            destroyed = true;

            if(ColorHandle.Valid)
            {
                bgfx.destroy_uniform(ColorHandle);
            }

            if(MainTextureHandle.Valid)
            {
                bgfx.destroy_uniform(MainTextureHandle);
            }

            if(program.Valid)
            {
                bgfx.destroy_program(program);
            }

            if(mainTexture != null)
            {
                mainTexture.Destroy();
            }
        }

        ~Material()
        {
            Destroy();
        }

        internal static Material Create(byte[] vertexShaderData, byte[] fragmentShaderData)
        {
            unsafe
            {
                bgfx.Memory* vs, fs;

                fixed(void *ptr = vertexShaderData)
                {
                    vs = bgfx.copy(ptr, (uint)vertexShaderData.Length);
                }

                fixed (void* ptr = fragmentShaderData)
                {
                    fs = bgfx.copy(ptr, (uint)fragmentShaderData.Length);
                }

                bgfx.ShaderHandle vsHandle = bgfx.create_shader(vs);
                bgfx.ShaderHandle fsHandle = bgfx.create_shader(fs);

                if(vsHandle.Valid == false || fsHandle.Valid == false)
                {
                    return null;
                }

                bgfx.ProgramHandle programHandle = bgfx.create_program(vsHandle, fsHandle, true);

                if(programHandle.Valid == false)
                {
                    return null;
                }

                return new Material(vsHandle, fsHandle, programHandle);
            }
        }
    }
}
