namespace Staple.Editor;

/// <summary>
/// Contains information on a build in progress
/// </summary>
public class BuildInfo
{
    /// <summary>
    /// The path of the project
    /// </summary>
    public readonly string basePath;

    /// <summary>
    /// The path of the C# assembly project
    /// </summary>
    public readonly string assemblyProjectPath;

    /// <summary>
    /// The output path
    /// </summary>
    public readonly string outPath;

    /// <summary>
    /// The path of the assets cache
    /// </summary>
    public readonly string assetsCacheDirectory;

    /// <summary>
    /// The path for the resources of the project
    /// </summary>
    public readonly string targetResourcesPath;

    /// <summary>
    /// The path to the backend's resources
    /// </summary>
    public readonly string backendResourcesPath;

    /// <summary>
    /// The current platform
    /// </summary>
    public readonly AppPlatform platform;

    /// <summary>
    /// The project app settings
    /// </summary>
    public readonly AppSettings projectAppSettings;

    internal BuildInfo(string basePath, string assemblyProjectPath, string outPath, string assetsCacheDirectory,
        string targetResourcesPath, string backendResourcesPath, AppPlatform platform, AppSettings projectAppSettings)
    {
        this.basePath = basePath;
        this.assemblyProjectPath = assemblyProjectPath;
        this.outPath = outPath;
        this.assetsCacheDirectory = assetsCacheDirectory;
        this.backendResourcesPath = backendResourcesPath;
        this.targetResourcesPath = targetResourcesPath;
        this.platform = platform;
        this.projectAppSettings = projectAppSettings;
    }
}
