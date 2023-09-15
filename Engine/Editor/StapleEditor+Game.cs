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

            var projectDirectory = Path.Combine(basePath, "Cache", "Assembly");
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
                            if(typeof(IComponent).IsAssignableFrom(type) || typeof(IEntitySystem).IsAssignableFrom(type))
                            {
                                TypeCache.RegisterType(type);
                            }
                        }
                    }
                }
            }
            catch(Exception)
            {
            }
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

                    if(Scene.current != null)
                    {
                        Scene.current.world.UnloadComponentsFromAssembly(assembly);
                    }
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

            var core = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(x => x.GetName().Name == "StapleCore");

            var t = Assembly.GetExecutingAssembly().GetTypes()
                .Concat(Assembly.GetCallingAssembly().GetTypes())
                .Concat(core.GetTypes())
                .ToList();

            if(gameAssembly?.TryGetTarget(out var assembly) ?? false)
            {
                t = t.Concat(assembly.GetTypes()).ToList();
            }

            t = t
                .Where(x => (typeof(IComponent).IsAssignableFrom(x) || typeof(IEntitySystem).IsAssignableFrom(x)) && x.IsInterface == false)
                .ToList();

            foreach (var v in t)
            {
                TypeCache.RegisterType(v);
            }
        }
    }
}
