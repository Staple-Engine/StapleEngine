using Bgfx;
using ImGuiNET;
using Staple.Internal;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("StapleEditorApp")]

namespace Staple.Editor
{
    internal class StapleEditor
    {
        internal const int ClearView = 0;

        private RenderWindow window;

        private ImGuiProxy imgui;

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

            ResourceManager.instance.basePath = $"{Environment.CurrentDirectory}/Data";

            imgui = new ImGuiProxy();

            if(imgui.Initialize() == false)
            {
                window.Cleanup();

                return;
            }

            float t = 0;

            window.OnUpdate = () =>
            {
                bgfx.touch(ClearView);

                var io = ImGui.GetIO();

                io.DisplaySize = new Vector2(window.screenWidth, window.screenHeight);
                io.DisplayFramebufferScale = new Vector2(1, 1);

                imgui.BeginFrame();

                ImGui.SetNextWindowPos(new Vector2(10.0f, 50.0f), ImGuiCond.FirstUseEver);

                ImGui.SetNextWindowSize(new Vector2(500, 500), ImGuiCond.FirstUseEver);

                ImGui.Begin("Test");

                ImGui.TextWrapped("Test Text");

                ImGui.SameLine();

                ImGui.SmallButton("Test Button");

                if(ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Test Tooltip");
                }

                ImGui.InputFloat("Test Float", ref t);

                ImGui.End();

                imgui.EndFrame();
            };

            window.OnScreenSizeChange = (hasFocus) =>
            {
                var flags = AppPlayer.ResetFlags(PlayerSettings.VideoFlags.Vsync);

                bgfx.reset((uint)window.screenWidth, (uint)window.screenHeight, (uint)flags, bgfx.TextureFormat.RGBA8);
                bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);
                bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), 0x334455FF, 0, 0);
            };

            window.Run();

            imgui.Destroy();

            ResourceManager.instance.Destroy();

            window.Cleanup();
        }
    }
}