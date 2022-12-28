using Bgfx;
using System.Collections.Generic;
using System.Linq;

namespace Staple
{
    public class RenderTarget
    {
        internal bgfx.FrameBufferHandle handle;
        internal ushort width;
        internal ushort height;
        internal bgfx.TextureFormat format;
        internal TextureFlags flags;
        internal bgfx.BackbufferRatio ratio;
        internal List<Texture> textures = new List<Texture>();

        private bool destroyed = false;

        ~RenderTarget()
        {
            Destroy();
        }

        public void Destroy()
        {
            if (destroyed)
            {
                return;
            }

            destroyed = true;

            if (handle.Valid)
            {
                bgfx.destroy_frame_buffer(handle);
            }

            foreach(var texture in textures)
            {
                texture?.Destroy();
            }
        }

        public Texture GetTexture(byte attachment = 0)
        {
            return attachment < textures.Count ? textures[attachment] : null;
        }

        internal void SetActive(ushort viewID)
        {
            if(destroyed)
            {
                return;
            }

            bgfx.set_view_frame_buffer(viewID, handle);
        }

        internal static void SetActive(ushort viewID, RenderTarget target)
        {
            if(target == null || target.destroyed)
            {
                return;
            }

            target.SetActive(viewID);
        }

        public static RenderTarget Create(ushort width, ushort height, bgfx.TextureFormat colorFormat = bgfx.TextureFormat.RGBA8,
            bool hasMips = false, ushort layers = 1, TextureFlags flags = TextureFlags.SamplerUClamp | TextureFlags.SamplerVClamp)
        {
            var depthFormat = bgfx.is_texture_valid(0, false, 1, bgfx.TextureFormat.D16, (ulong)flags) ? bgfx.TextureFormat.D16 :
                bgfx.is_texture_valid(0, false, 1, bgfx.TextureFormat.D24S8, (ulong)flags) ? bgfx.TextureFormat.D24S8 :
                bgfx.TextureFormat.D32;

            var colorTexture = Texture.CreateEmpty(width, height, hasMips, layers, colorFormat, flags | TextureFlags.RenderTarget);
            var depthTexture = Texture.CreateEmpty(width, height, hasMips, layers, depthFormat, flags | TextureFlags.RenderTarget);

            if(colorTexture == null || depthTexture == null)
            {
                colorTexture?.Destroy();
                depthTexture?.Destroy();

                return null;
            }

            var outValue = Create(new Texture[] { colorTexture, depthTexture }.ToList());

            if(outValue == null)
            {
                colorTexture?.Destroy();
                depthTexture?.Destroy();

                return null;
            }

            return outValue;
        }

        public static RenderTarget Create(bgfx.BackbufferRatio ratio, bgfx.TextureFormat format,
            TextureFlags flags = TextureFlags.SamplerUClamp | TextureFlags.SamplerVClamp)
        {
            var handle = bgfx.create_frame_buffer_scaled(ratio, format, (ulong)flags);

            if (handle.Valid == false)
            {
                return null;
            }

            var textureHandle = bgfx.get_texture(handle, 0);

            if (textureHandle.Valid == false)
            {
                bgfx.destroy_frame_buffer(handle);

                return null;
            }

            var factor = 1.0f;

            switch(ratio)
            {
                case bgfx.BackbufferRatio.Sixteenth:

                    factor = 1 / 16.0f;

                    break;

                case bgfx.BackbufferRatio.Eighth:

                    factor = 1 / 8.0f;

                    break;

                case bgfx.BackbufferRatio.Quarter:

                    factor = 0.25f;

                    break;

                case bgfx.BackbufferRatio.Double:

                    factor = 2;

                    break;

                case bgfx.BackbufferRatio.Equal:

                    //Default

                    break;

                case bgfx.BackbufferRatio.Half:

                    factor = 0.5f;

                    break;
            }

            var width = (byte)(AppPlayer.ScreenWidth * factor);
            var height = (byte)(AppPlayer.ScreenHeight * factor);

            var texture = new Texture(textureHandle, width, height, true);

            return new RenderTarget()
            {
                handle = handle,
                format = format,
                flags = flags,
                textures = new List<Texture>(new Texture[] { texture })
            };
        }

        public static RenderTarget Create(List<Texture> textures, bool destroyTextures = false)
        {
            if(textures.Any(x => x == null || x.handle.Valid == false))
            {
                return null;
            }

            var handles = textures.Select(x => x.handle).ToArray();

            unsafe
            {
                fixed (bgfx.TextureHandle* h = handles)
                {
                    var handle = bgfx.create_frame_buffer_from_handles((byte)textures.Count, h, destroyTextures);

                    if(handle.Valid == false)
                    {
                        return null;
                    }

                    return new RenderTarget()
                    {
                        handle = handle,
                        textures = destroyTextures ? new List<Texture>() : textures,
                    };
                }
            }
        }
    }
}
