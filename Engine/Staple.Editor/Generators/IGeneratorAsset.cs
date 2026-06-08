namespace Staple.Editor;

public interface IGeneratorAsset
{
    string Extension { get; }

    bool IsText { get; }

    byte[] CreateNew();

    byte[] Generate();

    abstract static bool Load(string guid, out IGeneratorAsset asset);
}
