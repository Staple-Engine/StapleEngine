using Bgfx;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class Material
    {
        internal Shader shader;

        private Color mainColor;

        public Color MainColor
        {
            get => mainColor;

            set
            {
                mainColor = value;

                shader?.SetColor("mainColor", mainColor);
            }
        }

        private Texture mainTexture;

        public Texture MainTexture
        {
            get => mainTexture;

            set
            {
                mainTexture = value;

                shader?.SetTexture("mainTexture", mainTexture);
            }
        }

        private bool destroyed = false;

        internal void Destroy()
        {
            if(destroyed)
            {
                return;
            }

            destroyed = true;

            shader?.Destroy();
        }

        ~Material()
        {
            Destroy();
        }

        public void SetColor(string name, Color value)
        {
            if(name == "mainColor")
            {
                MainColor = value;

                return;
            }

            shader?.SetColor(name, value);
        }

        public void SetVector4(string name, Vector4 value)
        {
            if (name == "mainColor")
            {
                MainColor = new Color(value.X, value.Y, value.Z, value.W);

                return;
            }

            shader?.SetVector4(name, value);
        }

        public void SetTexture(string name, Texture value)
        {
            if (name == "mainTexture")
            {
                MainTexture = value;

                return;
            }

            shader?.SetTexture(name, value);
        }

        public void SetMatrix3x3(string name, Matrix3x3 value)
        {
            shader?.SetMatrix3x3(name, value);
        }

        public void SetMatrix4x4(string name, Matrix4x4 value)
        {
            shader?.SetMatrix4x4(name, value);
        }
    }
}
