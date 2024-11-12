namespace Staple.Internal;

public sealed class GameRegistration
{
    public void RegisterAll()
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();
    }
}
