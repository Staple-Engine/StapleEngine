using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Staple.Internal;

/// <summary>
/// Initializer class for exposing functionality from modules to Staple
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public abstract class ModuleInitializer
{
    private static readonly List<ModuleInitializer> loadedInitializers = [];

    /// <summary>
    /// Gets a type list for each type belonging to the module
    /// </summary>
    /// <returns>The types</returns>
    public abstract Dictionary<string, Type> ProvidedTypes { get; }

    /// <summary>
    /// Gets the kind of module this is
    /// </summary>
    /// <returns>The type</returns>
    public abstract ModuleType Kind { get; }

    /// <summary>
    /// Called to initialize the module on startup
    /// </summary>
    public abstract void InitializeModule();

    /// <summary>
    /// Called to deinitialize the module on shutdown
    /// </summary>
    public abstract void CleanupModule();

    /// <summary>
    /// Whether a module type is unique
    /// </summary>
    /// <param name="type">The module type</param>
    /// <returns>Whether it is unique</returns>
    public static bool IsModuleTypeUnique(ModuleType type)
    {
        return type switch
        {
            ModuleType.Physics or ModuleType.Audio => true,
            _ => false,
        };
    }

    internal static void UnloadAll()
    {
        foreach(var module in loadedInitializers)
        {
            try
            {
                module.CleanupModule();
            }
            catch(Exception e)
            {
                Log.Error($"While unloading {module.GetType().FullName}: {e}");
            }
        }
    }

    internal static void LoadAll()
    {
        var moduleTypes = TypeCache.AllTypesSubclassingOrImplementing<ModuleInitializer>();

        foreach (var type in moduleTypes)
        {
            var instance = ObjectCreation.CreateObject<ModuleInitializer>(type);

            if (instance != null)
            {
                try
                {
                    instance.InitializeModule();

                    loadedInitializers.Add(instance);

                    var moduleType = instance.Kind;
                    var types = instance.ProvidedTypes;

                    switch (moduleType)
                    {
                        case ModuleType.Audio:

                            {
                                if (types == null ||
                                    !types.TryGetValue(nameof(AudioSystem.AudioListenerImpl), out var audioListener) ||
                                    !types.TryGetValue(nameof(AudioSystem.AudioSourceImpl), out var audioSource) ||
                                    !types.TryGetValue(nameof(AudioSystem.AudioDeviceImpl), out var audioDevice) ||
                                    !types.TryGetValue(nameof(AudioSystem.AudioClipImpl), out var audioClip))
                                {
                                    Log.Error($"Failed to use audio provider for module {type.FullName}: Invalid types");
                                }
                                else
                                {
                                    AudioSystem.AudioListenerImpl = audioListener;
                                    AudioSystem.AudioSourceImpl = audioSource;
                                    AudioSystem.AudioDeviceImpl = audioDevice;
                                    AudioSystem.AudioClipImpl = audioClip;
                                }
                            }

                            break;

                        case ModuleType.Physics:

                            {
                                if (types == null ||
                                    !types.TryGetValue(nameof(Physics3D.Impl), out var physics))
                                {
                                    Log.Error($"Failed to use physics provider for module {type.FullName}: Invalid types");
                                }
                                else
                                {
                                    Physics3D.ImplType = physics;
                                }
                            }

                            break;
                    }

                    Log.Debug($"Initialized module {type.FullName}");
                }
                catch (Exception e)
                {
                    Log.Error($"Failed to initialize module {type.FullName}: {e}");
                }
            }
        }
    }
}
