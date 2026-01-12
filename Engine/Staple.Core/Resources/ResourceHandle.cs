using System;

namespace Staple.Internal;

public readonly struct ResourceHandle<T>(ushort handle)
{
    public static readonly ResourceHandle<T> Invalid = new(ushort.MaxValue);

    public readonly ushort handle = handle;

    public readonly bool IsValid => handle != ushort.MaxValue;

    public override int GetHashCode() => HashCode.Combine(handle);
}
