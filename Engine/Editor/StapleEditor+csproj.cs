using System;

namespace Staple.Editor;

internal partial class StapleEditor
{
    /// <summary>
    /// Updates the game project and then builds and loads the game
    /// </summary>
    /// <param name="platform">The platform to build for</param>
    /// <param name="onFinish">Called when finished</param>
    internal void UpdateCSProj(AppPlatform platform, Action onFinish)
    {
        var backend = PlayerBackendManager.Instance.GetBackend(buildBackend);

        if(backend == null)
        {
            onFinish();

            return;
        }

        csProjManager.GenerateGameCSProj(backend, platform, false);
        csProjManager.GenerateGameCSProj(backend, platform, true);

        BuildGame(() =>
        {
            onFinish();
        });
    }
}
