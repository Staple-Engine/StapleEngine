using Bgfx;
using GLFW;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Staple
{
    internal class AppPlayer
    {
        public readonly AppSettings appSettings;

        private const ushort ClearView = 0;

        private PlayerSettings playerSettings;

        public static int ScreenWidth { get; private set; }

        public static int ScreenHeight { get; private set; }

        public static bgfx.RendererType ActiveRendererType { get; private set; }

        public static AppPlayer active;

        public AppPlayer(AppSettings settings, string[] args)
        {
            appSettings = settings;
            active = this;

            var baseDirectory = AppContext.BaseDirectory;

#if _DEBUG
            baseDirectory = Environment.CurrentDirectory;
#endif

            ResourceManager.instance.basePath = Path.Combine(baseDirectory, "Data");

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-datadir")
                {
                    if(i + 1 < args.Length)
                    {
                        ResourceManager.instance.basePath = args[i + 1];
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

            bgfx.reset((uint)ScreenWidth, (uint)ScreenHeight, (uint)flags, bgfx.TextureFormat.RGBA8);
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

            var renderSystem = new RenderSystem();

            SubsystemManager.instance.RegisterSubsystem(renderSystem, RenderSystem.Priority);
            SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.instance, EntitySystemManager.Priority);

            var cameraEntity = new Entity("Camera");

            var camera = cameraEntity.AddComponent<Camera>();

            camera.cameraType = CameraType.Orthographic;
            camera.nearPlane = 0;

            Entity sprite = null;
            Entity child = null;

            var shader = ResourceManager.instance.LoadShader("Shaders/Sprite/sprite.stsh");

            if(shader != null)
            {
                var material = new Material()
                {
                    shader = shader,
                };

                var texture = ResourceManager.instance.LoadTexture("Textures/Sprites/DefaultSprite.png");

                if(texture != null)
                {
                    material.MainTexture = texture;

                    sprite = new Entity("Sprite");

                    sprite.Transform.LocalScale = Vector3.One * 0.5f;
                    sprite.Transform.LocalPosition = new Vector3(ScreenWidth / 2, ScreenHeight / 2, 0);

                    sprite.AddComponent<SpriteRenderer>().material = material;

                    child = new Entity("Child");

                    child.Transform.SetParent(sprite.Transform);

                    child.Transform.LocalScale = Vector3.One * 0.5f;

                    child.AddComponent<SpriteRenderer>().material = material;
                }
                else
                {
                    material?.Destroy();
                    texture?.Destroy();
                }
            }

            camera.clearColor = Color.Black;//new Color(0.25f, 0.5f, 0.0f, 0.0f);

#if _DEBUG
            bgfx.set_debug((uint)bgfx.DebugFlags.Text);
#endif

            DateTimeOffset last = (DateTimeOffset)DateTime.UtcNow;

            while (!Glfw.WindowShouldClose(window) && window.IsClosed == false)
            {
                Glfw.PollEvents();

                if (appSettings.runInBackground == true || window.IsFocused == true)
                {
                    DateTimeOffset current = (DateTimeOffset)DateTime.UtcNow;

                    Time.UpdateClock(current, last);

                    last = current;
                }

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

                SubsystemManager.instance.Update();

                var hasCamera = Scene.current.GetComponents<Camera>().ToArray().Length != 0;

                if(hasCamera == false)
                {
                    bgfx.touch(ClearView);
                    bgfx.dbg_text_clear(0, false);
                    bgfx.dbg_text_printf(40, 20, 1, "No cameras are Rendering", "");
                }

                bgfx.touch(ClearView);
                bgfx.dbg_text_clear(0, false);
                bgfx.dbg_text_printf(0, 0, 1, $"FPS: {Time.FPS}", "");

                if (sprite != null)
                {
                    sprite.Transform.LocalRotation = Quaternion.CreateFromYawPitchRoll(0, 0, Time.time);
                }

                if(child != null)
                {
                    child.Transform.LocalPosition = new Vector3(200 * Math.Cos(Math.Deg2Rad(Time.time * 100)), 200 * Math.Sin(Math.Deg2Rad(Time.time * 100)), 0);
                    child.Transform.LocalRotation = Quaternion.CreateFromYawPitchRoll(0, 0, Time.time);
                }

                bgfx.frame(false);
            }

            Scene.current?.Cleanup();

            SubsystemManager.instance.Destroy();

            ResourceManager.instance.Destroy();

            bgfx.shutdown();

            Glfw.Terminate();
        }

    }
}
