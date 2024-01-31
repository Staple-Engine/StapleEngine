namespace Staple.Editor;

public interface IBuildPreprocessor
{
    void OnPreprocessBuild(BuildInfo buildInfo);
}
