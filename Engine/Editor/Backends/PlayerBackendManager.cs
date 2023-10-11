using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.Internal
{
    internal class PlayerBackendManager
    {
        private readonly List<PlayerBackend> backends = new();

        public static string[] BackendNames { get; private set; }

        public static readonly PlayerBackendManager Instance = new();

        public void Initialize()
        {
            if(backends.Count != 0)
            {
                return;
            }

            try
            {
                var basePath = Path.Combine(Storage.StapleBasePath, "Staging", "Player Backends");

                var directories = Directory.GetDirectories(basePath);

                foreach(var directory in directories)
                {
                    try
                    {
                        var json = File.ReadAllText(Path.Combine(directory, "PlayerBackend.json"));

                        var backend = JsonConvert.DeserializeObject<PlayerBackend>(json);

                        if(backend != null && backend.IsValid())
                        {
                            backend.basePath = directory;

                            backends.Add(backend);
                        }
                    }
                    catch(Exception)
                    {
                        continue;
                    }
                }
            }
            catch(Exception)
            {
            }

            BackendNames = backends.Select(x => x.name).ToArray();
        }

        public PlayerBackend GetBackend(string name)
        {
            return backends.FirstOrDefault(x => x.name == name);
        }

        public PlayerBackend GetBackend(AppPlatform platform)
        {
            return backends.FirstOrDefault(x => x.platform == platform);
        }
    }
}
