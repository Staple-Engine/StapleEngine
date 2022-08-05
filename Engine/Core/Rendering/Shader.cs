using Bgfx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Staple.Internal
{
    internal class Shader
    {
        internal class UniformInfo
        {
            public ShaderUniform uniform;
            public bgfx.UniformHandle handle;
            public object value;
        }

        internal readonly bgfx.ShaderHandle vertexShader;
        internal readonly bgfx.ShaderHandle fragmentShader;
        internal readonly bgfx.ProgramHandle program;

        private bool destroyed = false;
        private List<UniformInfo> uniforms = new List<UniformInfo>();

        internal Shader(ShaderMetadata metadata, bgfx.ShaderHandle vertexShader, bgfx.ShaderHandle fragmentShader, bgfx.ProgramHandle program)
        {
            this.vertexShader = vertexShader;
            this.fragmentShader = fragmentShader;
            this.program = program;

            foreach(var uniform in metadata.uniforms)
            {
                bgfx.UniformType type;

                object defaultValue;

                switch(uniform.type)
                {
                    case ShaderUniformType.Vector4:

                        type = bgfx.UniformType.Vec4;

                        defaultValue = Vector4.Zero;

                        break;

                    case ShaderUniformType.Color:

                        type = bgfx.UniformType.Vec4;

                        defaultValue = new Vector4(1, 1, 1, 1);

                        break;

                    case ShaderUniformType.Matrix4x4:

                        type = bgfx.UniformType.Mat4;

                        defaultValue = Matrix4x4.Identity;

                        break;

                    case ShaderUniformType.Matrix3x3:

                        type = bgfx.UniformType.Mat3;

                        defaultValue = Matrix3x3.Identity;

                        break;

                    case ShaderUniformType.Texture:

                        type = bgfx.UniformType.Sampler;

                        defaultValue = null;

                        break;

                    default:

                        continue;
                }

                var handle = bgfx.create_uniform(uniform.name, type, 1);

                if(handle.Valid == false)
                {
                    continue;
                }

                uniforms.Add(new UniformInfo()
                {
                    handle = handle,
                    uniform = uniform,
                    value = defaultValue,
                });
            }
        }

        ~Shader()
        {
            Destroy();
        }

        internal UniformInfo GetUniform(string name)
        {
            return uniforms.FirstOrDefault(x => x.uniform.name == name);
        }

        public void SetVector4(string name, Vector4 value)
        {
            var uniform = GetUniform(name);

            if(uniform == null || uniform.uniform.type != ShaderUniformType.Vector4)
            {
                return;
            }

            uniform.value = value;
        }

        public void SetColor(string name, Color value)
        {
            var uniform = GetUniform(name);

            if (uniform == null || uniform.uniform.type != ShaderUniformType.Color)
            {
                return;
            }

            uniform.value = new Vector4(value.r, value.g, value.b, value.a);
        }

        public void SetTexture(string name, Texture value)
        {
            var uniform = GetUniform(name);

            if (uniform == null || uniform.uniform.type != ShaderUniformType.Texture)
            {
                return;
            }

            uniform.value = value;
        }

        public void SetMatrix4x4(string name, Matrix4x4 value)
        {
            var uniform = GetUniform(name);

            if (uniform == null || uniform.uniform.type != ShaderUniformType.Matrix4x4)
            {
                return;
            }

            uniform.value = value;
        }

        public void SetMatrix3x3(string name, Matrix3x3 value)
        {
            var uniform = GetUniform(name);

            if (uniform == null || uniform.uniform.type != ShaderUniformType.Matrix3x3)
            {
                return;
            }

            uniform.value = value;
        }

        internal void ApplyUniforms()
        {
            foreach(var uniform in uniforms)
            {
                if(uniform.handle.Valid == false)
                {
                    continue;
                }

                switch(uniform.uniform.type)
                {
                    case ShaderUniformType.Texture:

                        if ((uniform.value is Texture) == false)
                        {
                            continue;
                        }

                        unsafe
                        {
                            var texture = (Texture)uniform.value;

                            texture.SetActive(0, uniform.handle);
                        }

                        break;

                    case ShaderUniformType.Matrix3x3:

                        if ((uniform.value is Matrix3x3) == false)
                        {
                            continue;
                        }

                        unsafe
                        {
                            var value = (Matrix3x3)uniform.value;

                            bgfx.set_uniform(uniform.handle, &value, 1);
                        }

                        break;

                    case ShaderUniformType.Matrix4x4:

                        if ((uniform.value is Matrix4x4) == false)
                        {
                            continue;
                        }

                        unsafe
                        {
                            var value = (Matrix4x4)uniform.value;

                            bgfx.set_uniform(uniform.handle, &value, 1);
                        }

                        break;

                    case ShaderUniformType.Vector4:
                    case ShaderUniformType.Color:

                        if((uniform.value is Vector4) == false)
                        {
                            continue;
                        }

                        unsafe
                        {
                            var value = (Vector4)uniform.value;

                            bgfx.set_uniform(uniform.handle, &value, 1);
                        }

                        break;
                }
            }
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

            foreach(var uniform in uniforms)
            {
                if(uniform.handle.Valid)
                {
                    bgfx.destroy_uniform(uniform.handle);
                }
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

                            return new Shader(data.metadata, vsHandle, fsHandle, programHandle);
                        }

                    default:

                        return null;
                }
            }
        }
    }
}
