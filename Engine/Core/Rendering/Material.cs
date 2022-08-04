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

        internal bgfx.UniformHandle ColorHandle { get; private set; }

        public Color Color = Color.White;

        internal bgfx.UniformHandle MainTextureHandle { get; private set; }

        public Texture mainTexture;

        public Vector4 textureScale = new Vector4(0, 0, 1, 1);

        private bool destroyed = false;

        internal Material()
        {
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

            shader?.Destroy();

            if(mainTexture != null)
            {
                mainTexture.Destroy();
            }
        }

        ~Material()
        {
            Destroy();
        }
    }
}
