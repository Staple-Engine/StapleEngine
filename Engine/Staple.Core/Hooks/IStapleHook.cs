namespace Staple.Internal;

public interface IStapleHook
{
    public string Name { get; }

    public StapleHookEvent[] HookedEvents { get; }

    public void OnEvent(StapleHookEvent e, object args);
}
