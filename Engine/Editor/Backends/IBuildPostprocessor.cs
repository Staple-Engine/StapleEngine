namespace Staple.Editor;

public interface IBuildPostprocessor
{
    BuildProcessorResult OnPostprocessBuild(BuildInfo buildInfo);
}
