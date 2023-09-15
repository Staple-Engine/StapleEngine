using Bgfx;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;

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

        public AppPlayer(AppSettings settings, string[] args)
        {
            appSettings = settings;
            active = this;

            var baseDirectory = AppContext.BaseDirectory;

#if _DEBUG
            baseDirectory = Environment.CurrentDirectory;
#endif

            ResourceManager.instance.resourcePaths.Add(Path.Combine(baseDirectory, "Data"));

            Storage.Update(appSettings.appName, appSettings.companyName);

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

        public void LoadPlayerSettings()
        {
            try
            {
                var data = File.ReadAllText(Path.Combine(Storage.PersistentDataPath, "PlayerSettings.json"));

                playerSettings = JsonSerializer.Deserialize(data, PlayerSettingsSerializationContext.Default.PlayerSettings);
            }
            catch(System.Exception e)
            {
                playerSettings = new PlayerSettings()
                {
                    windowMode = appSettings.defaultWindowMode,
                    screenWidth = appSettings.defaultWindowWidth,
                    screenHeight = appSettings.defaultWindowHeight,
                };
            }
        }

        public void SavePlayerSettings()
        {
            try
            {
                var data = JsonSerializer.Serialize(playerSettings, PlayerSettingsSerializationContext.Default.PlayerSettings);

                File.WriteAllText(Path.Combine(Storage.PersistentDataPath, "PlayerSettings.json"), data);
            }
            catch(System.Exception)
            {
            }
        }

        public static bgfx.ResetFlags ResetFlags(VideoFlags videoFlags)
        {
            var resetFlags = bgfx.ResetFlags.SrgbBackbuffer;

            if (videoFlags.HasFlag(VideoFlags.Vsync))
            {
                resetFlags |= bgfx.ResetFlags.Vsync;
            }

            if (videoFlags.HasFlag(VideoFlags.MSAAX2))
            {
                resetFlags |= bgfx.ResetFlags.MsaaX2;
            }
            else if (videoFlags.HasFlag(VideoFlags.MSAAX4))
            {
                resetFlags |= bgfx.ResetFlags.MsaaX4;
            }
            else if (videoFlags.HasFlag(VideoFlags.MSAAX8))
            {
                resetFlags |= bgfx.ResetFlags.MsaaX8;
            }
            else if (videoFlags.HasFlag(VideoFlags.MSAAX16))
            {
                resetFlags |= bgfx.ResetFlags.MsaaX16;
            }

            if (videoFlags.HasFlag(VideoFlags.HDR10))
            {
                resetFlags |= bgfx.ResetFlags.Hdr10;
            }

            if (videoFlags.HasFlag(VideoFlags.HiDPI))
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
            var path = Path.Combine(Storage.PersistentDataPath, "Player.log");

            Log.SetLog(new FSLog(path));

            Log.Instance.onLog += (type, message) =>
            {
                Console.WriteLine($"[{type}] {message}");
            };

            LoadPlayerSettings();
            SavePlayerSettings();

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

                    renderWindow.shouldStop = true;

                    throw new System.Exception("Failed to load scene list");
                }

                Log.Info("Loaded scene list");

                var renderSystem = new RenderSystem();

                try
                {
                    Physics3D.Instance = new Physics3D(new JoltPhysics3D());
                }
                catch(System.Exception e)
                {
                    Log.Error(e.ToString());

                    renderWindow.shouldStop = true;

                    throw new System.Exception("Failed to initialize physics");
                }

                SubsystemManager.instance.RegisterSubsystem(renderSystem, RenderSystem.Priority);
                SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.GetEntitySystem(SubsystemType.FixedUpdate), EntitySystemManager.Priority);
                SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.GetEntitySystem(SubsystemType.Update), EntitySystemManager.Priority);
                SubsystemManager.instance.RegisterSubsystem(Physics3D.Instance, Physics3D.Priority);

                var types = TypeCache.AllTypes()
                    .Where(x => typeof(IEntitySystem).IsAssignableFrom(x) && x != typeof(IEntitySystem))
                    .ToArray();

                Log.Info($"Loading {types.Length} entity systems");

                foreach(var type in types)
                {
                    try
                    {
                        var instance = (IEntitySystem)Activator.CreateInstance(type);

                        if (instance != null)
                        {
                            EntitySystemManager.GetEntitySystem(instance.UpdateType)?.RegisterSystem(instance);

                            Log.Info($"Created entity system {type.FullName}");
                        }
                        else
                        {
                            Log.Info($"Failed to create entity system {type.FullName}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Log.Warning($"Player: Failed to load entity system {type.FullName}: {e}");
                    }
                }

                Scene.current = ResourceManager.instance.LoadScene(Scene.sceneList[0]);

                if (Scene.current == null)
                {
                    Log.Error($"Failed to load main scene");

                    renderWindow.shouldStop = true;

                    throw new System.Exception("Failed to load main scene");
                }

                Log.Info("Loaded first scene");

                Log.Info("Finished initializing");
            };

            renderWindow.OnFixedUpdate = () =>
            {
                SubsystemManager.instance.Update(SubsystemType.FixedUpdate);
            };

            renderWindow.OnUpdate = () =>
            {
                SubsystemManager.instance.Update(SubsystemType.Update);
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

                SubsystemManager.instance.Destroy();

                ResourceManager.instance.Destroy();

                Log.Info("Done");

                Log.Cleanup();
            };

            renderWindow.Run();
        }
    }
}
