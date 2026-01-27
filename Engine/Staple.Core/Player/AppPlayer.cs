using Staple.Internal;
using System;
using System.IO;
using System.Linq;

namespace Staple;

internal class AppPlayer
{
    internal PlayerSettings playerSettings;

    public static AppPlayer instance;

    internal RenderWindow renderWindow;

    internal const bool printTypeCacheTypes = false;

    private bool initialized = false;

    private readonly bool skipFlow;

    public AppPlayer(string[] args, bool shouldConsoleLog, bool skipFlow)
    {
        instance = this;

#if _DEBUG
        this.skipFlow = skipFlow;
#endif

        Storage.Update(AppSettings.Current.appName, AppSettings.Current.companyName);

        var path = Path.Combine(Storage.PersistentDataPath, "Player.log");

        Log.SetLog(new FSLog(path));

        if(shouldConsoleLog)
        {
            Log.Instance.onLog += (type, message) =>
            {
                Platform.platformProvider.ConsoleLog($"[{type}] {message}");
            };
        }

        if(printTypeCacheTypes)
        {
            Platform.platformProvider.ConsoleLog($"TypeCache Types:");

            foreach (var type in TypeCache.AllTypes())
            {
                Platform.platformProvider.ConsoleLog($"{type.FullName}");
            }

            Platform.platformProvider.ConsoleLog($"Done");
        }
    }

    public void Create()
    {
        playerSettings = PlayerSettings.Load(AppSettings.Current);

        if(playerSettings.screenWidth <= 0 || playerSettings.screenHeight <= 0 || playerSettings.windowPosition.X < -1000 || playerSettings.windowPosition.Y < -1000)
        {
            playerSettings.screenWidth = AppSettings.Current.defaultWindowWidth;
            playerSettings.screenHeight = AppSettings.Current.defaultWindowHeight;

            playerSettings.windowPosition = Vector2Int.Zero;
        }

        PlayerSettings.Save(playerSettings);

        renderWindow = RenderWindow.Create(playerSettings.screenWidth, playerSettings.screenHeight, false, playerSettings.windowMode,
            playerSettings.windowPosition != Vector2Int.Zero ? playerSettings.windowPosition : null,
            playerSettings.maximized, playerSettings.monitorIndex, RenderSystem.RenderFlags(playerSettings.videoFlags));

        if(renderWindow == null)
        {
            return;
        }

        renderWindow.OnInit = () =>
        {
            AssetDatabase.Reload(null, () =>
            {
                try
                {
                    var data = ResourceManager.instance.LoadFile("StapleAppIcon.png");

                    if (data != null)
                    {
                        var rawInfo = Texture.LoadStandard(data, StandardTextureColorComponents.RGBA);

                        if (rawInfo != null)
                        {
                            var width = 256;
                            var height = (int)(rawInfo.height / (float)rawInfo.width * 256);

                            if (rawInfo.Resize(width, height))
                            {
                                renderWindow.window.SetIcon(rawInfo);
                            }
                        }
                    }
                }
                catch (Exception)
                {
                }

                Time.fixedDeltaTime = 1 / (float)AppSettings.Current.fixedTimeFrameRate;

                if(!skipFlow)
                {
                    Scene.sceneList = ResourceManager.instance.LoadSceneList();

                    if (Scene.sceneList == null || Scene.sceneList.Count == 0)
                    {
                        Log.Error($"Failed to load scene list");

                        renderWindow.shouldStop = true;

                        throw new Exception("Failed to load scene list");
                    }

                    Log.Info("Loaded scene list");
                }

                if (Physics3D.ImplType != null && !Physics3D.ImplType.IsAssignableTo(typeof(IPhysics3D)))
                {
                    Log.Error($"Failed to initialize physics: {Physics3D.ImplType.FullName} doesn't implement IPhysics3D");

                    renderWindow.shouldStop = true;

                    throw new Exception("Failed to initialize physics");
                }

                var physicsInstance = Physics3D.ImplType != null ? ObjectCreation.CreateObject<IPhysics3D>(Physics3D.ImplType) : null;

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

                SubsystemManager.instance.RegisterSubsystem(RenderSystem.Instance, RenderSystem.Priority);
                SubsystemManager.instance.RegisterSubsystem(EntitySystemManager.Instance, EntitySystemManager.Priority);
                SubsystemManager.instance.RegisterSubsystem(AudioSystem.Instance, AudioSystem.Priority);

                if (Physics3D.Instance != null)
                {
                    SubsystemManager.instance.RegisterSubsystem(Physics3D.Instance, Physics3D.Priority);
                }

                void HandleTypes<T>(string caption, Func<Type, bool> check, Action<T> callback)
                {
                    var types = TypeCache.AllTypes()
                        .Where(x => check(x))
                        .ToArray();

                    Log.Info($"Loading {types.Length} {caption}s");

                    foreach (var type in types)
                    {
                        try
                        {
                            var instance = (T)Activator.CreateInstance(type);

                            if (instance != null)
                            {
                                callback(instance);

                                Log.Info($"Created {caption} {type.FullName}");
                            }
                            else
                            {
                                Log.Info($"Failed to create {caption} {type.FullName}");
                            }
                        }
                        catch (Exception e)
                        {
                            Log.Warning($"Player: Failed to load {caption} {type.FullName}: {e}");
                        }
                    }
                }

                HandleTypes<IRenderSystem>("render system",
                    (x => typeof(IRenderSystem).IsAssignableFrom(x) && x != typeof(IRenderSystem)),
                    (instance => RenderSystem.Instance.RegisterSystem(instance)));

                HandleTypes<object>("entity system",
                    (x => (typeof(IEntitySystemUpdate).IsAssignableFrom(x) && x != typeof(IEntitySystemUpdate)) ||
                        (typeof(IEntitySystemFixedUpdate).IsAssignableFrom(x) && x != typeof(IEntitySystemFixedUpdate))),
                    (instance => EntitySystemManager.Instance.RegisterSystem(instance)));

                if (!skipFlow)
                {
                    var scene = ResourceManager.instance.LoadScene(Scene.sceneList[0]);

                    if (scene == null)
                    {
                        Log.Error($"Failed to load main scene");

                        renderWindow.shouldStop = true;

                        throw new Exception("Failed to load main scene");
                    }

                    Scene.SetActiveScene(scene);

                    Log.Info("Loaded first scene");
                }

                Log.Info("Finished initializing");

                initialized = true;
            });
        };

        renderWindow.OnFixedUpdate = () =>
        {
            if(!initialized)
            {
                return;
            }

            SubsystemManager.instance.Update(SubsystemType.FixedUpdate);

            EntitySystemManager.Instance.UpdateFixed();
        };

        renderWindow.OnUpdate = () =>
        {
            if (!initialized)
            {
                return;
            }

            if ((AppSettings.Current?.allowFullscreenSwitch ?? true) && Input.GetKey(KeyCode.LeftAlt) && Input.GetKeyDown(KeyCode.Enter))
            {
                Screen.SetResolution(Screen.Width, Screen.Height, Screen.WindowMode == WindowMode.Windowed ? WindowMode.BorderlessFullscreen : WindowMode.Windowed);
            }

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

            PlayerSettings.Save(playerSettings);
        };

        renderWindow.OnCleanup = () =>
        {
            Log.Info("Terminating");

            SubsystemManager.instance.Destroy();

            ResourceManager.instance.Destroy(ResourceManager.DestroyMode.Final);

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
