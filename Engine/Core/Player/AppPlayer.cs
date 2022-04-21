using Bgfx;
using GLFW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Staple
{
    public class AppPlayer
    {
        public readonly AppSettings appSettings;

        private const ushort ClearView = 0;

        private PlayerSettings playerSettings;

        private int screenWidth = 0;
        private int screenHeight = 0;

        public AppPlayer(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        private bgfx.ResetFlags ResetFlags
        {
            get
            {
                var resetFlags = bgfx.ResetFlags.SrgbBackbuffer;

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.Vsync))
                {
                    resetFlags |= bgfx.ResetFlags.Vsync;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX2))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX2;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX4))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX4;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX8))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX8;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX16))
                {
                    resetFlags |= bgfx.ResetFlags.MsaaX16;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.HDR10))
                {
                    resetFlags |= bgfx.ResetFlags.Hdr10;
                }

                if (playerSettings.videoFlags.HasFlag(PlayerSettings.VideoFlags.HiDpi))
                {
                    resetFlags |= bgfx.ResetFlags.Hidpi;
                }

                return resetFlags;
            }
        }

        public void ResetRendering(bool hasFocus)
        {
            var flags = ResetFlags;

            if(hasFocus == false && appSettings.runInBackground == false)
            {
                flags |= bgfx.ResetFlags.Suspend;
            }

            bgfx.reset((uint)screenWidth, (uint)screenHeight, (uint)flags, bgfx.TextureFormat.RGBA32U);
            bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);
        }

        public void Run()
        {
            playerSettings = new PlayerSettings()
            {
                screenWidth = 1024,
                screenHeight = 768,
            };

            Glfw.Init();

            Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
            Glfw.WindowHint(Hint.Resizable, false);

            NativeWindow window = null;

            var monitor = Glfw.Monitors.Skip(playerSettings.monitorIndex).FirstOrDefault();

            if (monitor == null)
            {
                monitor = Glfw.PrimaryMonitor;
            }

            switch (playerSettings.windowMode)
            {
                case PlayerSettings.WindowMode.Windowed:

                    window = new NativeWindow(playerSettings.screenWidth, playerSettings.screenHeight, appSettings.appName);

                    break;

                case PlayerSettings.WindowMode.Fullscreen:

                    window = new NativeWindow(playerSettings.screenWidth, playerSettings.screenHeight, appSettings.appName, monitor, Window.None);

                    break;

                case PlayerSettings.WindowMode.Borderless:

                    Glfw.WindowHint(Hint.Floating, true);
                    Glfw.WindowHint(Hint.Decorated, false);

                    var videoMode = Glfw.GetVideoMode(monitor);

                    window = new NativeWindow(videoMode.Width, videoMode.Height, appSettings.appName);

                    break;
            }

            if (window == null)
            {
                return;
            }

            bgfx.render_frame(0);

            var init = new bgfx.Init();

            unsafe
            {
                init.platformData.ndt = null;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    init.platformData.nwh = Native.GetWin32Window(window).ToPointer();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var display = Native.GetX11Display();
                    var windowHandle = Native.GetX11Window(window);

                    if (display == IntPtr.Zero || window == IntPtr.Zero)
                    {
                        display = Native.GetWaylandDisplay();
                        windowHandle = Native.GetWaylandWindow(window);
                    }

                    init.platformData.ndt = display.ToPointer();
                    init.platformData.nwh = windowHandle.ToPointer();
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    init.platformData.ndt = (void*)Native.GetCocoaMonitor(window.Monitor);
                    init.platformData.nwh = Native.GetCocoaWindow(window).ToPointer();
                }
            }

            Glfw.GetFramebufferSize(window, out screenWidth, out screenHeight);

            init.type = bgfx.RendererType.Count;
            init.resolution.width = (uint)screenWidth;
            init.resolution.height = (uint)screenHeight;
            init.resolution.reset = (uint)ResetFlags;

            unsafe
            {
                if (!bgfx.init(&init))
                {
                    return;
                }
            }

            ushort kClearView = 0;

            bgfx.set_view_clear(kClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), 0x334455FF, 24, 0);
            bgfx.set_view_rect_ratio(kClearView, 0, 0, bgfx.BackbufferRatio.Equal);

            bool hasFocus = window.IsFocused;

            if(appSettings.runInBackground == false && hasFocus == false)
            {
                ResetRendering(hasFocus);
            }

            while (!Glfw.WindowShouldClose(window) && window.IsClosed == false)
            {
                Glfw.PollEvents();

                Glfw.GetFramebufferSize(window, out var currentW, out var currentH);

                if (currentW != screenWidth || currentH != screenHeight)
                {
                    screenWidth = currentW;
                    screenHeight = currentH;

                    ResetRendering(hasFocus);
                }

                if(appSettings.runInBackground == false && window.IsFocused != hasFocus)
                {
                    hasFocus = window.IsFocused;

                    ResetRendering(hasFocus);
                }

                bgfx.touch(0);

                bgfx.dbg_text_clear(0, false);
                bgfx.dbg_text_printf(0, 0, 0x0f, "Press F1 to toggle stats.", "");
                bgfx.dbg_text_printf(0, 1, 0x0f, "Color can be changed with ANSI \x1b[9;me\x1b[10;ms\x1b[11;mc\x1b[12;ma\x1b[13;mp\x1b[14;me\x1b[0m code too.", "");
                bgfx.dbg_text_printf(80, 1, 0x0f, "\x1b[;0m    \x1b[;1m    \x1b[; 2m    \x1b[; 3m    \x1b[; 4m    \x1b[; 5m    \x1b[; 6m    \x1b[; 7m    \x1b[0m", "");
                bgfx.dbg_text_printf(80, 2, 0x0f, "\x1b[;8m    \x1b[;9m    \x1b[;10m    \x1b[;11m    \x1b[;12m    \x1b[;13m    \x1b[;14m    \x1b[;15m    \x1b[0m", "");

                unsafe
                {
                    bgfx.Stats* stats = bgfx.get_stats();

                    bgfx.dbg_text_printf(0, 2, 0x0f, $"Backbuffer {stats->width}W x {stats->height}H in pixels, debug text {stats->textWidth}W x {stats->textHeight}H in characters.", "");
                }

                bgfx.set_debug((uint)(bgfx.DebugFlags.Text));

                bgfx.frame(false);
            }

            bgfx.shutdown();

            Glfw.Terminate();
        }

    }
}
