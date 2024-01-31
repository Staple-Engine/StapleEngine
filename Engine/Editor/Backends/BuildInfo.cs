namespace Staple.Editor;

public class BuildInfo
{
    public readonly string basePath;
    public readonly string assemblyProjectPath;
    public readonly string outPath;
    public readonly string assetsCacheDirectory;
    public readonly string targetResourcesPath;
    public readonly AppPlatform platform;
    public readonly AppSettings projectAppSettings;

    internal BuildInfo(string basePath, string assemblyProjectPath, string outPath, string assetsCacheDirectory,
        string targetResourcesPath, AppPlatform platform, AppSettings projectAppSettings)
    {
        this.basePath = basePath;
        this.assemblyProjectPath = assemblyProjectPath;
        this.outPath = outPath;
        this.assetsCacheDirectory = assetsCacheDirectory;
        this.targetResourcesPath = targetResourcesPath;
        this.platform = platform;
        this.projectAppSettings = projectAppSettings;
    }
}
