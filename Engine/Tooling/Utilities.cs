using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Staple.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Staple.Tooling;

public class Utilities
{
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

    public static void ExpandSerializedAsset(SerializableStapleAsset asset)
    {
        object HandleValue(object target)
        {
            if (target is JObject objectValue)
            {
                var outValue = new Dictionary<object, object>();

                foreach (var pair in objectValue)
                {
                    outValue.Add(pair.Key, HandleValue(pair.Value));
                }

                return outValue;
            }
            else if (target is JArray arrayValue)
            {
                var outValue = new List<object>();

                foreach (var value in arrayValue)
                {
                    outValue.Add(HandleValue(value));
                }

                return outValue;
            }
            else if (target is JToken token)
            {
                switch (token.Type)
                {
                    case JTokenType.String:

                        return token.Value<string>();

                    case JTokenType.Boolean:

                        return token.Value<bool>();

                    case JTokenType.Float:

                        return token.Value<float>();

                    case JTokenType.Integer:

                        return token.Value<int>();

                    default:

                        return null;
                }
            }
            else
            {
                return target;
            }
        }

        void HandleParameter(SerializableStapleAssetParameter parameter)
        {
            parameter.value = HandleValue(parameter.value);
        }

        foreach (var pair in asset.parameters)
        {
            HandleParameter(pair.Value);
        }
    }

    public static void ExecuteAndCollectProcess(Process process, Action<string> messageCallback)
    {
        process.StartInfo.RedirectStandardError = process.StartInfo.RedirectStandardOutput = true;

        using var waitHandle = new AutoResetEvent(false);

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data == null)
            {
                waitHandle.Set();

                return;
            }

            Log.Info(args.Data);

            try
            {
                messageCallback?.Invoke(args.Data);
            }
            catch (Exception)
            {
            }
        };

        process.Start();

        process.BeginOutputReadLine();

        process.WaitForExit(300000);

        waitHandle.WaitOne();
    }

    public static void ExecuteAndCollectProcessAsync(Process process, Action<string> messageCallback, Action onFinish)
    {
        process.StartInfo.RedirectStandardError = process.StartInfo.RedirectStandardOutput = true;

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data == null)
            {
                onFinish?.Invoke();

                return;
            }

            Log.Info(args.Data);

            try
            {
                messageCallback?.Invoke(args.Data);
            }
            catch(Exception)
            {
            }
        };

        process.Start();

        process.BeginOutputReadLine();
    }
}
