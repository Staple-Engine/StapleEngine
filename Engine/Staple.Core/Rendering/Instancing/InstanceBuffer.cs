using System;

namespace Staple;

internal abstract class InstanceBuffer
{
    public abstract void Bind(int start, int count);

    public abstract void SetData(Span<byte> data);

    public abstract void SetData<T>(Span<T> data) where T : unmanaged;

    public static int AvailableInstances(int requested, int stride)
    {
        return 0;
    }
}
