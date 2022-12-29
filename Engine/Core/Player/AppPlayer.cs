using Bgfx;
using GLFW;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("StapleEditor")]

namespace Staple
{
    internal class AppPlayer
    {
        public readonly AppSettings appSettings;

        private PlayerSettings playerSettings;

        public static int ScreenWidth { get; internal set; }

        public static int ScreenHeight { get; internal set; }

        public static bgfx.RendererType ActiveRendererType { get; internal set; }

        public static AppPlayer active;

        internal Assembly playerAssembly;

        public AppPlayer(AppSettings settings, string[] args)
        {
            appSettings = settings;
            active = this;

            var baseDirectory = AppContext.BaseDirectory;

#if _DEBUG
            baseDirectory = Environment.CurrentDirectory;
#endif

            ResourceManager.instance.resourcePaths.Add(Path.Combine(baseDirectory, "Data"));

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-datadir")
                {
                    if(i + 1 < args.Length)
                    {
                        ResourceManager.instance.resourcePaths.Clear();
                        ResourceManager.instance.resourcePaths.Add(args[i + 1]);
                    }
                }
            }
        }

        public static bgfx.ResetFlags ResetFlags(PlayerSettings.VideoFlags videoFlags)
        {
            var resetFlags = bgfx.ResetFlags.SrgbBackbuffer;

            if (videoFlags.HasFlag(PlayerSettings.VideoFlags.Vsync))
            {
                resetFlags |= bgfx.ResetFlags.Vsync;
            }

            if (videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX2))
            {
                resetFlags |= bgfx.ResetFlags.MsaaX2;
            }

            if (videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX4))
            {
                resetFlags |= bgfx.ResetFlags.MsaaX4;
            }

            if (videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX8))
            {
                resetFlags |= bgfx.ResetFlags.MsaaX8;
            }

            if (videoFlags.HasFlag(PlayerSettings.VideoFlags.MsaaX16))
            {
                resetFlags |= bgfx.ResetFlags.MsaaX16;
            }

            if (videoFlags.HasFlag(PlayerSettings.VideoFlags.HDR10))
            {
                resetFlags |= bgfx.ResetFlags.Hdr10;
            }

            if (videoFlags.HasFlag(PlayerSettings.VideoFlags.HiDpi))
            {
                resetFlags |= bgfx.ResetFlags.Hidpi;
            }

            return resetFlags;
        }

        public void ResetRendering(bool hasFocus)
        {
            var flags = ResetFlags(playerSettings.videoFlags);

            if(hasFocus == false && appSettings.runInBackground == false)
            {
                flags |= bgfx.ResetFlags.Suspend;
            }

            AppEventQueue.instance.Add(AppEvent.ResetFlags(flags));
        }

        public void Run()
        {
            var baseDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace("Roaming", "LocalLow"), appSettings.appName);

            try
            {
                Directory.CreateDirectory(baseDirectory);
            }
            catch(System.Exception)
            {
            }

            var path = Path.Combine(baseDirectory, "Player.log");

            Log.SetLog(new FSLog(path));

            try
            {
                playerAssembly = Assembly.LoadFrom("Data/Game.dll");
            }
            catch(System.Exception e)
            {
                Log.Error($"Error: Failed to load player assembly: {e}");

                return;
            }
            finally
            {
                Log.Info("Loaded player assembly");
            }

            playerSettings = new PlayerSettings()
            {
                screenWidth = 1024,
                screenHeight = 768,
            };

            var renderWindow = RenderWindow.Create(playerSettings.screenWidth, playerSettings.screenHeight, false, playerSettings.windowMode,
                appSettings, playerSettings.monitorIndex, ResetFlags(playerSettings.videoFlags));

            renderWindow.OnInit = () =>
            {
                Time.fixedDeltaTime = 1000.0f / appSettings.fixedTimeFrameRate / 1000.0f;

                bool hasFocus = renderWindow.window.IsFocused;

                if (appSettings.runInBackground == false && hasFocus == false)
                {
                    ResetRendering(hasFocus);
                }

                Scene.sceneList = ResourceManager.instance.LoadSceneList();

                if (Scene.sceneList == null || Scene.sceneList.Count == 0)
                {
                    Log.Error($"Failed to load scene list");

                    bgfx.shutdown();
                    Glfw.Terminate();

                    return;
                }

                Log.Info("Loaded scene list");

                Scene.current = ResourceManager.instance.LoadScene(Scene.sceneList[0]);

                if (Scene.current == null)
                {
                    Log.Error($"Failed to load main scene");

                    bgfx.shutdown();
                    Glfw.Terminate();

                    return;
                }

                Log.Info("Loaded first scene");

                var renderSystem = new RenderSystem();

                SubsystemManager.instance.RegisterSubsystem(renderSystem, RenderSystem.Priority);
                SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.instance, EntitySystemManager.Priority);

                if (playerAssembly != null)
                {
                    var types = playerAssembly.GetTypes()
                        .Where(x => typeof(IEntitySystem).IsAssignableFrom(x));

                    foreach (var type in types)
                    {
                        try
                        {
                            var instance = (IEntitySystem)Activator.CreateInstance(type);

                            if (instance != null)
                            {
                                EntitySystemManager.instance.RegisterSystem(instance);
                            }
                        }
                        catch (System.Exception e)
                        {
                            Log.Warning($"Player: Failed to load entity system {type.FullName}: {e}");
                        }
                    }
                }

                Log.Info("Finished initializing");
            };

            renderWindow.OnUpdate = () =>
            {
                SubsystemManager.instance.Update(SubsystemType.Update);
            };

            renderWindow.OnFixedUpdate = () =>
            {
                SubsystemManager.instance.Update(SubsystemType.FixedUpdate);
            };

            renderWindow.OnRender = () =>
            {
                SubsystemManager.instance.Update(SubsystemType.Render);
            };

            renderWindow.OnScreenSizeChange = (focus) =>
            {
                ScreenWidth = playerSettings.screenWidth = renderWindow.screenWidth;
                ScreenHeight = playerSettings.screenHeight = renderWindow.screenHeight;

                ResetRendering(focus);
            };

            renderWindow.OnCleanup = () =>
            {
                Log.Info("Terminating");

                Scene.current?.Cleanup();

                SubsystemManager.instance.Destroy();

                ResourceManager.instance.Destroy();

                Log.Info("Done");
            };

            renderWindow.Run();
        }
    }
}
