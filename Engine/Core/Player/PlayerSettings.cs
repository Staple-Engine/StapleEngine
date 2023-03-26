using Newtonsoft.Json;
using System;

namespace Staple
{
    /// <summary>
    /// Saves settings for the player
    /// </summary>
    [Serializable]
    internal class PlayerSettings
    {
        public WindowMode windowMode = WindowMode.Windowed;
        public VideoFlags videoFlags = VideoFlags.Vsync;
        public int screenWidth;
        public int screenHeight;
        public int monitorIndex = 0;

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
}
