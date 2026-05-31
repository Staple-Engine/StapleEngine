using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple.Internal;

public class RefCountedContainer<K, V> : IDisposable
{
    private class RefContainer
    {
        public readonly List<RefCountedContainer<K, V>> refs = [];
    }

    public delegate void FreeCallback(K key, object content);

    private static readonly Dictionary<K, RefContainer> refs = [];
    private static readonly Lock refLock = new();
    private bool disposed;
    private readonly K key;
    private readonly FreeCallback freeCallback;

    public object content;

    public bool IsValid => !disposed && content is not null;

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

    public RefCountedContainer(K key, object content, FreeCallback freeCallback)
    {
        this.key = key;
        this.content = content;
        this.freeCallback = freeCallback;

        lock (refLock)
        {
            if (!refs.TryGetValue(key, out var r))
            {
                r = new();

                refs.Add(key, r);
            }

            r.refs.Add(this);
        }
    }

    public static void Replace(K key, object content)
    {
        lock (refLock)
        {
            if (!refs.TryGetValue(key, out var r) || r.refs.Count == 0)
            {
                return;
            }

            foreach (var handle in r.refs)
            {
                handle.content = content;
            }
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
                        freeCallback?.Invoke(key, content);

                        refs.Remove(key);
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
