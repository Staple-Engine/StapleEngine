using Newtonsoft.Json.Linq;
using Staple.Internal;
using System.Collections.Generic;

namespace Staple.Tooling;

public class Utilities
{
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
}
