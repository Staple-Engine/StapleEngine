using System;
using System.IO;

namespace Staple.Editor;

internal partial class StapleEditor
{
    /// <summary>
    /// Updates the game project and then builds and loads the game
    /// </summary>
    /// <param name="platform">The platform to build for</param>
    /// <param name="checkBuild">Check whether to build</param>
    /// <param name="onFinish">Called when finished</param>
    internal void UpdateCSProj(AppPlatform platform, bool checkBuild, Action onFinish)
    {
        var backend = PlayerBackendManager.Instance.GetBackend(buildBackend);

        if(backend == null)
        {
            onFinish();

            return;
        }

        UnloadGame();

        csProjManager.GenerateGameCSProj(backend, projectAppSettings, platform, false);
        csProjManager.GenerateGameCSProj(backend, projectAppSettings, platform, true);

        void Build()
        {
            BuildGame(() =>
            {
                onFinish();
            });
        }

        if (checkBuild)
        {
            try
            {
                var gameChange = DateTime.MinValue;

                var targetPath = Path.Combine(BasePath, "Cache", "Assembly", "Game", "bin", "Game.dll");

                if (File.Exists(targetPath))
                {
                    gameChange = File.GetLastWriteTime(targetPath);
                }

                var assetsDirectory = Path.Combine(BasePath, "Assets");

                var lastChange = csProjManager.GetLastFileChange(assetsDirectory);

                var editorPath = Path.Combine(AppContext.BaseDirectory, "StapleEditor.dll");

                if (File.Exists(editorPath))
                {
                    var date = File.GetLastWriteTime(editorPath);

                    if(date > lastChange)
                    {
                        lastChange = date;
                    }
                }

                var corePath = Path.Combine(AppContext.BaseDirectory, "StapleCore.dll");

                if (File.Exists(corePath))
                {
                    var date = File.GetLastWriteTime(corePath);

                    if (date > lastChange)
                    {
                        lastChange = date;
                    }
                }

                if (lastChange > gameChange)
                {
                    Build();
                }
                else
                {
                    onFinish();
                }
            }
            catch (Exception)
            {
                Build();
            }
        }
        else
        {
            Build();
        }
    }
}
