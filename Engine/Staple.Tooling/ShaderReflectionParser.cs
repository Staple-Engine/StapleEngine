using Newtonsoft.Json;
using Staple.Internal;
using System;

namespace Staple.Tooling;

public static class ShaderReflectionParser
{
    public static ShaderUniformContainer Parse(string json)
    {
        ShaderReflectionData data = null;

        try
        {
            data = JsonConvert.DeserializeObject<ShaderReflectionData>(json);
        }
        catch(Exception e)
        {
            Log.Debug($"Failed to parse shader reflection data: {e}");

            return null;
        }

        return data?.ToContainer();
    }
}
