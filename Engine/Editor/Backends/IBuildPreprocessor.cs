namespace Staple.Editor;

public interface IBuildPreprocessor
{
    BuildProcessorResult OnPreprocessBuild(BuildInfo buildInfo);
}
