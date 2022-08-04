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
        public Color color = Color.White;

        internal SpriteRenderer(Entity entity) : base(entity)
        {
        }

        internal void OnDestroy()
        {
            material.Destroy();
        }
    }
}
