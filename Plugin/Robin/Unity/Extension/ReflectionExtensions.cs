using System;
using System.Reflection;

namespace Plugins.Unity.Extension
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public static class ReflectionExtensions
    {
        public static T GetFieldValue<T>(this object obj, string fieldName)
        {
            if (obj == null)
            {
                return default;
            }
            FieldInfo field = obj.GetType().GetField(fieldName, BindingAttributes);
            return field == null ? default : (T)field.GetValue(obj);
        }

        public static MethodInfo GetMethodInfo(this object target, string methodName)
        {
            if (target == null)
            {
                return null;
            }
            MethodInfo method = target.GetType().GetMethod(methodName, BindingAttributes);
            return method ?? null;
        }

        public static void InvokeMethodSafely(this object obj, string methodName, params object[] parameters)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }
            var method = obj.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? throw new MissingMethodException();
            method.Invoke(obj, parameters);
        }

        private static readonly BindingFlags BindingAttributes = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    }
}