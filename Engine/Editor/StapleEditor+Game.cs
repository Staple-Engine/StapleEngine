using Staple.Internal;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Staple.Editor
{
    internal partial class StapleEditor
    {
        public void LoadGame()
        {
            UnloadGame();

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly", "Game");
            var outPath = Path.Combine(projectDirectory, "bin");

            var assemblyPath = Path.Combine(outPath, "Game.dll");

            try
            {
                if(File.Exists(assemblyPath))
                {
                    gameAssemblyLoadContext = new(AppContext.BaseDirectory);

                    var assembly = gameAssemblyLoadContext.LoadFromAssemblyPath(assemblyPath);

                    if(assembly != null)
                    {
                        gameAssembly = new(assembly);

                        var types = assembly.GetTypes();

                        foreach(var type in types)
                        {
                            TypeCache.RegisterType(type);

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

            registeredComponents = registeredComponents.OrderBy(x => x.Name).ToList();
        }

        public void UnloadGame()
        {
            if(gameAssemblyLoadContext != null)
            {
                WeakReference weak = new(gameAssemblyLoadContext);

                if (gameAssembly?.TryGetTarget(out var assembly) ?? false)
                {
                    var renderSystems = renderSystem.renderSystems
                        .Where(x => x.GetType().Assembly == assembly)
                        .ToList();

                    foreach(var r in renderSystems)
                    {
                        renderSystem.renderSystems.Remove(r);
                    }

                    Scene.current?.world.UnloadComponentsFromAssembly(assembly);
                }

                gameAssembly = null;

                ReloadTypeCache();

                gameAssemblyLoadContext.Unload();

                gameAssemblyLoadContext = null;

                while(weak.IsAlive)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
        }

        public void ReloadTypeCache()
        {
            TypeCache.Clear();
            registeredAssetTypes.Clear();
            registeredComponents.Clear();

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
}
