using System;
using System.Reflection;

namespace Staple
{
    internal static class ComponentExtensions
    {
        public static void Invoke(this IComponent self, string name)
        {
            const BindingFlags flags = BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;

            MethodInfo method = self.GetType().GetMethod(name, flags);

            try
            {
                if (method != null && method.GetParameters().Length == 0)
                {
                    method.Invoke(self, new object[0]);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        public static void Invoke<T>(this IComponent self, string name, T value)
        {
            const BindingFlags flags = BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;

            MethodInfo method = self.GetType().GetMethod(name, flags);

            try
            {
                if (method != null && method.GetParameters().Length == 1)
                {
                    method.Invoke(self, new object[] { value });
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        public static void Invoke<T, T2>(this IComponent self, string name, T arg, T2 arg2)
        {
            const BindingFlags flags = BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.Instance;

            MethodInfo method = self.GetType().GetMethod(name, flags);

            try
            {
                if (method != null && method.GetParameters().Length == 2)
                {
                    method.Invoke(self, new object[] { arg, arg2 });
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }
    }
}
