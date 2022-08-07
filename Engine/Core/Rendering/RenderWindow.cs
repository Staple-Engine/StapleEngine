using Bgfx;
using GLFW;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("StapleEditor")]

namespace Staple
{
    internal class RenderWindow
    {
        //In case we have more than one window in the future
        internal static int glfwReferences = 0;
        internal static int bgfxReferences = 0;

        public int screenWidth = 0;
        public int screenHeight = 0;
        public bool runInBackground = false;
        public bool hasFocus = true;
        public NativeWindow window;
        public bgfx.RendererType rendererType;
        public Action OnUpdate;
        public Action<bool> OnScreenSizeChange;

        public void Run()
        {
            OnScreenSizeChange?.Invoke(window.IsFocused);

            double last = Glfw.Time;

            while (!Glfw.WindowShouldClose(window) && window.IsClosed == false)
            {
                Glfw.PollEvents();

                if (runInBackground == true || window.IsFocused == true)
                {
                    double current = Glfw.Time;

                    Time.UpdateClock(current, last);

                    last = current;
                }

                Glfw.GetFramebufferSize(window, out var currentW, out var currentH);

                if (currentW != screenWidth || currentH != screenHeight)
                {
                    screenWidth = currentW;
                    screenHeight = currentH;

                    try
                    {
                        OnScreenSizeChange?.Invoke(hasFocus);
                    }
                    catch (System.Exception)
                    {
                    }
                }

                if (runInBackground == false && window.IsFocused != hasFocus)
                {
                    hasFocus = window.IsFocused;

                    try
                    {
                        OnScreenSizeChange?.Invoke(hasFocus);
                    }
                    catch (System.Exception)
                    {
                    }

                    if (hasFocus == false)
                    {
                        continue;
                    }
                }

                try
                {
                    OnUpdate?.Invoke();
                }
                catch(System.Exception)
                {
                }

                bgfx.frame(false);
            }
        }

        public void Cleanup()
        {
            if(bgfxReferences > 0)
            {
                bgfxReferences--;

                if(bgfxReferences == 0)
                {
                    bgfx.shutdown();
                }
            }

            if(glfwReferences > 0)
            {
                glfwReferences--;

                if(glfwReferences == 0)
                {
                    Glfw.Terminate();
                }
            }
        }

        public static RenderWindow Create(int width, int height, bool resizable, PlayerSettings.WindowMode windowMode,
            AppSettings settings, int monitorIndex, bgfx.ResetFlags resetFlags, bool runInBackground)
        {
            if (glfwReferences > 0)
            {
                return null;
            }

            var renderWindow = new RenderWindow()
            {
                runInBackground = runInBackground,
            };

            glfwReferences++;

            Glfw.Init();

            Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
            Glfw.WindowHint(Hint.Resizable, resizable);

            var monitor = Glfw.Monitors.Skip(monitorIndex).FirstOrDefault();

            if (monitor == null)
            {
                monitor = Glfw.PrimaryMonitor;
            }

            switch (windowMode)
            {
                case PlayerSettings.WindowMode.Windowed:

                    renderWindow.window = new NativeWindow(width, height, settings.appName);

                    break;

                case PlayerSettings.WindowMode.Fullscreen:

                    renderWindow.window = new NativeWindow(width, height, settings.appName, monitor, Window.None);

                    break;

                case PlayerSettings.WindowMode.Borderless:

                    Glfw.WindowHint(Hint.Floating, true);
                    Glfw.WindowHint(Hint.Decorated, false);

                    var videoMode = Glfw.GetVideoMode(monitor);

                    renderWindow.window = new NativeWindow(videoMode.Width, videoMode.Height, settings.appName);

                    break;
            }

            if (renderWindow.window == null)
            {
                glfwReferences--;

                Glfw.Terminate();

                return null;
            }

            bgfxReferences++;

            bgfx.render_frame(0);

            var init = new bgfx.Init();
            var rendererType = RendererType.OpenGL;

            unsafe
            {
                bgfx.init_ctor(&init);

                init.platformData.ndt = null;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    init.platformData.nwh = Native.GetWin32Window(renderWindow.window).ToPointer();

                    if (settings.renderers.TryGetValue(AppPlatform.Windows, out var type))
                    {
                        rendererType = type;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    var display = Native.GetX11Display();
                    var windowHandle = Native.GetX11Window(renderWindow.window);

                    if (display == IntPtr.Zero || renderWindow.window == IntPtr.Zero)
                    {
                        display = Native.GetWaylandDisplay();
                        windowHandle = Native.GetWaylandWindow(renderWindow.window);
                    }

                    init.platformData.ndt = display.ToPointer();
                    init.platformData.nwh = windowHandle.ToPointer();

                    if (settings.renderers.TryGetValue(AppPlatform.Linux, out var type))
                    {
                        rendererType = type;
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    init.platformData.ndt = (void*)Native.GetCocoaMonitor(renderWindow.window.Monitor);
                    init.platformData.nwh = Native.GetCocoaWindow(renderWindow.window).ToPointer();

                    if (settings.renderers.TryGetValue(AppPlatform.MacOSX, out var type))
                    {
                        rendererType = type;
                    }
                }
            }

            Glfw.GetFramebufferSize(renderWindow.window, out renderWindow.screenWidth, out renderWindow.screenHeight);

            renderWindow.rendererType = bgfx.RendererType.Count;

            switch (rendererType)
            {
                case RendererType.Direct3D11:

                    renderWindow.rendererType = bgfx.RendererType.Direct3D11;

                    break;

                case RendererType.Direct3D12:

                    renderWindow.rendererType = bgfx.RendererType.Direct3D12;

                    break;

                case RendererType.OpenGL:

                    renderWindow.rendererType = bgfx.RendererType.OpenGL;

                    break;

                case RendererType.OpenGLES:

                    renderWindow.rendererType = bgfx.RendererType.OpenGLES;

                    break;

                case RendererType.Metal:

                    renderWindow.rendererType = bgfx.RendererType.Metal;

                    break;

                case RendererType.Vulkan:

                    renderWindow.rendererType = bgfx.RendererType.Vulkan;

                    break;
            }

            init.type = renderWindow.rendererType;
            init.resolution.width = (uint)renderWindow.screenWidth;
            init.resolution.height = (uint)renderWindow.screenHeight;
            init.resolution.reset = (uint)resetFlags;

            unsafe
            {
                if (!bgfx.init(&init))
                {
                    bgfxReferences--;

                    Glfw.Terminate();

                    glfwReferences--;

                    return null;
                }
            }

            Input.window = renderWindow.window;

            Glfw.SetKeyCallback(renderWindow.window, (_, key, scancode, action, mods) =>
            {
                Input.KeyCallback(key, scancode, action, mods);
            });

            Glfw.SetCharCallback(renderWindow.window, (_, codepoint) =>
            {
                Input.CharCallback(codepoint);
            });

            Glfw.SetCursorPositionCallback(renderWindow.window, (_, xpos, ypos) =>
            {
                Input.CursorPosCallback((float)xpos, (float)ypos);
            });

            Glfw.SetMouseButtonCallback(renderWindow.window, (_, button, state, modifiers) =>
            {
                Input.MouseButtonCallback(button, state, modifiers);
            });

            Glfw.SetScrollCallback(renderWindow.window, (_, xOffset, yOffset) =>
            {
                Input.MouseScrollCallback((float)xOffset, (float)yOffset);
            });

            if (Glfw.RawMouseMotionSupported())
            {
                Glfw.SetInputMode(renderWindow.window, InputMode.RawMouseMotion, (int)GLFW.Constants.True);
            }

            return renderWindow;
        }
    }
}
