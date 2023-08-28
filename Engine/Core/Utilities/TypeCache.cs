using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Staple
{
    public static class TypeCache
    {
        internal static Dictionary<string, Type> types = new();

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicFields |
            DynamicallyAccessedMemberTypes.NonPublicFields |
            DynamicallyAccessedMemberTypes.PublicMethods |
            DynamicallyAccessedMemberTypes.NonPublicMethods)]
        public static Type GetType(string name)
        {
            return types.TryGetValue(name, out var type) ? type : null;
        }

        public static Type[] AllTypes()
        {
            return types.Values.ToArray();
        }

        public static void RegisterType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
                DynamicallyAccessedMemberTypes.PublicFields |
                DynamicallyAccessedMemberTypes.NonPublicFields |
                DynamicallyAccessedMemberTypes.PublicMethods |
                DynamicallyAccessedMemberTypes.NonPublicMethods)]
            Type type)
        {
            if(types.ContainsKey(type.FullName))
            {
                return;
            }

            types.Add(type.FullName, type);
        }
    }
}
