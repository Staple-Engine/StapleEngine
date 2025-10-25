using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Render Target resource. Used to render to texture.
/// </summary>
public sealed class RenderTarget
{
    private static ulong counter = 0;

    //internal bgfx.FrameBufferHandle handle;
    internal ushort width;
    internal ushort height;
    internal TextureFormat format;
    internal TextureFlags flags;
    internal List<Texture> textures = [];

    private bool destroyed = false;

    //For some reason using anything other than a static ptr or buffer results in the buffer always being zero'd.
    //So we're gonna have to do this in an unconventional way...
    private static nint renderPtr = nint.Zero;

    private static readonly List<Action> renderQueue = [];

    ~RenderTarget()
    {
        Destroy();
    }

    /// <summary>
    /// Destroys the render target's resources
    /// </summary>
    public void Destroy()
    {
        if (destroyed)
        {
            return;
        }

        destroyed = true;

        /*
        if (handle.Valid)
        {
            bgfx.destroy_frame_buffer(handle);
        }
        */

        foreach(var texture in textures)
        {
            texture?.Destroy();
        }
    }

    /// <summary>
    /// Gets a texture from our attachments
    /// </summary>
    /// <param name="attachment">The attachment index</param>
    /// <returns>The texture or null</returns>
    public Texture GetTexture(byte attachment = 0)
    {
        if(destroyed)
        {
            return null;
        }

        return attachment < textures.Count ? textures[attachment] : null;
    }

    /// <summary>
    /// Renders with this render target
    /// </summary>
    /// <param name="viewID">The view ID to use</param>
    /// <param name="renderCallback">A callback with render instrucitons</param>
    public void Render(ushort viewID, Action renderCallback)
    {
        if(destroyed)
        {
            return;
        }

        var screenWidth = Screen.Width;
        var screenHeight = Screen.Height;

        Screen.Width = width;
        Screen.Height = height;

        SetActive(viewID);

        try
        {
            renderCallback?.Invoke();
        }
        catch(Exception e)
        {
            Log.Debug($"[RenderTarget] While rendering view ID {viewID}: {e}");
        }

        Screen.Width = screenWidth;
        Screen.Height = screenHeight;
    }

    /// <summary>
    /// Sets this framebuffer as active for a view
    /// </summary>
    /// <param name="viewID">The view ID</param>
    internal void SetActive(ushort viewID)
    {
        if(destroyed)
        {
            return;
        }

        //bgfx.set_view_frame_buffer(viewID, handle);
    }

    public void ReadTexture(ushort viewID, byte attachment, Action<Texture, byte[]> completion)
    {
        void RunQueueItem()
        {
            try
            {
                //Delay by 1 frame so that the rendering happens
                ThreadHelper.Dispatch(() =>
                {
                    var texture = GetTexture(attachment);

                    /*
                    if (texture == null ||
                        texture.Disposed ||
                        texture.info.storageSize == 0)
                    {
                        completion?.Invoke(null, null);

                        RunQueueItem();

                        return;
                    }
                    */

                    /*
                    var readBackTexture = Texture.CreateEmpty(texture.info.width, texture.info.height, false, 1,
                        BGFXUtils.GetBGFXTextureFormat(texture.info.format),
                        TextureFlags.BlitDestination | TextureFlags.ReadBack | TextureFlags.SamplerUClamp | TextureFlags.SamplerVClamp);

                    bgfx.blit(viewID, readBackTexture.handle, 0, 0, 0, 0, texture.handle, 0, 0, 0, 0, texture.info.width, texture.info.height, 0);

                    unsafe
                    {
                        renderPtr = Marshal.AllocHGlobal((int)texture.info.storageSize);

                        var buffer = new byte[texture.info.storageSize];

                        var frame = bgfx.read_texture(readBackTexture.handle, (void*)renderPtr, 0);

                        RenderSystem.Instance.QueueFrameCallback(frame + 1, () =>
                        {
                            Marshal.Copy(renderPtr, buffer, 0, buffer.Length);

                            Marshal.FreeHGlobal(renderPtr);

                            renderPtr = nint.Zero;

                            readBackTexture.Destroy();

                            completion?.Invoke(texture, buffer);

                            renderQueue.RemoveAt(0);

                            if (renderQueue.Count > 0)
                            {
                                RunQueueItem();
                            }
                        });
                    }
                    */
                });
            }
            catch (Exception e)
            {
                Log.Debug($"[RenderTarget] Failed to read data: {e}");

                RunQueueItem();
            }
        }

        renderQueue.Add(RunQueueItem);

        if(renderQueue.Count == 1)
        {
            RunQueueItem();
        }
    }

    /// <summary>
    /// Sets this framebuffer as active for a view
    /// </summary>
    /// <param name="viewID">The view ID</param>
    /// <param name="target">The render target to set</param>
    internal static void SetActive(ushort viewID, RenderTarget target)
    {
        if(target == null || target.destroyed)
        {
            return;
        }

        target.SetActive(viewID);
    }

    /// <summary>
    /// Creates a render target
    /// </summary>
    /// <param name="width">The width</param>
    /// <param name="height">The height</param>
    /// <param name="colorFormat">The color format to use</param>
    /// <param name="hasMips">Whether to use mipmaps</param>
    /// <param name="layers">Amount of layers to use on the textures</param>
    /// <param name="flags">Additional texture flags</param>
    /// <returns>The render target, or null</returns>
    public static RenderTarget Create(ushort width, ushort height, TextureFormat colorFormat = TextureFormat.RGBA8,
        bool hasMips = false, ushort layers = 1, TextureFlags flags = TextureFlags.None )//TextureFlags.SamplerUClamp | TextureFlags.SamplerVClamp)
    {
        /*
        var depthFormat = bgfx.is_texture_valid(0, false, 1, bgfx.TextureFormat.D16, (ulong)flags) ? TextureFormat.D16 :
            bgfx.is_texture_valid(0, false, 1, bgfx.TextureFormat.D24S8, (ulong)flags) ? TextureFormat.D24S8 :
            TextureFormat.D32;

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
        */

        return null;
    }

    /// <summary>
    /// Creates a render target based on a list of textures
    /// </summary>
    /// <param name="textures">The list of textures</param>
    /// <param name="destroyTextures">Whether to destroy the textures after</param>
    /// <returns>The render target, or null</returns>
    public static RenderTarget Create(List<Texture> textures, bool destroyTextures = false)
    {
        /*
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

                var name = $"RenderTarget {++counter}";

                bgfx.set_frame_buffer_name(handle, name, name.Length);

                for(var i = 0; i < handles.Length; i++)
                {
                    name = $"RenderTarget {counter} Texture {i + 1}";

                    bgfx.set_texture_name(handles[i], name, name.Length);
                }

                if(destroyTextures)
                {
                    foreach(var t in textures)
                    {
                        t.Destroy();
                    }
                }

                return new RenderTarget()
                {
                    handle = handle,
                    textures = destroyTextures ? new List<Texture>() : textures,
                    width = (ushort)textures[0].Width,
                    height = (ushort)textures[0].Height,
                };
            }
        }
        */

        return null;
    }
}
