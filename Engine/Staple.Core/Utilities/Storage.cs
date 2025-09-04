﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Staple;

/// <summary>
/// Storage accessibility class. Use this to query storage and app information.
/// </summary>
public static class Storage
{
    internal static string basePath;

    /// <summary>
    /// The base path where we should store persistent files
    /// </summary>
    internal static string BasePath
    {
        get
        {
            if(basePath != null)
            {
                return basePath;
            }

            basePath = Platform.platformProvider.StorageBasePath;

            return basePath;
        }
    }

    private static readonly Lazy<string> stapleBasePath = new(() =>
    {
        var higherDir = AppContext.BaseDirectory.Split(Path.DirectorySeparatorChar).ToList();

        while (higherDir.Count > 0 && higherDir.LastOrDefault() != "StapleEngine")
        {
            higherDir.RemoveAt(higherDir.Count - 1);
        }

        if (higherDir.Count == 0)
        {
            return null;
        }

        return string.Join(Path.DirectorySeparatorChar, higherDir);
    });

    /// <summary>
    /// Attempts to find the base path for the Staple Engine folder.
    /// Assumes we're running somewhere inside that folder.
    /// Returns null if the directory isn't found.
    /// </summary>
    public static string StapleBasePath => stapleBasePath.Value;

    /// <summary>
    /// The current app's name
    /// </summary>
    public static string AppName
    {
        get;
        private set;
    }

    /// <summary>
    /// The current app's company name
    /// </summary>
    public static string CompanyName
    {
        get;
        private set;
    }

    /// <summary>
    /// The path we should use to store persistent data
    /// </summary>
    public static string PersistentDataPath
    {
        get
        {
            if(AppName == null || CompanyName == null)
            {
                return null;
            }

            return Path.Combine(BasePath, CompanyName, AppName);
        }
    }

    /// <summary>
    /// Updates the current app name and company name
    /// </summary>
    /// <param name="appName">The app name</param>
    /// <param name="companyName">The company name</param>
    internal static void Update(string appName, string companyName)
    {
        AppName = appName;
        CompanyName = companyName;

        try
        {
            var path = PersistentDataPath;

            if(path != null)
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception)
        {
        }
    }
}
