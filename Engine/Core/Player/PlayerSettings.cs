using System;
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
    }

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, WriteIndented = true)]
    [JsonSerializable(typeof(PlayerSettings))]
    [JsonSerializable(typeof(WindowMode))]
    [JsonSerializable(typeof(VideoFlags))]
    [JsonSerializable(typeof(int))]
    internal partial class PlayerSettingsSerializationContext : JsonSerializerContext
    {
    }
}
