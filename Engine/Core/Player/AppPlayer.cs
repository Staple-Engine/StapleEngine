using Bgfx;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;

namespace Staple;

internal class AppPlayer
{
    public readonly AppSettings appSettings;

    internal PlayerSettings playerSettings;

    public static AppPlayer instance;

    internal RenderWindow renderWindow;

    internal const bool printTypeCacheTypes = false;

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

        ModuleInitializer.LoadAll();

        if(printTypeCacheTypes)
        {
            Console.WriteLine($"TypeCache Types:");

            foreach (var type in TypeCache.AllTypes())
            {
                Console.WriteLine($"{type.FullName}");
            }

            Console.WriteLine($"Done");
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
            AssetDatabase.Reload();

            try
            {
                var data = ResourceManager.instance.LoadFile("StapleAppIcon.png");

                if(data != null)
                {
                    var rawInfo = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);

                    if(rawInfo != null)
                    {
                        var width = 256;
                        var height = (int)(rawInfo.height / (float)rawInfo.width * 256);

                        if(rawInfo.Resize(width, height))
                        {
                            renderWindow.window.SetIcon(rawInfo);
                        }
                    }
                }
            }
            catch(Exception)
            {
            }

            Time.fixedDeltaTime = 1 / (float)appSettings.fixedTimeFrameRate;
            Physics3D.PhysicsDeltaTime = 1 / (float)appSettings.physicsFrameRate;

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

            if(Physics3D.ImplType != null)
            {
                if(Physics3D.ImplType.IsAssignableTo(typeof(IPhysics3D)) == false)
                {
                    Log.Error($"Failed to initialize physics: {Physics3D.ImplType.FullName} doesn't implement IPhysics3D");

                    renderWindow.shouldStop = true;

                    throw new Exception("Failed to initialize physics");
                }

                var physicsInstance = ObjectCreation.CreateObject<IPhysics3D>(Physics3D.ImplType);

                if (physicsInstance != null)
                {
                    try
                    {
                        Physics3D.Instance = new Physics3D(physicsInstance);
                    }
                    catch (Exception e)
                    {
                        Log.Error(e.ToString());

                        renderWindow.shouldStop = true;

                        throw new Exception("Failed to initialize physics");
                    }
                }
            }

            SubsystemManager.instance.RegisterSubsystem(RenderSystem.Instance, RenderSystem.Priority);
            SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.Instance, EntitySystemManager.Priority);
            SubsystemManager.instance.RegisterSubsystem(AudioSystem.Instance, AudioSystem.Priority);

            if (Physics3D.Instance != null)
            {
                SubsystemManager.instance.RegisterSubsystem(Physics3D.Instance, Physics3D.Priority);
            }

            var types = TypeCache.AllTypes()
                .Where(x => typeof(IRenderSystem).IsAssignableFrom(x) && x != typeof(IRenderSystem))
                .ToArray();

            Log.Info($"Loading {types.Length} render systems");

            foreach (var type in types)
            {
                try
                {
                    var instance = (IRenderSystem)Activator.CreateInstance(type);

                    if (instance != null)
                    {
                        RenderSystem.Instance.RegisterSystem(instance);

                        Log.Info($"Created render system {type.FullName}");
                    }
                    else
                    {
                        Log.Info($"Failed to create render system {type.FullName}");
                    }
                }
                catch (Exception e)
                {
                    Log.Warning($"Player: Failed to load render system {type.FullName}: {e}");
                }
            }

            types = TypeCache.AllTypes()
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
                        EntitySystemManager.Instance.RegisterSystem(instance);

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

            EntitySystemManager.Instance.UpdateFixed();
        };

        renderWindow.OnUpdate = () =>
        {
            SubsystemManager.instance.Update(SubsystemType.Update);
        };

        renderWindow.OnMove = (position) =>
        {
            playerSettings.monitorIndex = renderWindow.MonitorIndex;
            playerSettings.maximized = renderWindow.Maximized;
            playerSettings.windowPosition = position;

            PlayerSettings.Save(playerSettings);
        };

        renderWindow.OnScreenSizeChange = (focus) =>
        {
            Screen.Width = playerSettings.screenWidth = renderWindow.width;
            Screen.Height = playerSettings.screenHeight = renderWindow.height;

            playerSettings.monitorIndex = renderWindow.MonitorIndex;
            playerSettings.maximized = renderWindow.Maximized;

            ResetRendering(focus);

            PlayerSettings.Save(playerSettings);
        };

        renderWindow.OnCleanup = () =>
        {
            Log.Info("Terminating");

            SubsystemManager.instance.Destroy();

            ResourceManager.instance.Destroy(true);

            ModuleInitializer.UnloadAll();

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
