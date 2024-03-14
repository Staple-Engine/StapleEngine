using Staple;

class MyEntitySystem : IEntitySystem
{
    public EntitySubsystemType UpdateType => EntitySubsystemType.FixedUpdate;

    public void FixedUpdate(float deltaTime)
    {
        //Handle each update here
    }

    public void Update(float deltaTime)
    {
        //Handle each update here
    }

    public void Shutdown()
    {
        //Called when the engine is shutting down
    }

    public void Startup()
    {
        //Called when the engine is starting up
    }
}
