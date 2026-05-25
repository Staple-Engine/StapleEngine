using System;
using System.Collections.Generic;
using System.Threading;

namespace Staple.Internal;

public class RefCountedResourceHandle : IDisposable
{
    public delegate void FreeCallback(StringID key, object content);

    private static readonly Dictionary<StringID, int> refs = [];
    private static readonly Lock refLock = new();
    private bool disposed;
    private readonly StringID key;
    private readonly FreeCallback freeCallback;

    public readonly object content;

    public bool IsValid => !disposed && content is not null;

    internal int RefCount
    {
        get
        {
            lock(refLock)
            {
                return refs.TryGetValue(key, out var r) ? r : 0;
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
            var r = refs.GetValueOrDefault(key, 0);

            r++;

            refs[key] = r;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            lock(refLock)
            {
                var r = refs.GetValueOrDefault(key, 0);

                if(r > 0)
                {
                    r--;

                    if (r <= 0)
                    {
                        freeCallback?.Invoke(key, content);

                        refs.Remove(key);
                    }
                    else
                    {
                        refs[key] = r;
                    }
                }
            }

            disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }
}
