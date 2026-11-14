using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Staple.ProjectManagement;

public class ProjectTemplateManager
{
    private const string TAG = "ProjectTemplateManager";

    public class ProjectTemplate
    {
        public ProjectTemplateManifest manifest;
        public string basePath;
        public string additionalSettingsContent;
        public Texture iconHolder;
    }

    public readonly List<ProjectTemplate> templates = [];

    public static readonly ProjectTemplateManager Instance = new();

    public void LoadTemplates(string path, bool loadIcons)
    {
        foreach(var template in templates)
        {
            template.iconHolder?.Destroy();
        }

        templates.Clear();

        string[] directories = [];

        try
        {
            directories = Directory.GetDirectories(path);
        }
        catch(Exception)
        {
            return;
        }

        foreach (var directory in directories)
        {
            try
            {
                if(!File.Exists(Path.Combine(directory, "Template.json")) ||
                    !File.Exists(Path.Combine(directory, "Icon.png")))
                {
                    continue;
                }

                var manifestText = File.ReadAllText(Path.Combine(directory, "Template.json"));

                var manifest = JsonConvert.DeserializeObject<ProjectTemplateManifest>(manifestText);

                if (manifest == null)
                {
                    continue;
                }

                var additionalSettings = "";

                try
                {
                    additionalSettings = File.ReadAllText(Path.Combine(directory, "AdditionalSettings.json"));
                }
                catch(Exception)
                {
                }

                if(loadIcons)
                {
                    var iconPath = Path.Combine(directory, "Icon.png");

                    try
                    {
                        var data = File.ReadAllBytes(iconPath);

                        var texture = Texture.CreateStandard(iconPath, data, StandardTextureColorComponents.RGBA);

                        if(texture != null)
                        {
                            templates.Add(new()
                            {
                                basePath = directory,
                                manifest = manifest,
                                additionalSettingsContent = additionalSettings,
                                iconHolder = texture,
                            });

                            continue;
                        }
                    }
                    catch (Exception)
                    {
                    }
                }

                templates.Add(new()
                {
                    basePath = directory,
                    manifest = manifest,
                    additionalSettingsContent = additionalSettings,
                });
            }
            catch(Exception e)
            {
                Log.Error($"Failed to load project template at {directory}:\n{e}", TAG);
            }
        }

        templates.Sort((a, b) => a.manifest.name.CompareTo(b.manifest.name, StringComparison.InvariantCultureIgnoreCase));
    }
}
