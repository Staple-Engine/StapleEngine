using Staple.Internal;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Material resource
    /// </summary>
    public sealed class Material : IGuidAsset
    {
        private class ParameterInfo
        {
            public MaterialParameterType type;
            public object value;

            public ParameterInfo Clone()
            {
                return new()
                {
                    type = type,
                    value = value,
                };
            }
        }

        internal const string MainColorProperty = "mainColor";
        internal const string MainTextureProperty = "mainTexture";

        internal static Texture WhiteTexture;

        internal Shader shader;
        internal string guid;

        private Dictionary<string, ParameterInfo> parameters = new();

        /// <summary>
        /// The material's main color
        /// </summary>
        public Color MainColor
        {
            get => parameters.TryGetValue(MainColorProperty, out var parameter) ? (Color)parameter.value : Color.White;

            set
            {
                if(parameters.TryGetValue(MainColorProperty, out var parameter) && parameter != null && parameter.type == MaterialParameterType.Color)
                {
                    parameter.value = value;
                }
                else
                {
                    parameters.AddOrSetKey(MainColorProperty, new ParameterInfo()
                    {
                        type = MaterialParameterType.Color,
                        value = value,
                    });
                }
            }
        }

        /// <summary>
        /// The material's main texture
        /// </summary>
        public Texture MainTexture
        {
            get => parameters.TryGetValue(MainTextureProperty, out var parameter) ? (Texture)parameter.value : null;

            set
            {
                if (parameters.TryGetValue(MainTextureProperty, out var parameter) && parameter != null && parameter.type == MaterialParameterType.Texture)
                {
                    parameter.value = value;
                }
                else
                {
                    parameters.AddOrSetKey(MainTextureProperty, new ParameterInfo()
                    {
                        type = MaterialParameterType.Texture,
                        value = value,
                    });
                }
            }
        }

        /// <summary>
        /// The asset's guid (if any)
        /// </summary>
        public string Guid
        {
            get => guid;

            set => guid = value;
        }

        /// <summary>
        /// Whether this material has been disposed and is now invalid.
        /// </summary>
        public bool Disposed { get; internal set; } = false;

        public Material()
        {
            SetColor(MainColorProperty, Color.White);
        }

        public Material(Material sourceMaterial)
        {
            foreach (var parameter in sourceMaterial.parameters)
            {
                parameters.AddOrSetKey(parameter.Key, parameter.Value.Clone());
            }

            guid = sourceMaterial.guid;
            shader = sourceMaterial.shader;
        }

        ~Material()
        {
            Destroy();
        }

        /// <summary>
        /// Destroys this material's resources.
        /// </summary>
        internal void Destroy()
        {
            if(Disposed)
            {
                return;
            }

            Disposed = true;

            shader?.Destroy();
        }

        /// <summary>
        /// IPathAsset implementation. Loads a material from path.
        /// </summary>
        /// <param name="path">The path to load from</param>
        /// <returns>The material, or null</returns>
        public static object Create(string path) => ResourceManager.instance.LoadMaterial(path);

        /// <summary>
        /// Sets a color property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The color value</param>
        public void SetColor(string name, Color value)
        {
            if(parameters.TryGetValue(name, out var parameter))
            {
                if(parameter.type == MaterialParameterType.Color)
                {
                    parameter.value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    type = MaterialParameterType.Color,
                    value = value,
                });
            }
        }

        /// <summary>
        /// Sets a float property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The float value</param>
        public void SetFloat(string name, float value)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Float)
                {
                    parameter.value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    type = MaterialParameterType.Float,
                    value = value,
                });
            }
        }

        /// <summary>
        /// Sets a Vector2 property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The Vector2 value</param>
        public void SetVector2(string name, Vector2 value)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector2)
                {
                    parameter.value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    type = MaterialParameterType.Vector2,
                    value = value,
                });
            }
        }

        /// <summary>
        /// Sets a Vector3 property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The Vector3 value</param>
        public void SetVector3(string name, Vector3 value)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector3)
                {
                    parameter.value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    type = MaterialParameterType.Vector3,
                    value = value,
                });
            }
        }

        /// <summary>
        /// Sets a Vector4 property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The Vector4 value</param>
        public void SetVector4(string name, Vector4 value)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Vector4)
                {
                    parameter.value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    type = MaterialParameterType.Vector4,
                    value = value,
                });
            }
        }

        /// <summary>
        /// Sets a Texture property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The Texture value</param>
        public void SetTexture(string name, Texture value)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Texture)
                {
                    parameter.value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    type = MaterialParameterType.Texture,
                    value = value,
                });
            }
        }

        /// <summary>
        /// Sets a Matrix3x3 property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The Matrix3x3 value</param>
        public void SetMatrix3x3(string name, Matrix3x3 value)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Matrix3x3)
                {
                    parameter.value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    type = MaterialParameterType.Matrix3x3,
                    value = value,
                });
            }
        }

        /// <summary>
        /// Sets a Matrix4x4 property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The Matrix4x4 value</param>
        public void SetMatrix4x4(string name, Matrix4x4 value)
        {
            if (parameters.TryGetValue(name, out var parameter))
            {
                if (parameter.type == MaterialParameterType.Matrix4x4)
                {
                    parameter.value = value;
                }
            }
            else
            {
                parameters.AddOrSetKey(name, new ParameterInfo()
                {
                    type = MaterialParameterType.Matrix4x4,
                    value = value,
                });
            }
        }

        /// <summary>
        /// Applies the default properties of this material to the shader
        /// </summary>
        internal void ApplyProperties()
        {
            var t = MainTexture;

            if(t == null || t.Disposed)
            {
                if(WhiteTexture == null)
                {
                    var pixels = Enumerable.Repeat((byte)255, 64 * 64 * 4).ToArray();

                    WhiteTexture = Texture.CreatePixels("WHITE", pixels, 64, 64, new TextureMetadata()
                    {
                        filter = TextureFilter.Linear,
                        format = TextureMetadataFormat.RGBA8,
                        type = TextureType.Texture,
                        useMipmaps = false,
                    }, Bgfx.bgfx.TextureFormat.RGBA8);
                }

                SetTexture(MainTextureProperty, WhiteTexture);
            }

            foreach(var parameter in parameters)
            {
                switch(parameter.Value.type)
                {
                    case MaterialParameterType.Texture:

                        shader?.SetTexture(parameter.Key, (Texture)parameter.Value.value);

                        break;

                    case MaterialParameterType.Matrix3x3:

                        shader?.SetMatrix3x3(parameter.Key, (Matrix3x3)parameter.Value.value);

                        break;

                    case MaterialParameterType.Matrix4x4:

                        shader?.SetMatrix4x4(parameter.Key, (Matrix4x4)parameter.Value.value);

                        break;

                    case MaterialParameterType.Float:

                        shader?.SetVector4(parameter.Key, new Vector4((float)parameter.Value.value, 0, 0, 0));

                        break;

                    case MaterialParameterType.Vector2:

                        shader?.SetVector4(parameter.Key, new Vector4((Vector2)parameter.Value.value, 0, 0));

                        break;

                    case MaterialParameterType.Vector3:

                        shader?.SetVector4(parameter.Key, new Vector4((Vector3)parameter.Value.value, 0));

                        break;

                    case MaterialParameterType.Vector4:

                        shader?.SetVector4(parameter.Key, (Vector4)parameter.Value.value);

                        break;

                    case MaterialParameterType.Color:

                        shader?.SetColor(parameter.Key, (Color)parameter.Value.value);

                        break;
                }
            }
        }
    }
}
