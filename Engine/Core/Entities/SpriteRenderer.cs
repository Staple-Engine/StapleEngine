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

        internal SpriteRenderer(Entity entity) : base(entity)
        {
        }
    }
}
