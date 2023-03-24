using Staple.Internal;
using System.Linq;
using System.Numerics;

namespace Staple
{
    /// <summary>
    /// Material resource
    /// </summary>
    public class Material
    {
        internal const string MainColorProperty = "mainColor";
        internal const string MainTextureProperty = "mainTexture";

        internal static Texture WhiteTexture;

        internal Shader shader;
        internal string path;

        private Color mainColor;

        /// <summary>
        /// The material's main color
        /// </summary>
        public Color MainColor
        {
            get => mainColor;

            set
            {
                mainColor = value;
            }
        }

        private Texture mainTexture;

        /// <summary>
        /// The material's main texture
        /// </summary>
        public Texture MainTexture
        {
            get => mainTexture;

            set
            {
                mainTexture = value;
            }
        }

        /// <summary>
        /// Whether this material has been disposed and is now invalid.
        /// </summary>
        public bool Disposed { get; internal set; } = false;

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

        ~Material()
        {
            Destroy();
        }

        /// <summary>
        /// Sets a color property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The color value</param>
        public void SetColor(string name, Color value)
        {
            shader?.SetColor(name, value);
        }

        /// <summary>
        /// Sets a Vector4 property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The Vector4 value</param>
        public void SetVector4(string name, Vector4 value)
        {
            shader?.SetVector4(name, value);
        }

        /// <summary>
        /// Sets a Texture property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The Texture value</param>
        public void SetTexture(string name, Texture value)
        {
            shader?.SetTexture(name, value);
        }

        /// <summary>
        /// Sets a Matrix3x3 property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The Matrix3x3 value</param>
        public void SetMatrix3x3(string name, Matrix3x3 value)
        {
            shader?.SetMatrix3x3(name, value);
        }

        /// <summary>
        /// Sets a Matrix4x4 property's value
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">The Matrix4x4 value</param>
        public void SetMatrix4x4(string name, Matrix4x4 value)
        {
            shader?.SetMatrix4x4(name, value);
        }

        /// <summary>
        /// Applies the default properties of this material to the shader
        /// </summary>
        internal void ApplyProperties()
        {
            var t = mainTexture;

            if(t == null || t.destroyed)
            {
                if(WhiteTexture == null)
                {
                    var pixels = Enumerable.Repeat((byte)255, 64 * 64 * 4).ToArray();

                    WhiteTexture = Texture.CreatePixels("WHITE", pixels, 64, 64, new TextureMetadata()
                    {
                        filter = TextureFilter.Linear,
                        format = TextureMetadataFormat.RGBA8,
                        type = TextureType.SRGB,
                        useMipmaps = false,
                    }, Bgfx.bgfx.TextureFormat.RGBA8);
                }

                t = WhiteTexture;
            }

            SetTexture(MainTextureProperty, t);

            SetColor(MainColorProperty, mainColor);
        }
    }
}
