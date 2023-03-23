namespace Staple
{
    /// <summary>
    /// Entity system.
    /// You can implement this interface in order to modify entities.
    /// </summary>
    public interface IEntitySystem
    {
        void Process(World world, float deltaTime);
    }
}
