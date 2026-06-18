using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple.Internal;

/// <summary>
/// Reference counted instance of something
/// </summary>
/// <typeparam name="K">The key for this something, to identify it across different ones</typeparam>
/// <typeparam name="V">The value to keep in this</typeparam>
public class Ref<K, V> : IDisposable
{
    /// <summary>
    /// Callback for when freeing the content in this reference
    /// </summary>
    /// <param name="key">The key used when creating this</param>
    /// <param name="content">The current content in this</param>
    public delegate void FreeCallback(K key, V content);

    private class RefContainer
    {
        public readonly List<Ref<K, V>> refs = [];
        public V content;
    }

    private static readonly Dictionary<K, RefContainer> refs = [];
    private static readonly Lock refLock = new();
    private bool disposed;
    private readonly K key;
    private readonly FreeCallback freeCallback;
    private readonly RefContainer container;

    /// <summary>
    /// The content in this reference
    /// </summary>
    public V content => container != null ? container.content : default;

    /// <summary>
    /// Whether this reference is valid
    /// </summary>
    public bool IsValid => !disposed && content is not null;

    /// <summary>
    /// Amount of references
    /// </summary>
    public int RefCount
    {
        get
        {
            lock (refLock)
            {
                return refs.TryGetValue(key, out var r) ? r.refs.Count : 0;
            }
        }
    }

    /// <summary>
    /// Creates a new reference
    /// </summary>
    /// <param name="key">The key to identify this group of references</param>
    /// <param name="content">The content that is stored in this reference</param>
    /// <param name="freeCallback">A callback that will be called to cleanup whatever resources once the reference count becomes zero</param>
    public Ref(K key, V content, FreeCallback freeCallback)
    {
        this.key = key;
        this.freeCallback = freeCallback;

        lock (refLock)
        {
            if (!refs.TryGetValue(key, out container))
            {
                container = new()
                {
                    content = content,
                };

                refs.Add(key, container);
            }

            container.refs.Add(this);
        }
    }

    /// <summary>
    /// Creates a copy of this reference. The new copy increases the ref count.
    /// </summary>
    /// <returns>The copy</returns>
    public Ref<K, V> Clone() => new(key, content, freeCallback);

    /// <summary>
    /// Replaces the content shared by a group of references
    /// </summary>
    /// <param name="key">The key to replace</param>
    /// <param name="content">The new content</param>
    public static void Replace(K key, V content)
    {
        lock (refLock)
        {
            if (!refs.TryGetValue(key, out var r) || r.refs.Count == 0)
            {
                return;
            }

            r.content = content;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            disposed = true;

            lock (refLock)
            {
                if (!refs.TryGetValue(key, out var r))
                {
                    return;
                }

                if (r.refs.Count > 0)
                {
                    r.refs.Remove(this);

                    if (r.refs.Count == 0)
                    {
                        refs.Remove(key);

                        var c = r.content;

                        r.content = default;

                        freeCallback?.Invoke(key, c);
                    }
                }
            }
        }
    }

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }
}
