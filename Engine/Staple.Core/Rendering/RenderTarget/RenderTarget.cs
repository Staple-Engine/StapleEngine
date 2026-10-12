using Staple.Internal;
using System.Collections.Generic;

namespace Staple;

/// <summary>
/// Render Target resource. Used to render to texture.
/// </summary>
public sealed class RenderTarget
{
    public static RenderTarget Current;

    internal readonly int width;
    internal readonly int height;
    internal readonly TextureFlags flags;
    internal readonly List<Texture> colorTextures;

    internal Texture depthTexture;

    internal readonly Vector2Int size;

    public int ColorTextureCount => colorTextures.Count;

    public bool Disposed { get; private set; } = false;

    private RenderTarget(int width, int height, TextureFlags flags, List<Texture> colorTextures, Texture depthTexture)
    {
        this.width = width;
        this.height = height;
        this.flags = flags;
        this.colorTextures = colorTextures ?? [];
        this.depthTexture = depthTexture;

        size = new(width, height);
    }

    ~RenderTarget()
    {
        Destroy();
    }

    /// <summary>
    /// Destroys the render target's resources
    /// </summary>
    public void Destroy()
    {
        if (Disposed)
        {
            return;
        }

        Disposed = true;

        foreach(var texture in colorTextures)
        {
            texture?.Destroy();
        }

        colorTextures.Clear();

        depthTexture?.Destroy();

        depthTexture = null;
    }

    /// <summary>
    /// Gets a texture from our attachments
    /// </summary>
    /// <param name="attachment">The attachment index</param>
    /// <returns>The texture or null</returns>
    public Texture GetColorTexture(int index = 0)
    {
        if(Disposed)
        {
            return null;
        }

        return index < colorTextures.Count ? colorTextures[index] : null;
    }

    /// <summary>
    /// Creates a render target
    /// </summary>
    /// <param name="width">The width</param>
    /// <param name="height">The height</param>
    /// <param name="colorFormat">The color format to use</param>
    /// <param name="flags">Additional texture flags</param>
    /// <returns>The render target, or null</returns>
    public static RenderTarget Create(ushort width, ushort height, TextureFormat? colorFormat = null,
        TextureFlags flags = TextureFlags.ClampU | TextureFlags.ClampV)
    {
        var colorTexture = Texture.CreateEmpty(width, height, colorFormat ?? RenderSystem.Backend.SwapchainFormat, flags | TextureFlags.ColorTarget);
        var depthTexture = Texture.CreateEmpty(width, height, RenderSystem.Backend.DepthStencilFormat.Value, flags | TextureFlags.DepthStencilTarget);

        if (colorTexture == null || depthTexture == null)
        {
            colorTexture?.Destroy();
            depthTexture?.Destroy();

            return null;
        }

        return new RenderTarget(width, height, flags, [colorTexture], depthTexture);
    }
}
