using System.IO;
using System;

namespace Staple.Editor;

internal class LinuxBuildProcessor : IBuildPreprocessor
{
    public BuildProcessorResult OnPreprocessBuild(BuildInfo buildInfo)
    {
        if (buildInfo.platform != AppPlatform.Linux)
        {
            return BuildProcessorResult.Continue;
        }

        var projectDirectory = buildInfo.assemblyProjectPath;

        if(EditorUtils.CopyFile(Path.Combine(buildInfo.backendResourcesPath, "Program.cs"), Path.Combine(projectDirectory, "Program.cs")) == false)
        {
            Log.Debug($"{GetType().Name}: Failed to copy program script");

            return BuildProcessorResult.Failed;
        }

        return BuildProcessorResult.Success;
    }
}
