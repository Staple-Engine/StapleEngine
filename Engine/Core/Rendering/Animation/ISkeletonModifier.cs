namespace Staple;

public interface ISkeletonModifier : IComponent
{
    void Apply(Transform bone, bool wasReset);
}
