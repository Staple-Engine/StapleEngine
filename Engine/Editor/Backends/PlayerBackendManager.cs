using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Editor;

/// <summary>
/// Loads and manages player backends
/// </summary>
internal class PlayerBackendManager
{
    private readonly List<PlayerBackend> backends = new();

    /// <summary>
    /// The name of each backend
    /// </summary>
    public static string[] BackendNames { get; private set; }

    public static readonly PlayerBackendManager Instance = new();

    /// <summary>
    /// Loads and initializes each player backend
    /// </summary>
    public void Initialize()
    {
        if(backends.Count != 0)
        {
            return;
        }

        var basePath = Path.Combine(EditorUtils.EditorPath.Value, "PlayerBackends");

        try
        {
            var directories = Directory.GetDirectories(basePath);

            foreach(var directory in directories)
            {
                try
                {
                    var json = File.ReadAllText(Path.Combine(directory, "PlayerBackend.json"));

                    var backend = JsonConvert.DeserializeObject<PlayerBackend>(json);

                    if(backend != null && backend.IsValid())
                    {
                        backend.basePath = directory;

                        backends.Add(backend);
                    }
                }
                catch(Exception)
                {
                    continue;
                }
            }
        }
        catch(Exception)
        {
        }

        BackendNames = backends.Select(x => x.name).ToArray();
    }

    /// <summary>
    /// Gets a player backend by name
    /// </summary>
    /// <param name="name">The backend's name</param>
    /// <returns>The backend or null</returns>
    public PlayerBackend GetBackend(string name)
    {
        return backends.FirstOrDefault(x => x.name == name);
    }

    /// <summary>
    /// Gets a player backend by platform
    /// </summary>
    /// <param name="platform">The platform to build for</param>
    /// <returns>The backend or null</returns>
    public PlayerBackend GetBackend(AppPlatform platform)
    {
        return backends.FirstOrDefault(x => x.platform == platform);
    }
}
