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
    internal class AppPlayer
    {
        public readonly AppSettings appSettings;

        private const ushort ClearView = 0;

        private PlayerSettings playerSettings;

        private RenderSystem renderSystem = new RenderSystem();

        public static int ScreenWidth { get; private set; }

        public static int ScreenHeight { get; private set; }

        public static bgfx.RendererType ActiveRendererType { get; private set; }

        public static AppPlayer active;

        public AppPlayer(AppSettings settings, string[] args)
        {
            appSettings = settings;
            active = this;

            for(var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-datadir")
                {
                    if(i + 1 < args.Length)
                    {
                        ResourceLocator.instance.basePath = args[i + 1];
                    }
                }
            }
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

            bgfx.reset((uint)ScreenWidth, (uint)ScreenHeight, (uint)flags, bgfx.TextureFormat.RGBA32U);
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
            var rendererType = RendererType.OpenGL;

            unsafe
            {
                bgfx.init_ctor(&init);

                init.platformData.ndt = null;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    init.platformData.nwh = Native.GetWin32Window(window).ToPointer();

                    if(appSettings.renderers.TryGetValue(AppPlatform.Windows, out var type))
                    {
                        rendererType = type;
                    }
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

                    if (appSettings.renderers.TryGetValue(AppPlatform.Linux, out var type))
                    {
                        rendererType = type;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    init.platformData.ndt = (void*)Native.GetCocoaMonitor(window.Monitor);
                    init.platformData.nwh = Native.GetCocoaWindow(window).ToPointer();

                    if (appSettings.renderers.TryGetValue(AppPlatform.MacOSX, out var type))
                    {
                        rendererType = type;
                    }
                }
            }

            Glfw.GetFramebufferSize(window, out playerSettings.screenWidth, out playerSettings.screenHeight);

            ScreenWidth = playerSettings.screenWidth;
            ScreenHeight = playerSettings.screenHeight;

            ActiveRendererType = bgfx.RendererType.Count;

            switch(rendererType)
            {
                case RendererType.Direct3D11:

                    ActiveRendererType = bgfx.RendererType.Direct3D11;

                    break;

                case RendererType.Direct3D12:

                    ActiveRendererType = bgfx.RendererType.Direct3D12;

                    break;

                case RendererType.OpenGL:

                    ActiveRendererType = bgfx.RendererType.OpenGL;

                    break;

                case RendererType.OpenGLES:

                    ActiveRendererType = bgfx.RendererType.OpenGLES;

                    break;

                case RendererType.Metal:

                    ActiveRendererType = bgfx.RendererType.Metal;

                    break;

                case RendererType.Vulkan:

                    ActiveRendererType = bgfx.RendererType.Vulkan;

                    break;
            }

            init.type = ActiveRendererType;
            init.resolution.width = (uint)ScreenWidth;
            init.resolution.height = (uint)ScreenHeight;
            init.resolution.reset = (uint)ResetFlags;

            unsafe
            {
                if (!bgfx.init(&init))
                {
                    return;
                }
            }

            bgfx.set_view_clear(ClearView, (ushort)(bgfx.ClearFlags.Color | bgfx.ClearFlags.Depth), 0x334455FF, 0, 0);
            bgfx.set_view_rect_ratio(ClearView, 0, 0, bgfx.BackbufferRatio.Equal);

            bool hasFocus = window.IsFocused;

            if(appSettings.runInBackground == false && hasFocus == false)
            {
                ResetRendering(hasFocus);
            }

            Scene.current = new Scene();

            var cameraEntity = new Entity("Camera");

            var camera = cameraEntity.AddComponent<Camera>();

            camera.cameraType = CameraType.Orthographic;

            var spriteShaderVS = ResourceLocator.instance.LoadFile("Shaders/Sprite/sprite_vs.sc");
            var spriteShaderFS = ResourceLocator.instance.LoadFile("Shaders/Sprite/sprite_fs.sc");

            if(spriteShaderVS != null && spriteShaderFS != null)
            {
                var material = Material.Create(spriteShaderVS, spriteShaderFS);

                if(material != null)
                {
                    var sprite = new Entity("Sprite");

                    sprite.AddComponent<SpriteRenderer>().material = material;
                }
            }

            //camera.clearColor = new Color(0.25f, 0.5f, 0.0f, 0.0f);

#if _DEBUG
            bgfx.set_debug((uint)bgfx.DebugFlags.Text);
#endif

            while (!Glfw.WindowShouldClose(window) && window.IsClosed == false)
            {
                Glfw.PollEvents();

                Glfw.GetFramebufferSize(window, out var currentW, out var currentH);

                if (currentW != ScreenWidth || currentH != ScreenHeight)
                {
                    playerSettings.screenWidth = ScreenWidth = currentW;
                    playerSettings.screenHeight = ScreenHeight = currentH;

                    ResetRendering(hasFocus);
                }

                if(appSettings.runInBackground == false && window.IsFocused != hasFocus)
                {
                    hasFocus = window.IsFocused;

                    ResetRendering(hasFocus);

                    if(hasFocus == false)
                    {
                        continue;
                    }
                }

                if(renderSystem.Perform(Scene.current) == false)
                {
                    bgfx.touch(ClearView);
                    bgfx.dbg_text_clear(0, false);
                    bgfx.dbg_text_printf(40, 20, 1, "No cameras are Rendering", "");
                }

                bgfx.frame(false);
            }

            bgfx.shutdown();

            Glfw.Terminate();
        }

    }
}
