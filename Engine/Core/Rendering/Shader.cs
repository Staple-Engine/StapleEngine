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
            public object value;

            public bool Create()
            {
                bgfx.UniformType type;

                switch (uniform.type)
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

                        return false;
                }

                handle = bgfx.create_uniform(uniform.name, type, 1);

                return handle.Valid;
            }
        }

        internal bgfx.ShaderHandle vertexShader;
        internal bgfx.ShaderHandle fragmentShader;
        internal bgfx.ProgramHandle program;

        internal readonly ShaderMetadata metadata;
        internal readonly BlendMode sourceBlend = BlendMode.Off, destinationBlend = BlendMode.Off;
        internal readonly byte[] vertexShaderSource;
        internal readonly byte[] fragmentShaderSource;

        internal List<UniformInfo> uniforms = new();

        /// <summary>
        /// Whether this shader has been disposed
        /// </summary>
        public bool Disposed { get; internal set; } = false;

        internal Shader(ShaderMetadata metadata, byte[] vertexShaderSource, byte[] fragmentShaderSource)
        {
            this.vertexShaderSource = vertexShaderSource;
            this.fragmentShaderSource = fragmentShaderSource;
            this.metadata = metadata;

            sourceBlend = metadata.sourceBlend;
            destinationBlend = metadata.destinationBlend;
        }

        ~Shader()
        {
            Destroy();
        }

        internal unsafe bool Create()
        {
            bgfx.Memory* vs, fs;

            fixed (void* ptr = vertexShaderSource)
            {
                vs = bgfx.copy(ptr, (uint)vertexShaderSource.Length);
            }

            fixed (void* ptr = fragmentShaderSource)
            {
                fs = bgfx.copy(ptr, (uint)fragmentShaderSource.Length);
            }

            vertexShader = bgfx.create_shader(vs);
            fragmentShader = bgfx.create_shader(fs);

            if (vertexShader.Valid == false || fragmentShader.Valid == false)
            {
                return false;
            }

            program = bgfx.create_program(vertexShader, fragmentShader, true);

            if (program.Valid == false)
            {
                bgfx.destroy_shader(vertexShader);
                bgfx.destroy_shader(fragmentShader);

                return false;
            }

            if(uniforms.Count > 0)
            {
                foreach(var uniform in uniforms)
                {
                    if(uniform.Create())
                    {
                        if(uniform.value != null)
                        {
                            switch(uniform.value)
                            {
                                case Vector4 v:

                                    SetVector4(uniform.uniform.name, v);

                                    break;

                                case Texture t:

                                    SetTexture(uniform.uniform.name, t);

                                    break;

                                case Matrix3x3 m:

                                    SetMatrix3x3(uniform.uniform.name, m);

                                    break;

                                case Matrix4x4 m:

                                    SetMatrix4x4(uniform.uniform.name, m);

                                    break;
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var uniform in metadata.uniforms)
                {
                    var u = new UniformInfo()
                    {
                        uniform = uniform,
                    };

                    if (u.Create())
                    {
                        uniforms.Add(u);
                    }
                }
            }

            return true;
        }

        internal bgfx.StateFlags BlendingFlag()
        {
            if(sourceBlend != BlendMode.Off && destinationBlend != BlendMode.Off)
            {
                return (bgfx.StateFlags)RenderSystem.BlendFunction((bgfx.StateFlags)sourceBlend, (bgfx.StateFlags)destinationBlend);
            }

            return 0;
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

            uniform.value = value;

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

            uniform.value = value;

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

            uniform.value = value;

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

            uniform.value = value;

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

            uniform.value = value;

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
                            var shader = new Shader(data.metadata, data.vertexShader, data.fragmentShader);

                            if(shader.Create())
                            {
                                return shader;
                            }

                            return null;
                        }

                    default:

                        return null;
                }
            }
        }
    }
}
