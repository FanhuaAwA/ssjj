using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace SSJJPlugin.Utils
{
    public static class ReflectionHelper
    {
        private static readonly Dictionary<string, Type> _typeCache = new Dictionary<string, Type>();
        private static readonly Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();
        private static readonly Dictionary<string, PropertyInfo> _propCache = new Dictionary<string, PropertyInfo>();
        private static readonly Dictionary<string, MethodInfo> _methodCache = new Dictionary<string, MethodInfo>();

        public static Type FindType(string name)
        {
            if (_typeCache.TryGetValue(name, out var cached)) return cached;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(name);
                    if (t != null) { _typeCache[name] = t; return t; }
                    foreach (var t2 in asm.GetTypes())
                    {
                        if (t2.Name == name || t2.FullName == name)
                        {
                            _typeCache[name] = t2;
                            return t2;
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        public static object GetStatic(Type type, string name)
        {
            if (type == null) return null;
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var f = type.GetField(name, flags);
            if (f != null) return f.GetValue(null);
            var p = type.GetProperty(name, flags);
            return p?.GetValue(null);
        }

        public static object GetInstance(object obj, string name)
        {
            if (obj == null) return null;
            var key = $"{obj.GetType().FullName}.{name}";
            FieldInfo fi;
            if (!_fieldCache.TryGetValue(key, out fi))
            {
                fi = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _fieldCache[key] = fi;
            }
            if (fi != null) return fi.GetValue(obj);

            PropertyInfo pi;
            if (!_propCache.TryGetValue(key, out pi))
            {
                pi = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _propCache[key] = pi;
            }
            return pi?.GetValue(obj);
        }

        public static void SetField(object obj, string name, object value)
        {
            if (obj == null) return;
            var key = $"{obj.GetType().FullName}.{name}";
            FieldInfo fi;
            if (!_fieldCache.TryGetValue(key, out fi))
            {
                fi = obj.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _fieldCache[key] = fi;
            }
            if (fi != null) { fi.SetValue(obj, value); return; }

            PropertyInfo pi;
            if (!_propCache.TryGetValue(key, out pi))
            {
                pi = obj.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                _propCache[key] = pi;
            }
            if (pi != null && pi.CanWrite) pi.SetValue(obj, value);
        }

        public static MethodInfo GetMethod(Type type, string name)
        {
            if (type == null) return null;
            var key = $"{type.FullName}.{name}";
            MethodInfo mi;
            if (!_methodCache.TryGetValue(key, out mi))
            {
                mi = type.GetMethod(name, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                _methodCache[key] = mi;
            }
            return mi;
        }

        public static float GetFloat(object obj, string name)
        {
            try
            {
                var v = GetInstance(obj, name);
                if (v == null) return 0f;
                return Convert.ToSingle(v);
            }
            catch { return 0f; }
        }

        public static long GetLong(object obj, string name)
        {
            try
            {
                var v = GetInstance(obj, name);
                if (v == null) return 0;
                return Convert.ToInt64(v);
            }
            catch { return 0; }
        }

        public static int GetInt(object obj, string name)
        {
            try
            {
                var v = GetInstance(obj, name);
                if (v == null) return 0;
                return Convert.ToInt32(v);
            }
            catch { return 0; }
        }

        public static bool GetBool(object obj, string name)
        {
            try
            {
                var v = GetInstance(obj, name);
                if (v == null) return false;
                return Convert.ToBoolean(v);
            }
            catch { return false; }
        }

        public static string GetString(object obj, string name)
        {
            try { return GetInstance(obj, name) as string ?? ""; }
            catch { return ""; }
        }
    }
}
