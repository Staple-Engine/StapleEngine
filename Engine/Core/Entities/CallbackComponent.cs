namespace Staple;

/// <summary>
/// Component with callable unity-style events.
/// You should derive your components from this if you'd rather work without a custom system for that component type.
/// Just remember, it's less effective than using a system if there's a lot of entities doing things!
/// </summary>
[AbstractComponent]
public class CallbackComponent : IComponent
{
    /// <summary>
    /// Flag for knowing when to emit the Start() event
    /// There are better ways to do this, but for now this works!
    /// </summary>
    internal bool STAPLE_JUST_ADDED = true;

    public virtual void Awake()
    {
    }

    public virtual void Start()
    {
    }

    public virtual void OnDestroy()
    {
    }

    public virtual void Update()
    {
    }

    public virtual void FixedUpdate()
    {
    }

    public virtual void LateUpdate()
    {
    }
}
