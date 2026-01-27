using Staple.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Staple.ProjectManagement;

public class AndroidBuildProcessor : IBuildPreprocessor
{
    public BuildProcessorResult OnPreprocessBuild(BuildInfo buildInfo)
    {
        if(buildInfo.platform != AppPlatform.Android)
        {
            return BuildProcessorResult.Continue;
        }

        var basePath = buildInfo.basePath;
        var projectDirectory = buildInfo.assemblyProjectPath;
        var projectAppSettings = buildInfo.projectAppSettings;

        try
        {
            var iconData = File.ReadAllBytes(Path.Combine(basePath, "Settings", "Icon.png"));
            var backgroundData = File.ReadAllBytes(Path.Combine(basePath, "Settings", "Icon Background.png"));
            var foregroundData = File.ReadAllBytes(Path.Combine(basePath, "Settings", "Icon Foreground.png"));

            var sizes = new Dictionary<string, List<int>>
            {
                { "mipmap-mdpi", new() { 48, 108 } },
                { "mipmap-hdpi", new() { 72, 162 } },
                { "mipmap-xhdpi", new() { 96, 216 } },
                { "mipmap-xxhdpi", new() { 144, 324 } },
                { "mipmap-xxxhdpi", new() { 192, 432 } },
            };

            foreach (var pair in sizes)
            {
                var iconTexture = Texture.LoadStandard(iconData, StandardTextureColorComponents.RGBA);
                var backgroundTexture = Texture.LoadStandard(backgroundData, StandardTextureColorComponents.RGBA);
                var foregroundTexture = Texture.LoadStandard(foregroundData, StandardTextureColorComponents.RGBA);

                if (iconTexture == null || backgroundTexture == null || foregroundTexture == null)
                {
                    break;
                }

                if (!iconTexture.Resize(pair.Value.FirstOrDefault(), pair.Value.FirstOrDefault()) ||
                    !backgroundTexture.Resize(pair.Value.LastOrDefault(), pair.Value.LastOrDefault()) ||
                    !foregroundTexture.Resize(pair.Value.LastOrDefault(), pair.Value.LastOrDefault()))
                {
                    break;
                }

                var resourcePath = Path.Combine(projectDirectory, "Resources", pair.Key);

                StorageUtils.CreateDirectory(resourcePath);

                if(!StorageUtils.WriteFile(Path.Combine(resourcePath, "appicon.png"), iconTexture.EncodePNG()) ||
                    !StorageUtils.WriteFile(Path.Combine(resourcePath, "appicon_background.png"), backgroundTexture.EncodePNG()) ||
                    !StorageUtils.WriteFile(Path.Combine(resourcePath, "appicon_foreground.png"), foregroundTexture.EncodePNG()))
                {
                    Log.Debug($"{GetType().Name}: Failed to process app icon: Failed to process app icon {pair.Key}");

                    return BuildProcessorResult.Failed;
                }
            }
        }
        catch (Exception e)
        {
            Log.Debug($"{GetType().Name}: Failed to process app icon: {e}");

            return BuildProcessorResult.Failed;
        }

        var strings = $$"""
<resources>
    <string name="app_name">{{projectAppSettings.appName}}</string>
</resources>
""";

        if (!StorageUtils.WriteFile(Path.Combine(projectDirectory, "Resources", "values", "strings.xml"), strings))
        {
            return BuildProcessorResult.Failed;
        }

        var orientationType = "Unspecified";

        if (!projectAppSettings.portraitOrientation || !projectAppSettings.landscapeOrientation)
        {
            if (projectAppSettings.portraitOrientation)
            {
                orientationType = "UserPortrait";
            }
            else if (projectAppSettings.landscapeOrientation)
            {
                orientationType = "UserLandscape";
            }
        }

        var activity = $$"""
using Android.App;
using Android.Content.PM;
using Android.OS;
using Staple;

[Activity(Label = "@string/app_name",
    MainLauncher = true,
    Theme = "@android:style/Theme.NoTitleBar.Fullscreen",
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden,
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleInstance,
    ScreenOrientation = ScreenOrientation.{{orientationType}})]
public class PlayerActivity : StapleActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        StapleCodeGeneration.TypeCacheRegistration.RegisterAll();

        Staple.Internal.TypeCache.Freeze();

        Staple.Internal.ModuleInitializer.LoadAll();

        base.OnCreate(savedInstanceState);
    }
}
""";

        if (!StorageUtils.WriteFile(Path.Combine(projectDirectory, "PlayerActivity.cs"), activity))
        {
            return BuildProcessorResult.Failed;
        }

        return BuildProcessorResult.Success;
    }
}
