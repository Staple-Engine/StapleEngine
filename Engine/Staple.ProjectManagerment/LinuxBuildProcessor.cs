using Staple.Internal;
using System;
using System.IO;

namespace Staple.ProjectManagement;

public class LinuxBuildProcessor : IBuildPreprocessor
{
    public BuildProcessorResult OnPreprocessBuild(BuildInfo buildInfo)
    {
        if (buildInfo.platform != AppPlatform.Linux)
        {
            return BuildProcessorResult.Continue;
        }

        var projectDirectory = buildInfo.assemblyProjectPath;

        if(StorageUtils.CopyFile(Path.Combine(buildInfo.backendResourcesPath, "Program.cs"), Path.Combine(projectDirectory, "Program.cs")) == false)
        {
            Log.Debug($"{GetType().Name}: Failed to copy program script");

            return BuildProcessorResult.Failed;
        }

        return BuildProcessorResult.Success;
    }
}
