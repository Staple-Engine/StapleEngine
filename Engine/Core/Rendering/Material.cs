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
        public readonly bgfx.ShaderHandle vertexShader;
        public readonly bgfx.ShaderHandle fragmentShader;
        public readonly bgfx.ProgramHandle program;

        public Material(bgfx.ShaderHandle vertexShader, bgfx.ShaderHandle fragmentShader, bgfx.ProgramHandle program)
        {
            this.vertexShader = vertexShader;
            this.fragmentShader = fragmentShader;
            this.program = program;
        }

        public static Material Create(byte[] vertexShaderData, byte[] fragmentShaderData)
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
