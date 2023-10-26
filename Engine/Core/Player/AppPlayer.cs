using Bgfx;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;

namespace Staple
{
    internal class AppPlayer
    {
        public readonly AppSettings appSettings;

        private PlayerSettings playerSettings;

        public static AppPlayer instance;

        internal RenderWindow renderWindow;

        public AppPlayer(AppSettings settings, string[] args, bool shouldConsoleLog)
        {
            appSettings = settings;
            instance = this;

            Storage.Update(appSettings.appName, appSettings.companyName);

            var path = Path.Combine(Storage.PersistentDataPath, "Player.log");

            Log.SetLog(new FSLog(path));

            if(shouldConsoleLog)
            {
                Log.Instance.onLog += (type, message) =>
                {
                    Console.WriteLine($"[{type}] {message}");
                };
            }
        }

        public void ResetRendering(bool hasFocus)
        {
            var flags = RenderSystem.ResetFlags(playerSettings.videoFlags);

            if(hasFocus == false && appSettings.runInBackground == false)
            {
                flags |= bgfx.ResetFlags.Suspend;
            }

            AppEventQueue.instance.Add(AppEvent.ResetFlags(flags));
        }

        public void Create()
        {
            playerSettings = PlayerSettings.Load(appSettings);

            if(playerSettings.screenWidth <= 0 || playerSettings.screenHeight <= 0 || playerSettings.windowPosition.X < -1000 || playerSettings.windowPosition.Y < -1000)
            {
                playerSettings.screenWidth = appSettings.defaultWindowWidth;
                playerSettings.screenHeight = appSettings.defaultWindowHeight;

                playerSettings.windowPosition = Vector2Int.Zero;
            }

            PlayerSettings.Save(playerSettings);

            renderWindow = RenderWindow.Create(playerSettings.screenWidth, playerSettings.screenHeight, false, playerSettings.windowMode, appSettings,
                playerSettings.windowPosition != Vector2Int.Zero ? playerSettings.windowPosition : null,
                playerSettings.maximized, playerSettings.monitorIndex, RenderSystem.ResetFlags(playerSettings.videoFlags));

            if(renderWindow == null)
            {
                return;
            }

            renderWindow.OnInit = () =>
            {
                Time.fixedDeltaTime = 1 / (float)appSettings.fixedTimeFrameRate;

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

                    throw new Exception("Failed to load scene list");
                }

                Log.Info("Loaded scene list");

                var renderSystem = new RenderSystem();

                try
                {
                    Physics3D.Instance = new Physics3D(new JoltPhysics3D());
                }
                catch(Exception e)
                {
                    Log.Error(e.ToString());

                    renderWindow.shouldStop = true;

                    throw new Exception("Failed to initialize physics");
                }

                SubsystemManager.instance.RegisterSubsystem(renderSystem, RenderSystem.Priority);
                SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.GetEntitySystem(SubsystemType.FixedUpdate), EntitySystemManager.Priority);
                SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.GetEntitySystem(SubsystemType.Update), EntitySystemManager.Priority);
                SubsystemManager.instance.RegisterSubsystem(Physics3D.Instance, Physics3D.Priority);
                SubsystemManager.instance.RegisterSubsystem(AudioSystem.Instance, AudioSystem.Priority);

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
                    catch (Exception e)
                    {
                        Log.Warning($"Player: Failed to load entity system {type.FullName}: {e}");
                    }
                }

                var scene = ResourceManager.instance.LoadScene(Scene.sceneList[0]);

                if (scene == null)
                {
                    Log.Error($"Failed to load main scene");

                    renderWindow.shouldStop = true;

                    throw new Exception("Failed to load main scene");
                }

                Scene.SetActiveScene(scene);

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

            renderWindow.OnMove = (position) =>
            {
                playerSettings.windowPosition = position;

                PlayerSettings.Save(playerSettings);
            };

            renderWindow.OnScreenSizeChange = (focus) =>
            {
                Screen.Width = playerSettings.screenWidth = renderWindow.width;
                Screen.Height = playerSettings.screenHeight = renderWindow.height;

                ResetRendering(focus);

                PlayerSettings.Save(playerSettings);
            };

            renderWindow.OnCleanup = () =>
            {
                Log.Info("Terminating");

                SubsystemManager.instance.Destroy();

                ResourceManager.instance.Destroy(true);

                Log.Info("Done");

                Log.Cleanup();
            };
        }

        public void Run()
        {
            Create();

            if(renderWindow == null)
            {
                return;
            }

            renderWindow.Run();
        }
    }
}
