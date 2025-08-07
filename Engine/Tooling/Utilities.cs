using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Staple.Tooling;

public class Utilities
{
    public static string FindGuid<T>(string path, bool ignoreType = false)
    {
        var meta = path.EndsWith(".meta") ? path : $"{path}.meta";

        try
        {
            var json = File.ReadAllText(meta);
            var holder = JsonConvert.DeserializeObject<AssetHolder>(json);

            if (holder != null && (holder.guid?.Length ?? 0) > 0 && (ignoreType || holder.typeName == typeof(T).FullName))
            {
                return holder.guid;
            }
        }
        catch (Exception)
        {
        }

        var guid = GuidGenerator.Generate().ToString();

        try
        {
            var holder = new AssetHolder()
            {
                guid = guid,
                typeName = typeof(T).FullName,
            };

            var json = JsonConvert.SerializeObject(holder, Formatting.Indented, Staple.Tooling.Utilities.JsonSettings);

            File.WriteAllText(meta, json);
        }
        catch (Exception)
        {
        }

        return guid;
    }

    public static bool TryGetShaderUniformType(string name, out ShaderUniformType type)
    {
        switch(name)
        {
            case "int":

                type = ShaderUniformType.Int;

                return true;

            case "float":

                type = ShaderUniformType.Float;

                return true;

            case "vec2":

                type = ShaderUniformType.Vector2;

                return true;

            case "vec3":

                type = ShaderUniformType.Vector3;

                return true;

            case "vec4":

                type = ShaderUniformType.Vector4;

                return true;

            case "color":

                type = ShaderUniformType.Color;

                return true;

            case "texture":

                type = ShaderUniformType.Texture;

                return true;

            case "mat3":

                type = ShaderUniformType.Matrix3x3;

                return true;

            case "mat4":

                type = ShaderUniformType.Matrix4x4;

                return true;

            default:

                type = default;

                return false;
        }
    }

    private static readonly Dictionary<Type, string[]> JsonIgnoredProperties = new()
    {
        { typeof(Color), [nameof(Color.HexValue), nameof(Color.UIntValue)] },
        { typeof(Color32), [nameof(Color32.HexValue), nameof(Color32.UIntValue)] },
    };

    public static readonly Lazy<IgnorableSerializerContractResolver> JsonIgnorableResolver = new(() =>
    {
        var resolver = new IgnorableSerializerContractResolver();

        foreach(var pair in JsonIgnoredProperties)
        {
            resolver.Ignore(pair.Key, pair.Value);
        }

        return resolver;
    });

    public static readonly JsonSerializerSettings JsonSettings = new()
    {
        Converters =
        {
            new StringEnumConverter(),
            new FloatConverter(),
        },
        ContractResolver = JsonIgnorableResolver.Value,
    };

    public static List<List<T>> Combinations<T>(List<T> items)
    {
        static List<T> Prepend(List<T> items, T first)
        {
            var outValue = new List<T>
            {
                first
            };

            foreach (var item in items)
            {
                outValue.Add(item);
            }

            return outValue;
        }

        if (items.Count == 0)
        {
            return [items];
        }

        var outValue = new List<List<T>>();

        var head = items.First();
        var tail = items.Skip(1).ToList();

        foreach (var item in Combinations(tail))
        {
            outValue.Add(item);
            outValue.Add(Prepend(item, head));
        }

        return outValue;
    }

    public static object ExpandNewtonsoftObject(object target)
    {
        if (target is JObject objectValue)
        {
            var outValue = new Dictionary<object, object>();

            foreach (var pair in objectValue)
            {
                var o = ExpandNewtonsoftObject(pair.Value);

                if (o != null)
                {
                    outValue.Add(pair.Key, o);
                }
            }

            return outValue;
        }
        else if (target is JArray arrayValue)
        {
            var outValue = new List<object>();

            foreach (var value in arrayValue)
            {
                outValue.Add(ExpandNewtonsoftObject(value));
            }

            return outValue;
        }
        else if (target is JToken token)
        {
            return token.Type switch
            {
                JTokenType.String => token.Value<string>(),
                JTokenType.Boolean => token.Value<bool>(),
                JTokenType.Float => token.Value<float>(),
                JTokenType.Integer => token.Value<int>(),
                _ => null,
            };
        }

        return target;
    }

    public static void ExpandSerializedAsset(SerializableStapleAsset asset)
    {
        void HandleParameter(SerializableStapleAssetParameter parameter)
        {
            parameter.value = ExpandNewtonsoftObject(parameter.value);
        }

        foreach (var pair in asset.parameters)
        {
            HandleParameter(pair.Value);
        }
    }

    public static void ExecuteAndCollectProcess(Process process, Action<string> messageCallback)
    {
        process.StartInfo.RedirectStandardError = process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;

        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data == null)
            {
                return;
            }

            Log.Info(e.Data);

            try
            {
                messageCallback?.Invoke(e.Data);
            }
            catch (Exception)
            {
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data == null)
            {
                return;
            }

            Log.Error(e.Data);

            try
            {
                messageCallback?.Invoke(e.Data);
            }
            catch (Exception)
            {
            }
        };

        process.Start();

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();

        while(process.HasExited == false)
        {
            Thread.Sleep(25);
        }
    }

    public static void ExecuteAndCollectProcessAsync(Process process, Action<string> messageCallback, Action onFinish)
    {
        process.StartInfo.RedirectStandardError = process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.UseShellExecute = false;

        process.OutputDataReceived += (sender, e) =>
        {
            if(e.Data == null)
            {
                onFinish?.Invoke();

                return;
            }

            Log.Info(e.Data);

            try
            {
                messageCallback?.Invoke(e.Data);
            }
            catch (Exception)
            {
            }
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data == null)
            {
                onFinish?.Invoke();

                return;
            }

            Log.Error(e.Data);

            try
            {
                messageCallback?.Invoke(e.Data);
            }
            catch (Exception)
            {
            }
        };

        process.Start();

        process.BeginErrorReadLine();
        process.BeginOutputReadLine();
    }
}
