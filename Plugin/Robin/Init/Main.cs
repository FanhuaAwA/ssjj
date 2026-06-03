using System;
using System.IO;
using System.Reflection;
using System.Threading;
using Plugins.Init;
using UnityEngine;

namespace Plugins
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class Main
    {
        public static void Initialize()
        {
            try
            {
                new Thread(Load)
                {
                    IsBackground = true,
                    Name = "PluginLoader"
                }.Start();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Plugin] Initialize failed: {e}");
            }
        }

        private static void Load()
        {
            try
            {
                Thread.Sleep(500);

                if (!LoadEmbeddedDll("MonoMod.RuntimeDetour.dll"))
                    Debug.LogWarning("[Plugin] MonoMod.RuntimeDetour load failed");
                if (!LoadEmbeddedDll("MonoMod.Utils.dll"))
                    Debug.LogWarning("[Plugin] MonoMod.Utils load failed");
                if (!LoadEmbeddedDll("Mono.Cecil.dll"))
                    Debug.LogWarning("[Plugin] Mono.Cecil load failed");

                Thread.Sleep(200);

                try { HookManager.InitializeHooks(); }
                catch (Exception e) { Debug.LogError($"[Plugin] HookManager init failed: {e}"); }

                try
                {
                    gameObject = new GameObject("Release");
                    UnityEngine.Object.DontDestroyOnLoad(gameObject);
                    gameObject.AddComponent<Loop>();
                    Debug.Log("[Plugin] Loaded successfully");
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Plugin] Loop init failed: {e}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Plugin] Load failed: {e}");
            }
        }

        private static bool LoadEmbeddedDll(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Debug.LogWarning($"[Plugin] Resource not found: {resourceName}");
                    return false;
                }

                var tempFilePath = Path.Combine(Path.GetTempPath(), resourceName);
                using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }

                try
                {
                    var kernelType = assembly.GetType("Kernel32Wrapper");
                    if (kernelType != null)
                    {
                        var loadMethod = kernelType.GetMethod("InvokeLoadLibrary",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
                        if (loadMethod != null)
                        {
                            loadMethod.Invoke(null, new object[] { tempFilePath });
                            return true;
                        }
                    }
                }
                catch { }

                try
                {
                    Assembly.LoadFrom(tempFilePath);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Plugin] Failed to load {resourceName}: {e.Message}");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Plugin] LoadEmbeddedDll({resourceName}) error: {e}");
                return false;
            }
        }

        private static GameObject gameObject;
    }
}
