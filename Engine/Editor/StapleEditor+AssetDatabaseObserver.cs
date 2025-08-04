using Staple.Internal;

namespace Staple.Editor;

internal partial class StapleEditor : IAssetDatabaseObserver
{
    public void AssetDatabaseSetProgress(float progress, string message) => SetBackgroundProgress(progress, message);
}
