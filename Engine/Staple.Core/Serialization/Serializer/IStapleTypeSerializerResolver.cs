#nullable enable

namespace Staple.Internal;

public interface IStapleTypeSerializerResolver
{
    IStapleTypeSerializer<T>? GetSerializer<T>();
}
