using Staple.Internal;
using System;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Vertex Buffer resource
/// </summary>
public abstract class VertexBuffer
{
    internal VertexLayout layout;

    /// <summary>
    /// Contains the render buffer flags of this buffer
    /// </summary>
    public RenderBufferFlags Flags { get; internal set; }

    /// <summary>
    /// Whether this was destroyed
    /// </summary>
    public bool Disposed { get; private set; } = false;

    ~VertexBuffer()
    {
        Destroy();
    }

    /// <summary>
    /// Destroys the resource
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
    /// Updates the vertex buffer's data (if it's dynamic)
    /// </summary>
    /// <param name="data">An array of new data</param>
    /// <param name="lengthInBytes">The amount of bytes for the data</param>
    public abstract void Update(nint data, int lengthInBytes);

    /// <summary>
    /// Updates the vertex buffer's data (if it's dynamic)
    /// </summary>
    /// <typeparam name="T">A vertex type (probably a struct)</typeparam>
    /// <param name="data">An array of new data</param>
    public abstract void Update<T>(Span<T> data) where T : unmanaged;

    /// <summary>
    /// Updates the vertex buffer's data (if it's dynamic)
    /// </summary>
    /// <param name="data">An array of new data as bytes</param>
    public abstract void Update(Span<byte> data);

    /// <summary>
    /// Creates a vertex buffer from an array of data
    /// </summary>
    /// <typeparam name="T">A struct type</typeparam>
    /// <param name="data">An array of vertices</param>
    /// <param name="layout">The vertex layout to use</param>
    /// <param name="flags">Additional flags</param>
    /// <returns>The vertex buffer, or null</returns>
    public static VertexBuffer Create<T>(Span<T> data, VertexLayout layout, RenderBufferFlags flags = RenderBufferFlags.None) where T : unmanaged
    {
        var size = Marshal.SizeOf<T>();

        if (size != layout.Stride)
        {
            return null;
        }

        return RenderSystem.Backend.CreateVertexBuffer(data, layout, flags);
    }

    /// <summary>
    /// Creates a vertex buffer from an array of data
    /// </summary>
    /// <param name="data">An array of vertices</param>
    /// <param name="layout">The vertex layout to use</param>
    /// <param name="flags">Additional flags</param>
    /// <returns>The vertex buffer, or null</returns>
    public static VertexBuffer Create(Span<byte> data, VertexLayout layout, RenderBufferFlags flags = RenderBufferFlags.None)
    {
        var size = layout.Stride;

        if (data.Length == 0 || data.Length % size != 0)
        {
            return null;
        }

        return RenderSystem.Backend.CreateVertexBuffer(data, layout, flags);
    }
}
