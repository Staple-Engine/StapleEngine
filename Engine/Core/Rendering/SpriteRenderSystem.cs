using Bgfx;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple
{
    /// <summary>
    /// Sprite Render System
    /// </summary>
    internal class SpriteRenderSystem : IRenderSystem
    {
        /// <summary>
        /// Contains render information for a sprite
        /// </summary>
        private class SpriteRenderInfo
        {
            public Matrix4x4 transform;
            public Material material;
            public Color color;
            public Texture texture;
            public ushort viewID;
        }

        /// <summary>
        /// Contains a list of all sprites queued for rendering
        /// </summary>
        private readonly List<SpriteRenderInfo> sprites = new();

        /// <summary>
        /// The mesh to use for the sprites. Right now it's just a simple quad mesh.
        /// </summary>
        private Mesh spriteMesh;

        public void Destroy()
        {
            spriteMesh?.Destroy();
        }

        public Type RelatedComponent()
        {
            return typeof(Sprite);
        }

        public void Prepare()
        {
            sprites.Clear();
        }

        public void Preprocess(Entity entity, Transform transform, IComponent renderer)
        {
            var r = renderer as Sprite;

            //We recalculate the bounds of this sprite
            if(r.texture != null &&
                r.texture.Disposed == false &&
                r.material != null &&
                r.material.Disposed == false &&
                r.material.shader != null &&
                r.material.Disposed == false)
            {
                r.localBounds = new AABB(Vector3.Zero, new Vector3(r.texture.SpriteWidth, r.texture.SpriteHeight, 0));

                r.bounds = new AABB(transform.Position, new Vector3(r.texture.SpriteWidth, r.texture.SpriteHeight, 0));
            }
        }

        public void Process(Entity entity, Transform transform, IComponent renderer, ushort viewId)
        {
            var r = renderer as Sprite;

            if(r.texture == null ||
                r.texture.Disposed ||
                r.material == null ||
                r.material.shader == null ||
                r.material.Disposed ||
                r.material.shader.Disposed)
            {
                return;
            }

            var scale = Vector3.Zero;

            if (r.texture != null)
            {
                scale.X = r.texture.SpriteWidth;
                scale.Y = r.texture.SpriteHeight;
            }

            var matrix = Matrix4x4.CreateScale(scale) * transform.Matrix;

            sprites.Add(new SpriteRenderInfo()
            {
                color = r.color,
                material = r.material,
                texture = r.texture,
                transform = matrix,
                viewID = viewId
            });
        }

        public void Submit()
        {
            spriteMesh ??= Mesh.Quad;

            if(sprites.Count == 0 || spriteMesh.SetActive() == false)
            {
                return;
            }

            bgfx.StateFlags state = bgfx.StateFlags.WriteRgb |
                bgfx.StateFlags.WriteA |
                bgfx.StateFlags.DepthTestLequal |
                spriteMesh.PrimitiveFlag();

            for (var i = 0; i < sprites.Count; i++)
            {
                var s = sprites[i];

                unsafe
                {
                    var transform = s.transform;

                    _ = bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)(state | s.material.shader.BlendingFlag()), 0);

                s.material.shader.SetColor(Material.MainColorProperty, s.color);
                s.material.shader.SetTexture(Material.MainTextureProperty, s.texture);

                //Discard everything only if it's the last sprite. Otherwise, reuse mesh buffers
                var discardFlags = i == sprites.Count - 1 ? bgfx.DiscardFlags.All : bgfx.DiscardFlags.Transform | bgfx.DiscardFlags.Bindings | bgfx.DiscardFlags.State;

                bgfx.submit(s.viewID, s.material.shader.program, 0, (byte)discardFlags);
            }
        }
    }
}
