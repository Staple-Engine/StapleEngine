using Staple.ProjectManagement;
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

        ShowBackgroundProcess();

        UnloadGame();

        SetBackgroundProgress(0, "Updating game project (1/2)");

        ProjectManager.Instance.GenerateGameCSProj(backend, projectAppSettings, platform, false);

        SetBackgroundProgress(0.5f, "Updating game project (2/2)");

        ProjectManager.Instance.GenerateGameCSProj(backend, projectAppSettings, platform, true);

        HideBackgroundProcess();

        void Build()
        {
            if (gameLoadDisabled)
            {
                onFinish();

                return;
            }

            buildingGame = true;

            var handle = ProjectManager.Instance.BuildGame(() =>
            {
                buildingGame = false;

                onFinish();
            }, SetBackgroundProgress);

            StartBackgroundTask(handle);
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

                var lastChange = ProjectManager.GetLastFileChange(assetsDirectory);

                var editorPath = Path.Combine(AppContext.BaseDirectory, "Staple.Editor.dll");

                if (File.Exists(editorPath))
                {
                    var date = File.GetLastWriteTime(editorPath);

                    if(date > lastChange)
                    {
                        lastChange = date;
                    }
                }

                var corePath = Path.Combine(AppContext.BaseDirectory, "Staple.Core.dll");

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
