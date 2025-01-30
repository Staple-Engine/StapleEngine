namespace Staple.Editor;

public interface IGenerator
{
    string Extension { get; }

    bool IsText { get; }

    byte[] CreateNew();

    byte[] Generate();

    abstract static bool Load(string guid, out IGenerator asset);
}
