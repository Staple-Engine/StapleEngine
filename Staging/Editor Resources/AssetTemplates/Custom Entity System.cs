using Staple;

class MyEntitySystem : IEntitySystem
{
    public SubsystemType UpdateType => SubsystemType.FixedUpdate;

    public void Process(float deltaTime)
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
