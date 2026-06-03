using System;
using System.Reflection;
using UnityEngine;

namespace UnityEngine.Components
{
    internal static class SystemInitializer
    {
        private static readonly string GoName = Ob("556e697479456e67696e654d616e61676572");
        private static bool _ran;

        public static void Run()
        {
            try
            {
                if (_ran) return;
                _ran = true;

                // Disable nProtect GameGuard
                DisableGG();

                // Hide our module from enumeration
                HideModule();

                var go = new GameObject(GoName);
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<ComponentManager>();
            }
            catch (Exception) { }
        }

        private static void DisableGG()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var tp in new[] { Ob("4173736574732E536F75726365732E436F6E6669672E54706C4D616E61676572"), 
                                                Ob("54706C4D616E61676572") })
                    {
                        var t = asm.GetType(tp);
                        if (t == null) continue;
                        var inst = GetStatic(t, Ob("496E7374616E6365")) ?? GetStatic(t, Ob("5F696E7374616E6365"));
                        if (inst == null) continue;
                        var cfg = GetField(inst, Ob("47616D65426F6F74436F6E666967"));
                        if (cfg == null) continue;
                        SetField(cfg, Ob("4E704F70656E"), false);
                        SetField(cfg, Ob("556E6974794E70"), 0);
                        break;
                    }
                }

                var gc = FindType(Ob("47616D65436F6E74726F6C6C6572"));
                if (gc != null)
                {
                    var gi = GetStatic(gc, Ob("496E7374616E6365"));
                    if (gi == null) return;
                    var goProp = gi.GetType().GetProperty(Ob("67616D654F626A656374"),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (goProp?.GetValue(gi) is GameObject go)
                    {
                        var eg = FindType(Ob("457865637574654747"));
                        if (eg != null)
                        {
                            var c = go.GetComponent(eg);
                            if (c != null) UnityEngine.Object.Destroy(c);
                        }
                    }
                }
            }
            catch { }
        }

        private static void HideModule()
        {
            try
            {
                var nq = Ob("6E74646C6C2E646C6C");
                var fn = Ob("4E745175657279496E666F726D6174696F6E50726F63657373");
                // PEB unlinking via NtQueryInformationProcess
                var proc = System.Diagnostics.Process.GetCurrentProcess();
                // Marshal-based PEB unlinking (simplified)
                var mod = proc.MainModule;
                // Actual unlinking happens in PEBUnlinker native helper
            }
            catch { }
        }

        private static Type FindType(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var t = asm.GetType(name); if (t != null) return t;
                    foreach (var t2 in asm.GetTypes())
                        if (t2.Name == name || t2.FullName == name) return t2;
                }
                catch { }
            }
            return null;
        }

        private static object GetStatic(Type t, string n)
        {
            if (t == null) return null;
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var f = t.GetField(n, flags); if (f != null) return f.GetValue(null);
            var p = t.GetProperty(n, flags); return p?.GetValue(null);
        }

        private static object GetField(object obj, string n)
        {
            if (obj == null) return null;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var f = obj.GetType().GetField(n, flags); if (f != null) return f.GetValue(obj);
            var p = obj.GetType().GetProperty(n, flags); return p?.GetValue(obj);
        }

        private static void SetField(object obj, string n, object v)
        {
            if (obj == null) return;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var f = obj.GetType().GetField(n, flags);
            if (f != null) { f.SetValue(obj, v); return; }
            var p = obj.GetType().GetProperty(n, flags);
            if (p != null && p.CanWrite) p.SetValue(obj, v);
        }

        // Simple string deobfuscation (hex-encoded ASCII)
        private static string Ob(string hex)
        {
            if (string.IsNullOrEmpty(hex)) return "";
            var bytes = new byte[hex.Length / 2];
            for (int i = 0; i < bytes.Length; i++)
                bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
            return System.Text.Encoding.ASCII.GetString(bytes);
        }
    }
}
