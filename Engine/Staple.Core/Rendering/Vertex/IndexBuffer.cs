using Staple.Internal;
using System;

namespace Staple;

/// <summary>
/// Index Buffer resource
/// </summary>
public abstract class IndexBuffer
{
    /// <summary>
    /// Whether this was destroyed
    /// </summary>
    internal bool Disposed { get; private set; } = false;

    /// <summary>
    /// Contains the render buffer flags of this buffer
    /// </summary>
    public RenderBufferFlags Flags { get; internal set; }

    /// <summary>
    /// Whether this index buffer is 32-bit
    /// </summary>
    public bool Is32Bit { get; protected set; }

    ~IndexBuffer()
    {
        Destroy();
    }

    /// <summary>
    /// Destroys the index buffer's resources
    /// </summary>
    public virtual void Destroy()
    {
        if (Disposed)
        {
            return;
        }

        Disposed = true;
    }

    /// <summary>
    /// Updates the index buffer's data (if it's dynamic)
    /// </summary>
    /// <param name="indices">An array of new data</param>
    public abstract void Update(Span<ushort> indices);

    /// <summary>
    /// Updates the index buffer's data (if it's dynamic)
    /// </summary>
    /// <param name="indices">An array of new data</param>
    public abstract void Update(Span<uint> indices);

    /// <summary>
    /// Creates an index buffer from ushort data
    /// </summary>
    /// <param name="data">The data</param>
    /// <param name="flags">Additional flags</param>
    /// <returns>The index buffer, or null</returns>
    public static IndexBuffer Create(Span<ushort> data, RenderBufferFlags flags = RenderBufferFlags.None)
    {
        return RenderSystem.Backend.CreateIndexBuffer(data, flags);
    }

    /// <summary>
    /// Creates an index buffer from uint data
    /// </summary>
    /// <param name="data">The data</param>
    /// <param name="flags">Additional flags</param>
    /// <returns>The index buffer, or null</returns>
    public static IndexBuffer Create(Span<uint> data, RenderBufferFlags flags = RenderBufferFlags.None)
    {
        return RenderSystem.Backend.CreateIndexBuffer(data, flags);
    }
}
