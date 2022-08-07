using Bgfx;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("StapleEditorApp")]

namespace Staple.Editor
{
    internal class StapleEditor
    {
        internal const int ClearView = 0;

        private RenderWindow window;

        public void Run()
        {
            window = RenderWindow.Create(1024, 768, true, PlayerSettings.WindowMode.Windowed, new AppSettings()
            {
                appName = "Staple Editor",
            }, 0, bgfx.ResetFlags.Vsync, true);

            if(window == null)
            {
                return;
            }

            window.OnUpdate = () =>
            {
                bgfx.touch(ClearView);
            };

            window.OnScreenSizeChange = (hasFocus) =>
            {
                var flags = AppPlayer.ResetFlags(PlayerSettings.VideoFlags.Vsync);

                bgfx.reset((uint)window.screenWidth, (uint)window.screenHeight, (uint)flags, bgfx.TextureFormat.RGBA8);
                bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);
                bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), 0x334455FF, 0, 0);
            };

            window.Run();

            window.Cleanup();
        }
    }
}