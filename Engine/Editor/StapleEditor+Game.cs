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

                        if(type.IsInterface)
                        {
                            continue;
                        }

                        if(typeof(IStapleAsset).IsAssignableFrom(type))
                        {
                            registeredAssetTypes.AddOrSetKey(type.FullName, type);
                        }
                        else if(typeof(IComponent).IsAssignableFrom(type) &&
                            type.IsInterface == false &&
                            type.GetCustomAttribute<AbstractComponentAttribute>() == null)
                        {
                            registeredComponents.Add(type);
                        }
                        else if (type.IsSubclassOf(typeof(EditorWindow)))
                        {
                            foreach (var method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
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
            }
        }
        catch(Exception)
        {
        }

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

    public void UnloadGame()
    {
        if(gameLoadDisabled)
        {
            return;
        }

        if(gameAssemblyLoadContext != null)
        {
            WeakReference<GameAssemblyLoadContext> game = new(gameAssemblyLoadContext);

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
                var renderSystems = renderSystem.renderSystems
                    .Where(x => x.GetType().Assembly == assembly)
                    .ToList();

                foreach(var r in renderSystems)
                {
                    renderSystem.renderSystems.Remove(r);
                }

                if (Scene.current != null)
                {
                    try
                    {
                        var scene = Scene.current.Serialize();

                        var json = JsonConvert.SerializeObject(scene.objects, Formatting.Indented, new JsonSerializerSettings()
                        {
                            Converters =
                            {
                                new StringEnumConverter(),
                            }
                        });

                        File.WriteAllText(scenePath, json);
                    }
                    catch (Exception)
                    {
                    }
                }

                Scene.current?.world.UnloadComponentsFromAssembly(assembly);

                EntitySystemManager.GetEntitySystem(SubsystemType.FixedUpdate).UnloadSystemsFromAssembly(assembly);
                EntitySystemManager.GetEntitySystem(SubsystemType.Update).UnloadSystemsFromAssembly(assembly);

                for (var i = editorWindows.Count - 1; i >= 0; i--)
                {
                    if (editorWindows[i].GetType().Assembly == assembly)
                    {
                        editorWindows.RemoveAt(i);
                    }
                }

                SetSelectedEntity(Entity.Empty);
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

    public void ReloadTypeCache()
    {
        foreach (var editor in cachedEditors)
        {
            editor.Value?.Destroy();
        }

        TypeCache.Clear();
        registeredAssetTypes.Clear();
        registeredComponents.Clear();
        menuItems.Clear();
        cachedEditors.Clear();
        cachedGizmoEditors.Clear();

        var core = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "StapleCore");

        var t = Assembly.GetExecutingAssembly().GetTypes()
            .Concat(Assembly.GetCallingAssembly().GetTypes())
            .Concat(core.GetTypes())
            .ToList();

        if(gameAssembly?.TryGetTarget(out var assembly) ?? false)
        {
            t = t.Concat(assembly.GetTypes()).ToList();
        }

        foreach (var v in t)
        {
            TypeCache.RegisterType(v);

            if(v.IsInterface)
            {
                continue;
            }

            if(typeof(IStapleAsset).IsAssignableFrom(v))
            {
                registeredAssetTypes.AddOrSetKey(v.FullName, v);
            }
            else if(typeof(IComponent).IsAssignableFrom(v) &&
                v.IsInterface == false &&
                v.GetCustomAttribute<AbstractComponentAttribute>() == null)
            {
                registeredComponents.Add(v);
            }
            else if(v.IsSubclassOf(typeof(EditorWindow)))
            {
                foreach(var method in v.GetMethods(BindingFlags.Static | BindingFlags.Public))
                {
                    var menu = method.GetCustomAttribute<MenuItemAttribute>();

                    if(menu == null)
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
}
