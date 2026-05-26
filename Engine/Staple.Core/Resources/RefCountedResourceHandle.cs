using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple.Internal;

public class RefCountedResourceHandle : IDisposable
{
    private class RefContainer
    {
        public readonly List<RefCountedResourceHandle> refs = [];
    }

    public delegate void FreeCallback(StringID key, object content);

    private static readonly Dictionary<StringID, RefContainer> refs = [];
    private static readonly Lock refLock = new();
    private bool disposed;
    private readonly StringID key;
    private readonly FreeCallback freeCallback;

    public object content;

    public bool IsValid => !disposed && content is not null;

    internal int RefCount
    {
        get
        {
            lock(refLock)
            {
                return refs.TryGetValue(key, out var r) ? r.refs.Count : 0;
            }
        }
    }

    internal static void Replace(StringID key, object content)
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

    public RefCountedResourceHandle(StringID key, object content, FreeCallback freeCallback)
    {
        this.key = key;
        this.content = content;
        this.freeCallback = freeCallback;

        lock(refLock)
        {
            if(!refs.TryGetValue(key, out var r))
            {
                r = new();

                refs.Add(key, r);
            }

            r.refs.Add(this);
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            disposed = true;

            lock (refLock)
            {
                if(!refs.TryGetValue(key, out var r))
                {
                    return;
                }

                if(r.refs.Count > 0)
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
