namespace Staple;

/// <summary>
/// Component with callable unity-style events.
/// You should derive your components from this if you'd rather work without a custom system for that component type.
/// Just remember, it's less effective than using a system if there's a lot of entities doing things!
/// </summary>
[AbstractComponent]
[AutoAssignEntity]
public class CallbackComponent : IComponent
{
    /// <summary>
    /// Flag for knowing when to emit the Start() event
    /// There are better ways to do this, but for now this works!
    /// </summary>
    internal bool STAPLE_JUST_ADDED = true;

    /// <summary>
    /// The entity this belongs to
    /// </summary>
    public Entity entity { get; internal set; }

    /// <summary>
    /// Called when this component is added to its object
    /// </summary>
    public virtual void Awake()
    {
    }

    /// <summary>
    /// Called after the first frame this component was added to its object
    /// </summary>
    public virtual void Start()
    {
    }

    /// <summary>
    /// Called when this component is being destroyed
    /// </summary>
    public virtual void OnDestroy()
    {
    }

    /// <summary>
    /// Called once per frame.
    /// You should use Time.deltaTime if you need a delta time here.
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    /// Called once per fixed update tick.
    /// You should use Time.fixedDeltaTime if you need a delta time here.
    /// </summary>
    public virtual void FixedUpdate()
    {
    }

    /// <summary>
    /// Called at the end of each frame
    /// </summary>
    public virtual void LateUpdate()
    {
    }
}
