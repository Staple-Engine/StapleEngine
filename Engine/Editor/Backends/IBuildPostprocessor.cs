namespace Staple.Editor;

public interface IBuildPostprocessor
{
    void OnPostprocessBuild(BuildInfo buildInfo);
}
