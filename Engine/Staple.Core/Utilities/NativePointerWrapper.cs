namespace Staple.Utilities;

public unsafe class NativePointerWrapper<T>(T *ptr) where T: unmanaged
{
    public T* ptr = ptr;

    public bool Valid => ptr != null;

    public void Clear()
    {
        ptr = null;
    }
}
