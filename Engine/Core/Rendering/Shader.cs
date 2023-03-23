using Bgfx;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple.Internal
{
    /// <summary>
    /// Shader resource
    /// </summary>
    internal class Shader
    {
        internal class UniformInfo
        {
            public ShaderUniform uniform;
            public bgfx.UniformHandle handle;
        }

        internal readonly bgfx.ShaderHandle vertexShader;
        internal readonly bgfx.ShaderHandle fragmentShader;
        internal readonly bgfx.ProgramHandle program;

        internal List<UniformInfo> uniforms = new List<UniformInfo>();

        /// <summary>
        /// Whether this shader has been disposed
        /// </summary>
        public bool Disposed { get; internal set; } = false;

        internal Shader(ShaderMetadata metadata, bgfx.ShaderHandle vertexShader, bgfx.ShaderHandle fragmentShader, bgfx.ProgramHandle program)
        {
            this.vertexShader = vertexShader;
            this.fragmentShader = fragmentShader;
            this.program = program;

            foreach(var uniform in metadata.uniforms)
            {
                bgfx.UniformType type;

                switch(uniform.type)
                {
                    case ShaderUniformType.Vector4:

                        type = bgfx.UniformType.Vec4;

                        break;

                    case ShaderUniformType.Color:

                        type = bgfx.UniformType.Vec4;

                        break;

                    case ShaderUniformType.Matrix4x4:

                        type = bgfx.UniformType.Mat4;

                        break;

                    case ShaderUniformType.Matrix3x3:

                        type = bgfx.UniformType.Mat3;

                        break;

                    case ShaderUniformType.Texture:

                        type = bgfx.UniformType.Sampler;

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
                });
            }
        }

        ~Shader()
        {
            Destroy();
        }

        internal UniformInfo GetUniform(string name)
        {
            if (Disposed)
            {
                return null;
            }

            return uniforms.FirstOrDefault(x => x.uniform.name == name);
        }

        /// <summary>
        /// Sets a Vector4 uniform's value
        /// </summary>
        /// <param name="name">The uniform's name</param>
        /// <param name="value">The value</param>
        public void SetVector4(string name, Vector4 value)
        {
            if (Disposed)
            {
                return;
            }

            var uniform = GetUniform(name);

            if(uniform == null || uniform.uniform.type != ShaderUniformType.Vector4)
            {
                return;
            }

            unsafe
            {
                bgfx.set_uniform(uniform.handle, &value, 1);
            }
        }

        /// <summary>
        /// Sets a Color uniform's value
        /// </summary>
        /// <param name="name">The uniform's name</param>
        /// <param name="value">The value</param>
        public void SetColor(string name, Color value)
        {
            if(Disposed)
            {
                return;
            }

            var uniform = GetUniform(name);

            if (uniform == null || uniform.uniform.type != ShaderUniformType.Color)
            {
                return;
            }

            var colorValue = new Vector4(value.r, value.g, value.b, value.a);

            unsafe
            {
                bgfx.set_uniform(uniform.handle, &colorValue, 1);
            }
        }

        /// <summary>
        /// Sets a Texture uniform's value
        /// </summary>
        /// <param name="name">The uniform's name</param>
        /// <param name="value">The value</param>
        public void SetTexture(string name, Texture value)
        {
            if (Disposed)
            {
                return;
            }

            var uniform = GetUniform(name);

            if (value == null || uniform == null || uniform.uniform.type != ShaderUniformType.Texture)
            {
                return;
            }

            unsafe
            {
                value.SetActive(0, uniform.handle);
            }
        }

        /// <summary>
        /// Sets a Matrix4x4 uniform's value
        /// </summary>
        /// <param name="name">The uniform's name</param>
        /// <param name="value">The value</param>
        public void SetMatrix4x4(string name, Matrix4x4 value)
        {
            if (Disposed)
            {
                return;
            }

            var uniform = GetUniform(name);

            if (uniform == null || uniform.uniform.type != ShaderUniformType.Matrix4x4)
            {
                return;
            }

            unsafe
            {
                bgfx.set_uniform(uniform.handle, &value, 1);
            }
        }

        /// <summary>
        /// Sets a Matrix3x3 uniform's value
        /// </summary>
        /// <param name="name">The uniform's name</param>
        /// <param name="value">The value</param>
        public void SetMatrix3x3(string name, Matrix3x3 value)
        {
            if (Disposed)
            {
                return;
            }

            var uniform = GetUniform(name);

            if (uniform == null || uniform.uniform.type != ShaderUniformType.Matrix3x3)
            {
                return;
            }

            unsafe
            {
                bgfx.set_uniform(uniform.handle, &value, 1);
            }
        }

        /// <summary>
        /// Destroys this resource
        /// </summary>
        internal void Destroy()
        {
            if (Disposed)
            {
                return;
            }

            Disposed = true;

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

        /// <summary>
        /// Creates from shader data
        /// </summary>
        /// <param name="data">The data</param>
        /// <returns>The shader if valid</returns>
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
