namespace Staple.Editor;

/// <summary>
/// Interface for build postprocessors.
/// They execute code after a build is finished, so you can modify the results.
/// </summary>
public interface IBuildPostprocessor
{
    /// <summary>
    /// Called when a build is finished
    /// </summary>
    /// <param name="buildInfo">The current build info</param>
    /// <returns>The result of processing the build</returns>
    BuildProcessorResult OnPostprocessBuild(BuildInfo buildInfo);
}
