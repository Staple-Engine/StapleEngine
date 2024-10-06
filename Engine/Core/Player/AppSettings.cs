using MessagePack;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Staple;

/// <summary>
/// Stores information on app data
/// </summary>
[MessagePackObject]
[Serializable]
public class AppSettings
{
    /// <summary>
    /// What kind of profiling to use
    /// </summary>
    public enum ProfilingMode
    {
        None,
        PerformanceOverlay,
        RenderOverlay,
    }

    /// <summary>
    /// Whether to run in the background (not pause)
    /// </summary>
    [Key(0)]
    public bool runInBackground = false;

    /// <summary>
    /// The app's name
    /// </summary>
    [Key(1)]
    public string appName;

    /// <summary>
    /// The company's name
    /// </summary>
    [Key(2)]
    public string companyName;

    /// <summary>
    /// How many frames per second to run in fixed time
    /// </summary>
    [Key(3)]
    public int fixedTimeFrameRate = 50;

    /// <summary>
    /// Whether to use the multithreaded renderer
    /// </summary>
    [Key(4)]
    public bool multiThreadedRenderer = false;

    /// <summary>
    /// Which layers to use
    /// </summary>
    [Key(5)]
    public List<string> layers = [];

    /// <summary>
    /// Which sorting layers to use
    /// </summary>
    [Key(6)]
    public List<string> sortingLayers = [];

    /// <summary>
    /// Which renderers to use per platform
    /// </summary>
    [Key(7)]
    public Dictionary<AppPlatform, List<RendererType>> renderers = [];

    /// <summary>
    /// Default mode for the game window
    /// </summary>
    [Key(8)]
    public WindowMode defaultWindowMode = WindowMode.BorderlessFullscreen;

    /// <summary>
    /// Default width of the game window
    /// </summary>
    [Key(9)]
    public int defaultWindowWidth = 1024;

    /// <summary>
    /// Default height of the game window
    /// </summary>
    [Key(10)]
    public int defaultWindowHeight = 768;

    /// <summary>
    /// Mask of collision layers
    /// </summary>
    [Key(11)]
    public List<ColliderMask.Item> colliderMask = [];

    /// <summary>
    /// The app's bundle ID
    /// </summary>
    [Key(12)]
    public string appBundleID = "com.companyName.TestApp";

    /// <summary>
    /// The app build ID
    /// </summary>
    [Key(13)]
    public int appVersion = 1;

    /// <summary>
    /// The display value for the app version
    /// </summary>
    [Key(14)]
    public string appDisplayVersion = "1.0";

    /// <summary>
    /// Android Minimum SDK
    /// </summary>
    [Key(15)]
    public int androidMinSDK = 26;

    /// <summary>
    /// iOS Minimum deployment target
    /// </summary>
    [Key(16)]
    public int iOSDeploymentTarget = 14;

    /// <summary>
    /// Whether to allow portrait orientation
    /// </summary>
    [Key(17)]
    public bool portraitOrientation = true;

    /// <summary>
    /// Whether to allow landscape orientation
    /// </summary>
    [Key(18)]
    public bool landscapeOrientation = true;

    /// <summary>
    /// Maximum time to run fixed timesteps
    /// </summary>
    [Key(19)]
    public float maximumFixedTimestepTime = 0.1f;

    /// <summary>
    /// The frame rate at which the physics should simulate
    /// </summary>
    [Key(20)]
    public int physicsFrameRate = 50;

    /// <summary>
    /// Used modules for the player
    /// </summary>
    [Key(21)]
    public Dictionary<ModuleType, string> usedModules = new();

    /// <summary>
    /// What color to use for ambient lighting
    /// </summary>
    [Key(22)]
    public Color ambientLight = Color.LightBlue;

    /// <summary>
    /// Whether to show the performance profiler overlay
    /// </summary>
    [Key(23)]
    public ProfilingMode profilingMode = ProfilingMode.None;

    [IgnoreMember]
    public static AppSettings Default
    {
        get
        {
            return new AppSettings()
            {
                appName = "Game",
                companyName = "Default Company",
                appBundleID = "com.companyname.TestApp",
                appVersion = 1,
                appDisplayVersion = "1.0",
                layers =
                [
                    "Default",
                ],
                sortingLayers =
                [
                    "Default",
                ],
                renderers = new()
                {
                    {
                        AppPlatform.Windows,
                        new()
                        {
                            RendererType.Direct3D12, RendererType.Direct3D11, RendererType.Vulkan
                        }
                    },
                    {
                        AppPlatform.Linux,
                        new()
                        {
                            RendererType.Vulkan, RendererType.OpenGL
                        }
                    },
                    {
                        AppPlatform.MacOSX,
                        new()
                        {
                            RendererType.Metal
                        }
                    },
                    {
                        AppPlatform.Android,
                        new()
                        {
                            RendererType.Vulkan, RendererType.OpenGLES
                        }
                    },
                    {
                        AppPlatform.iOS,
                        new()
                        {
                            RendererType.Metal
                        }
                    }
                },
                usedModules = new()
                {
                    { ModuleType.Audio, "StapleOpenALAudio" },
                    { ModuleType.Physics, "StapleJoltPhysics" },
                },
            };
        }
    }

    [IgnoreMember]
    internal static AppSettings Current;

    internal AppSettings Clone()
    {
        var newRenderers = new Dictionary<AppPlatform, List<RendererType>>();

        foreach(var pair in renderers)
        {
            newRenderers.Add(pair.Key, pair.Value.Select(x => x).ToList());
        }

        return new()
        {
            androidMinSDK = androidMinSDK,
            appBundleID = appBundleID,
            appVersion = appVersion,
            appDisplayVersion = appDisplayVersion,
            appName = appName,
            colliderMask = colliderMask.Select(x => x.Clone()).ToList(),
            companyName = companyName,
            defaultWindowHeight = defaultWindowHeight,
            defaultWindowWidth = defaultWindowWidth,
            defaultWindowMode = defaultWindowMode,
            fixedTimeFrameRate = fixedTimeFrameRate,
            iOSDeploymentTarget = iOSDeploymentTarget,
            landscapeOrientation = landscapeOrientation,
            layers = layers.Select(x => x).ToList(),
            multiThreadedRenderer = multiThreadedRenderer,
            portraitOrientation = portraitOrientation,
            renderers = newRenderers,
            runInBackground = runInBackground,
            sortingLayers = sortingLayers.Select(x => x).ToList(),
            usedModules = new(usedModules),
            ambientLight = ambientLight,
            maximumFixedTimestepTime = maximumFixedTimestepTime,
            physicsFrameRate = physicsFrameRate,
            profilingMode = profilingMode,
        };
    }
}

[JsonSourceGenerationOptions(IncludeFields = true)]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(Dictionary<AppPlatform, List<RendererType>>))]
[JsonSerializable(typeof(JsonStringEnumConverter<AppPlatform>))]
[JsonSerializable(typeof(JsonStringEnumConverter<RendererType>))]
[JsonSerializable(typeof(JsonStringEnumConverter<WindowMode>))]
[JsonSerializable(typeof(List<ColliderMask.Item>))]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(int))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(AppSettings))]
internal partial class AppSettingsSerializationContext : JsonSerializerContext
{
}
