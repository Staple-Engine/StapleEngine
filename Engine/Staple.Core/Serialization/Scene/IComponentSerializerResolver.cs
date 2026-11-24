namespace Staple.Internal;

public interface IComponentSerializerResolver
{
    IComponentSerializer<T> GetSerializer<T>();
}