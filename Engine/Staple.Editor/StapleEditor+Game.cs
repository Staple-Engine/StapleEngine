using Staple.Editor.Templates;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;

namespace Staple.Editor;

internal partial class StapleEditor
{
    /// <summary>
    /// Loads the game
    /// </summary>
    public void LoadGame()
    {
        if (gameLoadDisabled)
        {
            return;
        }

        var projectDirectory = Path.Combine(BasePath, "Cache", "Assembly", "Game");
        var outPath = Path.Combine(projectDirectory, "bin");

        var assemblyPath = Path.Combine(outPath, "Game.dll");

        try
        {
            if(File.Exists(assemblyPath))
            {
                var csprojs = Directory.GetFiles(projectDirectory, "*.csproj", SearchOption.AllDirectories)
                    .Select(x => Path.GetFileNameWithoutExtension(x))
                    .Where(x => x != "Game")
                    .ToList();

                var pluginAssets = Directory.GetFiles(Path.Combine(BasePath, "Assets"), "*.dll", SearchOption.AllDirectories).ToList();

                var packages = Directory.GetDirectories(Path.Combine(BasePath, "Cache", "Packages"));

                foreach(var p in packages)
                {
                    pluginAssets.AddRange(Directory.GetFiles(p, "*.dll", SearchOption.AllDirectories));
                }

                foreach(var asset in pluginAssets)
                {
                    try
                    {
                        AssemblyName.GetAssemblyName(asset);

                        csprojs.Add(Path.GetFileNameWithoutExtension(asset));
                    }
                    catch (Exception)
                    {
                    }
                }

                gameAssemblyLoadContext = new(AppContext.BaseDirectory, () =>
                {
                    return ([outPath], csprojs.ToArray());
                });

                using var stream = new MemoryStream(File.ReadAllBytes(assemblyPath));

                var assembly = gameAssemblyLoadContext.LoadFromStream(stream);

                if(assembly != null)
                {
                    gameAssembly = new(assembly);
                }
            }
        }
        catch(Exception)
        {
        }

        ReloadTypeCache();

        Editor.UpdateEditorTypes();
        GizmoEditor.UpdateEditorTypes();
        GeneratorAssetManager.UpdateGeneratorAssets();

        registeredComponents = registeredComponents.OrderBy(x => x.Name).ToList();

        var scenePath = Path.Combine(BasePath, "Cache", $"LastScene.{AssetSerialization.SceneExtension}");

        try
        {
            if(File.Exists(scenePath))
            {
                var scene = ResourceManager.instance.LoadRawSceneFromPath(scenePath);

                if(scene != null)
                {
                    Scene.SetActiveScene(scene);
                }
            }
        }
        catch(Exception)
        {
        }
        finally
        {
            File.Delete(scenePath);
        }
    }

