using Bgfx;
using System;
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
        internal class UniformInfo<T>
        {
            public ShaderUniform uniform;
            public bgfx.UniformHandle handle;
            public T value;

            public bool Create()
            {
                bgfx.UniformType type;

                switch (uniform.type)
                {
                    case ShaderUniformType.Float:
                    case ShaderUniformType.Vector2:
                    case ShaderUniformType.Vector3:
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

        internal Dictionary<ShaderUniformType, object> uniforms = new();

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
                if(vertexShader.Valid)
                {
                    bgfx.destroy_shader(vertexShader);
                }

                if(fragmentShader.Valid)
                {
                    bgfx.destroy_shader(vertexShader);
                }

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
                void Apply<T>(object value, Action<UniformInfo<T>> callback)
                {
                    if (value is Dictionary<int, UniformInfo<T>> container)
                    {
                        foreach (var p in container)
                        {
                            var uniform = p.Value;

                            if (uniform.Create())
                            {
                                if (uniform.value != null)
                                {
                                    callback?.Invoke(uniform);
                                }
                            }
                        }
                    }
                }

                foreach (var pair in uniforms)
                {
                    switch(pair.Key)
                    {
                        case ShaderUniformType.Texture:

                            Apply<Texture>(pair.Value, (uniform) => SetTexture(uniform.uniform.name, uniform.value));

                            break;

                        case ShaderUniformType.Matrix3x3:

                            Apply<Matrix3x3>(pair.Value, (uniform) => SetMatrix3x3(uniform.uniform.name, uniform.value));

                            break;

                        case ShaderUniformType.Matrix4x4:

                            Apply<Matrix4x4>(pair.Value, (uniform) => SetMatrix4x4(uniform.uniform.name, uniform.value));

                            break;

                        case ShaderUniformType.Vector4:

                            Apply<Vector4>(pair.Value, (uniform) => SetVector4(uniform.uniform.name, uniform.value));

                            break;

                        case ShaderUniformType.Color:

                            Apply<Color>(pair.Value, (uniform) => SetColor(uniform.uniform.name, uniform.value));

                            break;

                        case ShaderUniformType.Vector3:

                            Apply<Vector3>(pair.Value, (uniform) => SetVector3(uniform.uniform.name, uniform.value));

                            break;

                        case ShaderUniformType.Vector2:

                            Apply<Vector2>(pair.Value, (uniform) => SetVector2(uniform.uniform.name, uniform.value));

                            break;

                        case ShaderUniformType.Float:

                            Apply<float>(pair.Value, (uniform) => SetFloat(uniform.uniform.name, uniform.value));

                            break;
                    }
                }
            }
            else
            {
                foreach (var uniform in metadata.uniforms)
                {
                    void Add<T>()
                    {
                        if (uniforms.TryGetValue(uniform.type, out var container) == false)
                        {
                            container = new Dictionary<int, UniformInfo<T>>();

                            uniforms.Add(uniform.type, container);
                        }

                        if (container is Dictionary<int, UniformInfo<T>> c)
                        {
                            var u = new UniformInfo<T>()
                            {
                                uniform = uniform,
                            };

                            if (u.Create())
                            {
                                c.Add(u.uniform.name.GetHashCode(), u);
                            }
                        }
                    }

                    switch (uniform.type)
                    {
                        case ShaderUniformType.Texture:

                            Add<Texture>();

                            break;

                        case ShaderUniformType.Matrix3x3:

                            Add<Matrix3x3>();

                            break;

                        case ShaderUniformType.Matrix4x4:

                            Add<Matrix4x4>();

                            break;

                        case ShaderUniformType.Vector4:

                            Add<Vector4>();

                            break;

                        case ShaderUniformType.Color:

                            Add<Color>();

                            break;

                        case ShaderUniformType.Vector3:

                            Add<Vector3>();

                            break;

                        case ShaderUniformType.Vector2:

                            Add<Vector2>();

                            break;

                        case ShaderUniformType.Float:

                            Add<float>();

                            break;
                    }
                }
            }

            Disposed = false;

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

        internal UniformInfo<T> GetUniform<T>(ShaderUniformType type, string name)
        {
            if (Disposed)
            {
                return null;
            }

            return uniforms.TryGetValue(type, out var container) &&
                container is Dictionary<int, UniformInfo<T>> c &&
                c.TryGetValue(name.GetHashCode(), out var outValue) ? outValue : null;
        }

        internal UniformInfo<T> GetUniform<T>(ShaderUniformType type, int hashCode)
        {
            if (Disposed)
            {
                return null;
            }

            return uniforms.TryGetValue(type, out var container) &&
                container is Dictionary<int, UniformInfo<T>> c &&
                c.TryGetValue(hashCode, out var outValue) ? outValue : null;
        }

        /// <summary>
        /// Sets a Vector3 uniform's value
        /// </summary>
        /// <param name="name">The uniform's name</param>
        /// <param name="value">The value</param>
        public void SetFloat(string name, float value)
        {
            if (Disposed)
            {
                return;
            }

            var uniform = GetUniform<float>(ShaderUniformType.Float, name);

            if (uniform == null)
            {
                return;
            }

            uniform.value = value;

            unsafe
            {
                var temp = new Vector4(value, 0, 0, 0);

                bgfx.set_uniform(uniform.handle, &temp, 1);
            }
        }

        /// <summary>
        /// Sets a Vector3 uniform's value
        /// </summary>
        /// <param name="name">The uniform's name</param>
        /// <param name="value">The value</param>
        public void SetVector2(string name, Vector2 value)
        {
            if (Disposed)
            {
                return;
            }

            var uniform = GetUniform<Vector2>(ShaderUniformType.Vector2, name);

            if (uniform == null)
            {
                return;
            }

            uniform.value = value;

            unsafe
            {
                var temp = new Vector4(value, 0, 0);

                bgfx.set_uniform(uniform.handle, &temp, 1);
            }
        }

        /// <summary>
        /// Sets a Vector3 uniform's value
        /// </summary>
        /// <param name="name">The uniform's name</param>
        /// <param name="value">The value</param>
        public void SetVector3(string name, Vector3 value)
        {
            if (Disposed)
            {
                return;
            }

            var uniform = GetUniform<Vector3>(ShaderUniformType.Vector3, name);

            if (uniform == null)
            {
                return;
            }

            uniform.value = value;

            unsafe
            {
                var temp = new Vector4(value, 0);

                bgfx.set_uniform(uniform.handle, &temp, 1);
            }
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

            var uniform = GetUniform<Vector4>(ShaderUniformType.Vector4, name);

            if(uniform == null)
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


            var uniform = GetUniform<Color>(ShaderUniformType.Color, name);

            if (uniform == null)
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
            if (Disposed || value == null || value.Disposed)
            {
                return;
            }

            var uniform = GetUniform<Texture>(ShaderUniformType.Texture, name);

            if (uniform == null)
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

            var uniform = GetUniform<Matrix4x4>(ShaderUniformType.Matrix4x4, name);

            if (uniform == null)
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

            var uniform = GetUniform<Matrix3x3>(ShaderUniformType.Matrix3x3, name);

            if (uniform == null)
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

            static void DestroyUniforms<T>(object value)
            {
                if (value is Dictionary<int, UniformInfo<T>> container)
                {
                    foreach (var p in container)
                    {
                        var uniform = p.Value;

                        if (uniform.handle.Valid)
                        {
                            bgfx.destroy_uniform(uniform.handle);
                        }
                    }
                }
            }

            foreach (var pair in uniforms)
            {
                switch(pair.Key)
                {
                    case ShaderUniformType.Texture:

                        DestroyUniforms<Texture>(pair.Value);

                        break;

                    case ShaderUniformType.Vector4:

                        DestroyUniforms<Vector4>(pair.Value);

                        break;

                    case ShaderUniformType.Matrix3x3:

                        DestroyUniforms<Matrix3x3>(pair.Value);

                        break;

                    case ShaderUniformType.Matrix4x4:

                        DestroyUniforms<Matrix4x4>(pair.Value);

                        break;

                    case ShaderUniformType.Color:

                        DestroyUniforms<Color>(pair.Value);

                        break;
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
