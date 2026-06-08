using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Staple.ProjectManagement;

/// <summary>
/// Describes a backend for building a game build (Player)
/// </summary>
[Serializable]
public class PlayerBackend
{
    /// <summary>
    /// The base directory of the backend's files (internal)
    /// </summary>
    [JsonIgnore]
    public string basePath;

    /// <summary>
    /// The backend's name
    /// </summary>
    public string name;

    /// <summary>
    /// The platform it implements
    /// </summary>
    public AppPlatform platform;

    /// <summary>
    /// Which directory should the redistributable be saved to
    /// </summary>
    public string redistOutput;

    /// <summary>
    /// The data directory name
    /// </summary>
    public string dataDir;

    /// <summary>
    /// The platform runtime
    /// </summary>
    public string platformRuntime;

    /// <summary>
    /// The framework to use
    /// </summary>
    public string framework;

    /// <summary>
    /// Whether to publish or build
    /// </summary>
    public bool publish;

    /// <summary>
    /// Whether the data dir is inside the output folder
    /// </summary>
    public bool dataDirIsOutput;

    /// <summary>
    /// The renderers this supports
    /// </summary>
    public List<RendererType> renderers = [];

    public bool IsValid()
    {
        return (name?.Length ?? 0) > 0 &&
            Enum.GetValues<AppPlatform>().Contains(platform) &&
            (redistOutput?.Length ?? 0) > 0 &&
            (dataDir?.Length ?? 0) > 0 &&
            (platformRuntime?.Length ?? 0) > 0 &&
            (framework?.Length ?? 0) > 0 &&
            (renderers?.Count ?? 0) > 0;
    }
}