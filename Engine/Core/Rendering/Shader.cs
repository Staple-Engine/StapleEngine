using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    internal class Shader
    {
        internal readonly bgfx.ShaderHandle vertexShader;
        internal readonly bgfx.ShaderHandle fragmentShader;
        internal readonly bgfx.ProgramHandle program;

        private bool destroyed = false;

        internal Shader(bgfx.ShaderHandle vertexShader, bgfx.ShaderHandle fragmentShader, bgfx.ProgramHandle program)
        {
            this.vertexShader = vertexShader;
            this.fragmentShader = fragmentShader;
            this.program = program;
        }

        ~Shader()
        {
            Destroy();
        }

        internal void Destroy()
        {
            if (destroyed)
            {
                return;
            }

            destroyed = true;

            if(program.Valid)
            {
                bgfx.destroy_program(program);
            }
        }

        internal static Shader Create(SerializableShader data)
        {
            unsafe
            {
                switch(data.metadata.type)
                {
                    case ShaderType.Compute:

                        //TODO
                        return null;

                    case ShaderType.VertexFragment:
                        {
                            bgfx.Memory* vs, fs;

                            fixed (void* ptr = data.vertexShader)
                            {
                                vs = bgfx.copy(ptr, (uint)data.vertexShader.Length);
                            }

                            fixed (void* ptr = data.fragmentShader)
                            {
                                fs = bgfx.copy(ptr, (uint)data.fragmentShader.Length);
                            }

                            bgfx.ShaderHandle vsHandle = bgfx.create_shader(vs);
                            bgfx.ShaderHandle fsHandle = bgfx.create_shader(fs);

                            if (vsHandle.Valid == false || fsHandle.Valid == false)
                            {
                                return null;
                            }

                            bgfx.ProgramHandle programHandle = bgfx.create_program(vsHandle, fsHandle, true);

                            if (programHandle.Valid == false)
                            {
                                return null;
                            }

                            return new Shader(vsHandle, fsHandle, programHandle);
                        }

                    default:

                        return null;
                }
            }
        }
    }
}
