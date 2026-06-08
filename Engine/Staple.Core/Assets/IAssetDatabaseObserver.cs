namespace Staple.Internal;

public interface IAssetDatabaseObserver
{
    void AssetDatabaseSetProgress(float progress, string message);
}
