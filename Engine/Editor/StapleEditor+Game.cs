using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Staple.Internal;
using System;
using System.IO;
using System.Linq;
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

        var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", "Game");
        var outPath = Path.Combine(projectDirectory, "bin");

        var assemblyPath = Path.Combine(outPath, "Game.dll");

        try
        {
            if(File.Exists(assemblyPath))
            {
                gameAssemblyLoadContext = new(AppContext.BaseDirectory);

                using var stream = new MemoryStream(File.ReadAllBytes(assemblyPath));

                var assembly = gameAssemblyLoadContext.LoadFromStream(stream);

                if(assembly != null)
                {
                    gameAssembly = new(assembly);

                    var types = assembly.GetTypes();

                    foreach(var type in types)
                    {
                        TypeCache.RegisterType(type);
                    }
                }
            }
        }
        catch(Exception)
        {
        }

        ReloadTypeCache();

        Editor.UpdateEditorTypes();
        GizmoEditor.UpdateEditorTypes();

        registeredComponents = registeredComponents.OrderBy(x => x.Name).ToList();

        var scenePath = Path.Combine(basePath, "Cache", "LastScene.stsc");

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

            var scenePath = Path.Combine(basePath, "Cache", "LastScene.stsc");

            try
            {
                File.Delete(scenePath);
            }
            catch (Exception)
            {
            }

            if (gameAssembly?.TryGetTarget(out var assembly) ?? false)
            {
                if (Scene.current != null)
                {
                    try
                    {
                        var scene = Scene.current.Serialize();

                        var json = JsonConvert.SerializeObject(scene.objects, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

                        File.WriteAllText(scenePath, json);
                    }
                    catch (Exception)
                    {
                    }
                }

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

            gameAssemblyLoadContext.Unload();

            gameAssemblyLoadContext = null;

            var time = DateTime.Now;

            while((DateTime.Now - time).TotalSeconds < 1)
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

            var core = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "StapleCore");

            var t = Assembly.GetExecutingAssembly().GetTypes()
                .Concat(Assembly.GetCallingAssembly().GetTypes())
                .Concat(core.GetTypes())
                .Distinct()
                .ToList();

            if (gameAssembly?.TryGetTarget(out var assembly) ?? false)
            {
                t = t.Concat(assembly.GetTypes())
                    .ToList();
            }

            foreach (var v in t)
            {
                TypeCache.RegisterType(v);

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

            registeredComponents = registeredComponents.OrderBy(x => x.Name).ToList();
            registeredEntityTemplates = registeredEntityTemplates.OrderBy(x => x.Name).ToList();
        }
    }
}
