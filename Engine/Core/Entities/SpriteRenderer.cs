using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class SpriteRenderer : Renderer
    {
        public Material material;
        public Texture texture;
        public Color color = Color.White;

        internal void OnDestroy()
        {
            material?.Destroy();
        }
    }
}
