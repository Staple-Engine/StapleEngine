namespace Staple.Internal;

public readonly struct ResourceHandle<T>(ushort handle)
{
    public static ResourceHandle<T> Invalid => new(ushort.MaxValue);

    public readonly ushort handle = handle;

    public readonly bool IsValid => handle != Invalid.handle;
}
