using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    [Serializable]
    internal class PlayerSettings
    {
        public enum WindowMode
        {
            Windowed,
            Fullscreen,
            Borderless
        }

        [Flags]
        public enum VideoFlags
        {
            None = 0,
            Vsync = (1 << 1),
            MsaaX2 = (1 << 2),
            MsaaX4 = (1 << 3),
            MsaaX8 = (1 << 4),
            MsaaX16 = (1 << 5),
            HDR10 = (1 << 6),
            HiDpi = (1 << 7),
        }

        public WindowMode windowMode = WindowMode.Windowed;
        public VideoFlags videoFlags = VideoFlags.Vsync;
        public int screenWidth;
        public int screenHeight;
        public int monitorIndex = 0;

        public int AALevel
        {
            get
            {
                return videoFlags.HasFlag(VideoFlags.MsaaX2) ? 2 :
                    videoFlags.HasFlag(VideoFlags.MsaaX4) ? 4 :
                    videoFlags.HasFlag(VideoFlags.MsaaX8) ? 8 :
                    videoFlags.HasFlag(VideoFlags.MsaaX16) ? 16 : 0;
            }
        }
    }
}
