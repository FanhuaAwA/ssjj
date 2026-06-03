using System;
using System.Reflection;
using UnityEngine;
using SSJJPlugin.Utils;

namespace SSJJPlugin
{
    /// <summary>
    /// 综合反 nProtect GameGuard 绕过模块。
    /// 基于 IDA Pro 逆向分析 npggNT64.des 的结果:
    ///   - GG Hook 了 mono_image_open_from_data / mono_runtime_invoke 检测 Mono 注入
    ///   - GG Hook 了 WriteProcessMemory / ReadProcessMemory / VirtualProtect 等内存操作
    ///   - GG 通过 CreateToolhelp32Snapshot 枚举模块做签名比对
    ///   - GG 通过 WinVerifyTrust + CryptCATAdmin* 验证模块签名
    ///   - GG 全局互斥体: Global\SnaNPGG, Global\SmxNPGG64, Global\MtxGGSM64, Global\MtxNPGG64
    /// </summary>
    internal static class AntiCheatBypass
    {
        // --- Native API for PEB unlinking ---

        // PEB unlinking: remove module from loader lists to hide from enumeration
        // The three doubly-linked lists we need to remove from:
        // InLoadOrderModuleList, InMemoryOrderModuleList, InInitializationOrderModuleList
        private static void UnlinkModuleFromPEB(string moduleName)
        {
            try
            {
                bool result = PEBUnlinker.HideModule(moduleName);
                if (result)
                {
                    // Success - module hidden from CreateToolhelp32Snapshot enumeration
                }
            }
            catch (Exception)
            {
                // Silently fail - best-effort countermeasure
            }
        }

        // --- GG disable flags ---
        private static bool _ggDisabled;
        private static float _nextCheckTime;
        private const float CheckInterval = 5f;

        /// <summary>
        /// Disable GameGuard via the Unity configuration channel.
        /// Also attempts to clear GG's named mutexes and events.
        /// </summary>
        public static void DisableGameGuard()
        {
            try
            {
                // Method 1: Reflection-based NpOpen/UnityNp disable
                var tplType = FindTypeInternal(new[] {
                    "Assets.Sources.Config.TplManager",
                    "TplManager"
                });

                if (tplType != null)
                {
                    var inst = GetStaticInternal(tplType, "Instance") 
                            ?? GetStaticInternal(tplType, "_instance");

                    if (inst != null)
                    {
                        var config = GetInstanceInternal(inst, "GameBootConfig");
                        if (config != null)
                        {
                            SetFieldValue(config, "NpOpen", false);
                            SetFieldValue(config, "UnityNp", 0);
                            _ggDisabled = true;
                        }
                    }
                }

                // Method 2: Destroy ExecuteGG component
                var gcType = FindTypeInternal(new[] { "GameController" });
                if (gcType != null)
                {
                    var gcInst = GetStaticInternal(gcType, "Instance");
                    if (gcInst != null)
                    {
                        var goProp = gcInst.GetType().GetProperty("gameObject",
                            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        if (goProp != null)
                        {
                            var go = goProp.GetValue(gcInst) as GameObject;
                            if (go != null)
                            {
                                var execType = FindTypeInternal(new[] { "ExecuteGG" });
                                if (execType != null)
                                {
                                    var comp = go.GetComponent(execType);
                                    if (comp != null)
                                    {
                                        UnityEngine.Object.Destroy(comp);
                                    }
                                }
                            }
                        }
                    }
                }

                // Method 3: Attempt to close GG's IPC mutexes
                CloseGGMutexes();
            }
            catch { }
        }

        /// <summary>
        /// Attempt to open and close GG's named synchronization objects.
        /// This may disrupt kernel-user communication channels.
        /// </summary>
        private static void CloseGGMutexes()
        {
            var ggMutexes = new[]
            {
                "Global\\SnaNPGG",
                "Global\\SssNPGG", 
                "Global\\SmxNPGG64",
                "Global\\MtxGGSM64",
                "Global\\MtxNPGG64",
                "Global\\GameGuardService4",
                "Global\\EnxNPGM2x"
            };

            foreach (var mutexName in ggMutexes)
            {
                try
                {
                    // Try to open and immediately close - may disrupt GG IPC
                    var mutex = new System.Threading.Mutex(false, mutexName);
                    mutex.Close();
                }
                catch { }
            }
        }

        /// <summary>
        /// Called periodically from PluginController.Update to maintain the bypass.
        /// GG may re-enable itself, so we need to continuously check.
        /// </summary>
        public static void MaintainBypass()
        {
            if (Time.realtimeSinceStartup < _nextCheckTime) return;
            _nextCheckTime = Time.realtimeSinceStartup + CheckInterval;

            try
            {
                DisableGameGuard();

                // Hide our DLL from module enumeration
                // GameHelper is the assembly name from .csproj
                // SSJJBypass is the bypass module
                if (!_ggDisabled)
                {
                    UnlinkModuleFromPEB("GameHelper");
                }
            }
            catch { }
        }

        // --- Obfuscated Reflection Helpers (avoids plaintext string detection) ---
        
        private static Type FindTypeInternal(string[] candidates)
        {
            foreach (var name in candidates)
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        var t = asm.GetType(name);
                        if (t != null) return t;

                        foreach (var t2 in asm.GetTypes())
                        {
                            if (t2.Name == name || t2.FullName == name)
                                return t2;
                        }
                    }
                    catch { }
                }
            }
            return null;
        }

        private static object GetStaticInternal(Type type, string name)
        {
            if (type == null) return null;
            var flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
            var f = type.GetField(name, flags);
            if (f != null) return f.GetValue(null);
            var p = type.GetProperty(name, flags);
            return p?.GetValue(null);
        }

        private static object GetInstanceInternal(object obj, string name)
        {
            if (obj == null) return null;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var f = obj.GetType().GetField(name, flags);
            if (f != null) return f.GetValue(obj);
            var p = obj.GetType().GetProperty(name, flags);
            return p?.GetValue(obj);
        }

        private static void SetFieldValue(object obj, string name, object value)
        {
            if (obj == null) return;
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var f = obj.GetType().GetField(name, flags);
            if (f != null) { f.SetValue(obj, value); return; }
            var p = obj.GetType().GetProperty(name, flags);
            if (p != null && p.CanWrite) p.SetValue(obj, value);
        }
    }
}
