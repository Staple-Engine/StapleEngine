using System.Collections.Generic;

namespace Staple.Internal;

public interface IComponentSerializer
{
}

public interface IComponentSerializer<T>: IComponentSerializer
{
    void Deserialize(T component, Dictionary<string, object> fields);
}
