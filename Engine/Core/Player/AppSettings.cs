using MessagePack;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Staple
{
    /// <summary>
    /// Stores information on app data
    /// </summary>
    [MessagePackObject]
    [Serializable]
    public class AppSettings
    {
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
        public int fixedTimeFrameRate = 30;

        /// <summary>
        /// Whether to use the multithreaded renderer
        /// </summary>
        [Key(4)]
        public bool multiThreadedRenderer = false;

        /// <summary>
        /// Which layers to use
        /// </summary>
        [Key(5)]
        public List<string> layers = new();

        /// <summary>
        /// Which sorting layers to use
        /// </summary>
        [Key(6)]
        public List<string> sortingLayers = new();

        /// <summary>
        /// Which renderers to use per platform
        /// </summary>
        [Key(7)]
        public Dictionary<AppPlatform, List<RendererType>> renderers = new();

        /// <summary>
        /// Default mode for the game window
        /// </summary>
        [Key(8)]
        public WindowMode defaultWindowMode = WindowMode.Borderless;

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
        public List<ColliderMask.Item> colliderMask = new();

        [IgnoreMember]
        public static AppSettings Default
        {
            get
            {
                return new AppSettings()
                {
                    appName = "Test",
                    companyName = "Test Company",
                    layers = new()
                    {
                        "Default",
                    },
                    sortingLayers = new ()
                    {
                        "Default",
                    },
                    renderers = new()
                    {
                        {
                            AppPlatform.Windows,
                            new List<RendererType>() {
                                RendererType.Direct3D12, RendererType.Direct3D11, RendererType.Vulkan, RendererType.OpenGL
                            }
                        },
                        {
                            AppPlatform.Linux,
                            new List<RendererType>() {
                                RendererType.Vulkan, RendererType.OpenGL
                            }
                        },
                        {
                            AppPlatform.MacOSX,
                            new List<RendererType>() {
                                RendererType.Metal
                            }
                        },
                        {
                            AppPlatform.Android,
                            new List<RendererType>()
                            {
                                RendererType.Vulkan,
                            }
                        },
                        {
                            AppPlatform.iOS,
                            new List<RendererType>()
                            {
                                RendererType.Metal,
                            }
                        }
                    },
                };
            }
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
}
