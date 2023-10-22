using System.Diagnostics.CodeAnalysis;

namespace Staple
{
    /// <summary>
    /// Entity system.
    /// You can implement this interface in order to modify entities.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
        DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
        DynamicallyAccessedMemberTypes.PublicMethods |
        DynamicallyAccessedMemberTypes.NonPublicMethods |
        DynamicallyAccessedMemberTypes.PublicFields |
        DynamicallyAccessedMemberTypes.NonPublicFields)]
    public interface IEntitySystem
    {
        SubsystemType UpdateType { get; }

        void Startup();

        void Process(World world, float deltaTime);

        void Shutdown();
    }
}
