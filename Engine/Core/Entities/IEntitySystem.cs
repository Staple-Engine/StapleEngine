namespace Staple
{
    public interface IEntitySystem
    {
        void Process(World world, float deltaTime);
    }
}
