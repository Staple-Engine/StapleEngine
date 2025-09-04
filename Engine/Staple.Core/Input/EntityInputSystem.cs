namespace Staple.Internal;

public sealed class EntityInputSystem : IEntitySystemLifecycle, IEntitySystemFixedUpdate
{
    private readonly SceneQuery<EntityInput, IInputReceiver> inputs = new();

    public void FixedUpdate(float deltaTime)
    {
        foreach(var (_, input, receiver) in inputs.Contents)
        {
            if(input.prevActions != input.actions)
            {
                if ((input.cachedActionIDs?.Length ?? 0) > 0)
                {
                    foreach (var id in input.cachedActionIDs)
                    {
                        Input.ClearAction(id);
                    }
                }

                input.prevActions = input.actions;

                if(input.actions != null)
                {
                    input.cachedActionIDs = input.actions.RegisterActions(receiver);
                }
                else
                {
                    input.cachedActionIDs = [];
                }
            }
        }
    }

    public void Shutdown()
    {
    }

    public void Startup()
    {
    }
}