    /// <summary>
    /// Attempts to unload the game.
    /// Due to .NET core not allowing us to force everything from the game out, we have to assume that the contents will be removed eventually.
    /// Doesn't help that debug runtime keeps objects alive longer than they should!
    /// </summary>
    public void UnloadGame()
    {
        if(gameLoadDisabled)
        {
            return;
        }

        if(gameAssemblyLoadContext != null)
        {
            WeakReference<StapleAssemblyLoadContext> game = new(gameAssemblyLoadContext);

            RecordScene();

            if (gameAssembly?.TryGetTarget(out var assembly) ?? false)
            {
                RenderSystem.Instance.RemoveAllSubsystems(assembly);

                Input.ClearAssemblyActions(assembly);

                World.Current?.UnloadComponentsFromAssembly(assembly);

                EntitySystemManager.Instance.UnloadSystemsFromAssembly(assembly);

                for (var i = editorWindows.Count - 1; i >= 0; i--)
                {
                    if (editorWindows[i].GetType().Assembly == assembly)
                    {
                        editorWindows.RemoveAt(i);
                    }
                }

                SetSelectedEntity(default);
            }

            Scene.SetActiveScene(null);

            gameAssembly = null;

            ReloadTypeCache();

            Editor.UpdateEditorTypes();
            GizmoEditor.UpdateEditorTypes();
            GeneratorAssetManager.UpdateGeneratorAssets();

            gameAssemblyLoadContext.Unload();

            gameAssemblyLoadContext = null;

            var time = DateTime.UtcNow;

            while((DateTime.UtcNow - time).TotalSeconds < 1)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }

    /// <summary>
    /// Reloads the type cache and cleans up editor data
    /// </summary>
    public void ReloadTypeCache()
    {
        lock(backgroundLock)
        {
            foreach (var editor in cachedEditors)
            {
                editor.Value?.Destroy();
            }

            TypeCache.Clear();
            registeredAssetTypes.Clear();
            registeredComponents.Clear();
            registeredEntityTemplates.Clear();
            menuItems.Clear();
            cachedEditors.Clear();
            cachedGizmoEditors.Clear();
            ResourceManager.instance.cachedAssets.Clear();

            StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

            EntitySystemManager.Instance.Shutdown();

            void RegisterTypes(Type[] types)
            {
                foreach (var v in types)
                {
                    if (v.IsInterface)
                    {
                        continue;
                    }

                    if (typeof(IStapleAsset).IsAssignableFrom(v))
                    {
                        registeredAssetTypes.AddOrSetKey(v.FullName, v);
                    }
                    else if (typeof(IComponent).IsAssignableFrom(v) &&
                        v.GetCustomAttribute<AbstractComponentAttribute>() == null)
                    {
                        registeredComponents.Add(v);
                    }
                    else if (typeof(IEntityTemplate).IsAssignableFrom(v))
                    {
                        try
                        {
                            var instance = (IEntityTemplate)Activator.CreateInstance(v);

                            registeredEntityTemplates.Add(instance);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else if (typeof(IRenderSystem).IsAssignableFrom(v) &&
                        v != typeof(IRenderSystem))
                    {
                        try
                        {
                            var instance = (IRenderSystem)Activator.CreateInstance(v);

                            RenderSystem.Instance.RegisterSystem(instance);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else if((typeof(IEntitySystemLifecycle).IsAssignableFrom(v) ||
                        typeof(IEntitySystemUpdate).IsAssignableFrom(v) ||
                        typeof(IEntitySystemFixedUpdate).IsAssignableFrom(v)) &&
                        !v.IsInterface)
                    {
                        try
                        {
                            var instance = Activator.CreateInstance(v);

                            EntitySystemManager.Instance.RegisterSystem(instance);
                        }
                        catch (Exception)
                        {
                        }
                    }
                    else if (v.IsSubclassOf(typeof(EditorWindow)))
                    {
                        foreach (var method in v.GetMethods(BindingFlags.Static | BindingFlags.Public))
                        {
                            var menu = method.GetCustomAttribute<MenuItemAttribute>();

                            if (menu == null)
                            {
                                continue;
                            }

                            var m = method;

                            AddMenuItem(menu.path, () =>
                            {
                                try
                                {
                                    m.Invoke(null, null);
                                }
                                catch (Exception)
                                {
                                }
                            });
                        }
                    }
                }
            }

            if (gameAssembly?.TryGetTarget(out var assembly) ?? false)
            {
                void HandleRegistration()
                {
                    try
                    {
                        var registrationType = assembly.GetType(typeof(GameRegistration).FullName);

                        if (registrationType == null)
                        {
                            return;
                        }

                        var instance = ObjectCreation.CreateObject(registrationType);

                        if (instance == null)
                        {
                            return;
                        }

                        var method = instance.GetType().GetMethod("RegisterAll");

                        method?.Invoke(instance, null);
                    }
                    catch(Exception e)
                    {
                        Log.Error($"Failed to initialize game: {e}\nForcing rebuild.");

                        needsGameRecompile = forceGameRecompile = true;
                    }
                }

                HandleRegistration();
            }

            RegisterTypes(TypeCache.AllTypes());

            registeredComponents = registeredComponents.OrderBy(x => x.Name).ToList();
            registeredEntityTemplates = registeredEntityTemplates.OrderBy(x => x.Name).ToList();
        }
    }

    public void ExecuteGameViewHandler(Action handler)
    {
        if(gameRenderTarget == null)
        {
            return;
        }

        var inputOffset = gameWindowPosition;

        Screen.Width = gameRenderTarget.width;
        Screen.Height = gameRenderTarget.height;

        var currentMousePosition = Input.MousePosition;

        var newMousePosition = Input.MousePosition - inputOffset;

        if (newMousePosition.X < 0)
        {
            newMousePosition.X = 0;
        }

        if (newMousePosition.Y < 0)
        {
            newMousePosition.Y = 0;
        }

        Input.MousePosition = newMousePosition;

        var previous = RenderTarget.Current;

        RenderTarget.Current = gameRenderTarget;

        handler();

        RenderTarget.Current = previous;

        Input.MousePosition = currentMousePosition;

        Screen.Width = window.width;
        Screen.Height = window.height;
    }
}
