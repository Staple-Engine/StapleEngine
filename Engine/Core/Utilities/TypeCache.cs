using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Staple
{
    public static class TypeCache
    {
        internal static Dictionary<string, Type> types = new();

        public static void Clear()
        {
            types.Clear();
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
        public static Type GetType(string name)
        {
            if(types.TryGetValue(name, out var type))
            {
                return type;
            }

            type = Type.GetType(name);

            if(type != null)
            {
                types.Add(name, type);
            }

            return type;
        }

        public static Type[] AllTypes()
        {
            return types.Values.ToArray();
        }

        public static void RegisterType(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
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
