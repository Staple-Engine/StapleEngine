namespace Staple.Editor;

/// <summary>
/// Interface for build preprocessors.
/// They execute code before a build is started, so you can modify the contents.
/// </summary>
public interface IBuildPreprocessor
{
    /// <summary>
    /// Called before a build is started
    /// </summary>
    /// <param name="buildInfo">The current build info</param>
    /// <returns>The result of processing the build</returns>
    BuildProcessorResult OnPreprocessBuild(BuildInfo buildInfo);
}
