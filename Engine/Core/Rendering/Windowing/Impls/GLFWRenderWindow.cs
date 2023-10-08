#if !ANDROID
using GLFW;
using System;
using System.Linq;

namespace Staple.Internal
{
    internal class GLFWRenderWindow : IRenderWindow
    {
        public NativeWindow window;

        private bool movedWindow = false;
        private DateTime movedWindowTimer;
        private Vector2Int previousWindowPosition;

        public bool ContextLost { get; set; } = false;

        public bool IsFocused => window.IsFocused;

        public bool ShouldClose => Glfw.WindowShouldClose(window) || window.IsClosed;

        public bool Unavailable => false;

        public bool Maximized => window?.Maximized ?? false;

        public Vector2Int Position
        {
            get => new(window.Position.X, window.Position.Y);

            set => window.Position = new System.Drawing.Point(value.X, value.Y);
        }

        public int MonitorIndex
        {
            get
            {
                if(window == null || window.Monitor == Monitor.None)
                {
                    return 0;
                }

                var monitors = Glfw.Monitors;

                for(var i = 0; i < monitors.Length; i++)
                {
                    if (monitors[i] == window.Monitor)
                    {
                        return i;
                    }
                }

                return 0;
            }
        }

        public bool Create(ref int width, ref int height, string title, bool resizable, WindowMode windowMode, Vector2Int? position,
            bool maximized, int monitorIndex)
        {
            Glfw.WindowHint(Hint.ClientApi, ClientApi.None);
            Glfw.WindowHint(Hint.Resizable, resizable);

            var monitor = Glfw.Monitors.Skip(monitorIndex).FirstOrDefault();

            if (monitor == null)
            {
                monitor = Glfw.PrimaryMonitor;
            }

            switch (windowMode)
            {
                case WindowMode.Windowed:

                    window = new NativeWindow(width, height, title)
                    {
                        Maximized = maximized,
                    };

                    if(position.HasValue)
                    {
                        window.Position = new System.Drawing.Point(position.Value.X, position.Value.Y);
                    }

                    break;

                case WindowMode.Fullscreen:

                    window = new NativeWindow(width, height, title, monitor, Window.None);

                    break;

                case WindowMode.Borderless:

                    Glfw.WindowHint(Hint.Floating, true);
                    Glfw.WindowHint(Hint.Decorated, false);

                    var videoMode = Glfw.GetVideoMode(monitor);

                    width = videoMode.Width;
                    height = videoMode.Height;

                    window = new NativeWindow(width, height, title);

                    break;
            }

            if(window == null)
            {
                return false;
            }

            Glfw.SetKeyCallback(window, (_, key, scancode, action, mods) =>
            {
                AppEventQueue.instance.Add(AppEvent.Key((KeyCode)key, scancode, (InputState)action, (ModifierKeys)mods));
            });

            Glfw.SetCharCallback(window, (_, codepoint) =>
            {
                AppEventQueue.instance.Add(AppEvent.Text(codepoint));
            });

            Glfw.SetCursorPositionCallback(window, (_, xpos, ypos) =>
            {
                Input.CursorPosCallback((float)xpos, (float)ypos);
            });

            Glfw.SetMouseButtonCallback(window, (_, button, state, modifiers) =>
            {
                AppEventQueue.instance.Add(AppEvent.Mouse((MouseButton)button, (InputState)state, (ModifierKeys)modifiers));
            });

            Glfw.SetScrollCallback(window, (_, xOffset, yOffset) =>
            {
                Input.MouseScrollCallback((float)xOffset, (float)yOffset);
            });

            Glfw.SetWindowMaximizeCallback(window, (_, maximized) =>
            {
                AppEventQueue.instance.Add(AppEvent.Maximize(maximized));
            });

            //TODO: Decide whether to keep this.
            /*
            if (Glfw.RawMouseMotionSupported())
            {
                Glfw.SetInputMode(window, InputMode.RawMouseMotion, (int)GLFW.Constants.True);
            }
            */

            return true;
        }

        public void Destroy()
        {
            window.Close();
        }

        public void GetWindowSize(out int width, out int height)
        {
            Glfw.GetFramebufferSize(window, out width, out height);
        }

        public nint MonitorPointer(AppPlatform platform)
        {
            switch (platform)
            {
                case AppPlatform.Windows:

                    return nint.Zero;

                case AppPlatform.Linux:

                    {
                        var display = Native.GetX11Display();

                        if (display == nint.Zero || window == nint.Zero)
                        {
                            display = Native.GetWaylandDisplay();
                        }

                        return display;
                    }

                case AppPlatform.MacOSX:

                    return (nint)Native.GetCocoaMonitor(window.Monitor);

                default:

                    return nint.Zero;
            }
        }

        public void PollEvents()
        {
            Glfw.PollEvents();

            var windowPosition = new Vector2Int(window.Position.X, window.Position.Y);

            if(previousWindowPosition != windowPosition)
            {
                previousWindowPosition = windowPosition;

                movedWindow = true;
                movedWindowTimer = DateTime.Now;
            }

            if (movedWindow && (DateTime.Now - movedWindowTimer).TotalSeconds >= 1.0f)
            {
                movedWindow = false;

                AppEventQueue.instance.Add(AppEvent.MoveWindow(windowPosition));
            }
        }

        public void Init()
        {
            Glfw.Init();
        }

        public void Terminate()
        {
            Glfw.Terminate();
        }

        public nint WindowPointer(AppPlatform platform)
        {
            switch (platform)
            {
                case AppPlatform.Windows:

                    return Native.GetWin32Window(window);

                case AppPlatform.Linux:

                    {
                        var display = Native.GetX11Display();
                        var windowHandle = Native.GetX11Window(window);

                        if (display == nint.Zero || window == nint.Zero)
                        {
                            windowHandle = Native.GetWaylandWindow(window);
                        }

                        return windowHandle;
                    }

                case AppPlatform.MacOSX:

                    return Native.GetCocoaWindow(window);

                default:

                    return nint.Zero;
            }
        }
        public void LockCursor()
        {
            Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Disabled);
        }

        public void UnlockCursor()
        {
            Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Normal);
        }

        public void HideCursor()
        {
            Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Hidden);
        }

        public void ShowCursor()
        {
            Glfw.SetInputMode(window, InputMode.Cursor, (int)CursorMode.Normal);
        }
    }
}
#endif