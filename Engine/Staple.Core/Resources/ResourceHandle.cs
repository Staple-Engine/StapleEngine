namespace Staple.Internal;

public class ResourceHandle<T>(object context = null)
{
    public static readonly ResourceHandle<T> Invalid = new();

    internal object context = context;

    public bool IsValid => context != null;

    public void Clear()
    {
        context = null;
    }
}
