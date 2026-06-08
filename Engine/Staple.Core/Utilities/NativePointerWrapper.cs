namespace Staple.Utilities;

public unsafe class NativePointerWrapper<T>(T *ptr) where T: unmanaged
{
    public T* ptr = ptr;
}
