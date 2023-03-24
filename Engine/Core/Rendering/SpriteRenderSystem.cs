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
        private Mesh spriteMesh;

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
        /// The vertices for a normal quad sprite
        /// </summary>
        private static Vector3[] positions = new Vector3[]
        {
            new Vector3(-0.5f, -0.5f, 0),
            new Vector3(-0.5f, 0.5f, 0),
            new Vector3(0.5f, 0.5f, 0),
            new Vector3(0.5f, -0.5f, 0),
        };

        private static Vector2[] uvs = new Vector2[]
        {
            Vector2.Zero,
            new Vector2(0, 1),
            Vector2.One,
            new Vector2(1, 0),
        };

        /// <summary>
        /// The indices for a normal quad sprite
        /// </summary>
        private static int[] indices = new int[]
        {
            0, 1, 2, 2, 3, 0
        };

        private List<SpriteRenderInfo> sprites = new List<SpriteRenderInfo>();

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
            if(r.texture != null && r.material != null && r.material.shader != null)
            {
                r.localBounds = new AABB(Vector3.Zero, new Vector3(r.texture.SpriteWidth, r.texture.SpriteHeight, 0));

                r.bounds = new AABB(transform.Position, new Vector3(r.texture.SpriteWidth, r.texture.SpriteHeight, 0));
            }
        }

        public void Process(Entity entity, Transform transform, IComponent renderer, ushort viewId)
        {
            var r = renderer as Sprite;

            if(r.material == null || r.material.shader == null || r.material.Disposed || r.material.shader.Disposed)
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
            if (spriteMesh == null)
            {
                spriteMesh = new Mesh();

                spriteMesh.vertices = positions;
                spriteMesh.uv = uvs;
                spriteMesh.indices = indices;

                spriteMesh.UploadMeshData();
            }

            if(sprites.Count == 0)
            {
                return;
            }

            spriteMesh?.SetActive();

            bgfx.StateFlags state = bgfx.StateFlags.WriteRgb | bgfx.StateFlags.WriteA | bgfx.StateFlags.DepthTestGequal | bgfx.StateFlags.PtTristrip;

            for (var i = 0; i < sprites.Count; i++)
            {
                var s = sprites[i];

                unsafe
                {
                    var transform = s.transform;

                    bgfx.set_transform(&transform, 1);
                }

                bgfx.set_state((ulong)state, 0);

                s.material.ApplyProperties();

                s.material.shader.SetColor(Material.MainColorProperty, s.color);

                if (s.texture != null)
                {
                    s.material.shader.SetTexture(Material.MainTextureProperty, s.texture);
                }

                var discardFlags = i == sprites.Count - 1 ? bgfx.DiscardFlags.All : bgfx.DiscardFlags.Transform | bgfx.DiscardFlags.Bindings | bgfx.DiscardFlags.State;

                bgfx.submit(s.viewID, s.material.shader.program, 0, (byte)discardFlags);
            }
        }
    }
}
