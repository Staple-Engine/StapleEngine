﻿using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

namespace Staple.Editor;

internal class AndroidBuildProcessor : IBuildPreprocessor
{
    public void OnPreprocessBuild(BuildInfo buildInfo)
    {
        if(buildInfo.platform != AppPlatform.Android)
        {
            return;
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

                if (iconTexture.Resize(pair.Value.FirstOrDefault(), pair.Value.FirstOrDefault()) == false ||
                    backgroundTexture.Resize(pair.Value.LastOrDefault(), pair.Value.LastOrDefault()) == false ||
                    foregroundTexture.Resize(pair.Value.LastOrDefault(), pair.Value.LastOrDefault()) == false)
                {
                    break;
                }

                try
                {
                    File.WriteAllBytes(Path.Combine(projectDirectory, "Resources", pair.Key, "appicon.png"), iconTexture.EncodePNG());
                    File.WriteAllBytes(Path.Combine(projectDirectory, "Resources", pair.Key, "appicon_background.png"), backgroundTexture.EncodePNG());
                    File.WriteAllBytes(Path.Combine(projectDirectory, "Resources", pair.Key, "appicon_foreground.png"), foregroundTexture.EncodePNG());
                }
                catch (Exception)
                {
                }
            }
        }
        catch (Exception)
        {
        }

        bool SaveResource(string path, string data)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            catch (Exception)
            {
            }

            try
            {
                File.WriteAllText(path, data);
            }
            catch (Exception)
            {
                Log.Error($"Failed generating csproj: Failed to save a resource");

                return false;
            }

            return true;
        }

        var strings = $$"""
<resources>
    <string name="app_name">{{projectAppSettings.appName}}</string>
</resources>
""";

        if (SaveResource(Path.Combine(projectDirectory, "Resources", "values", "strings.xml"), strings) == false)
        {
            return;
        }

        var orientationType = "Unspecified";

        if (projectAppSettings.portraitOrientation == false || projectAppSettings.landscapeOrientation == false)
        {
            if (projectAppSettings.portraitOrientation)
            {
                orientationType = "Portrait";
            }
            else if (projectAppSettings.landscapeOrientation)
            {
                orientationType = "Landscape";
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
        TypeCacheRegistration.RegisterAll();

        base.OnCreate(savedInstanceState);
    }
}
""";

        if (SaveResource(Path.Combine(projectDirectory, "PlayerActivity.cs"), activity) == false)
        {
            return;
        }
    }
}