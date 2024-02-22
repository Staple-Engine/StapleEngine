namespace Staple.Editor;

public interface IEntityTemplate
{
    string Name { get; }

    Entity Create();
}
