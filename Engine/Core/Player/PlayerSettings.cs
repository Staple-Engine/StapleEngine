using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Staple
{
    /// <summary>
    /// Saves settings for the player
    /// </summary>
    [Serializable]
    internal class PlayerSettings
    {
        public WindowMode windowMode { get; set; } = WindowMode.Windowed;

        public VideoFlags videoFlags { get; set; } = VideoFlags.Vsync;

        public int screenWidth { get; set; }

        public int screenHeight { get; set; }

        public int monitorIndex { get; set; } = 0;

        public Vector2Int windowPosition { get; set; }

        public bool maximized { get; set; } = false;

        [JsonIgnore]
        public int AALevel
        {
            get
            {
                return videoFlags.HasFlag(VideoFlags.MSAAX2) ? 2 :
                    videoFlags.HasFlag(VideoFlags.MSAAX4) ? 4 :
                    videoFlags.HasFlag(VideoFlags.MSAAX8) ? 8 :
                    videoFlags.HasFlag(VideoFlags.MSAAX16) ? 16 : 0;
            }
        }

        public static PlayerSettings Load(AppSettings appSettings)
        {
            try
            {
                var data = File.ReadAllText(Path.Combine(Storage.PersistentDataPath, "PlayerSettings.json"));

                return JsonSerializer.Deserialize(data, PlayerSettingsSerializationContext.Default.PlayerSettings);
            }
            catch (Exception e)
            {
                return new PlayerSettings()
                {
                    windowMode = appSettings.defaultWindowMode,
                    screenWidth = appSettings.defaultWindowWidth,
                    screenHeight = appSettings.defaultWindowHeight,
                };
            }
        }

        public static void Save(PlayerSettings playerSettings)
        {
            try
            {
                var data = JsonSerializer.Serialize(playerSettings, PlayerSettingsSerializationContext.Default.PlayerSettings);

                File.WriteAllText(Path.Combine(Storage.PersistentDataPath, "PlayerSettings.json"), data);
            }
            catch (System.Exception)
            {
            }
        }
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true, IncludeFields = true)]
    [JsonSerializable(typeof(PlayerSettings))]
    [JsonSerializable(typeof(Vector2Int))]
    [JsonSerializable(typeof(WindowMode))]
    [JsonSerializable(typeof(VideoFlags))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(bool))]
    internal partial class PlayerSettingsSerializationContext : JsonSerializerContext
    {
    }
}
