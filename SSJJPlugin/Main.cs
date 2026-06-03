using System;
using UnityEngine;

namespace SSJJPlugin
{
    public static class Loader
    {
        public static void Load()
        {
            try
            {
                Debug.Log("[GameHelper] Loader.Load() called");
                var go = new GameObject("GameHelper");
                UnityEngine.Object.DontDestroyOnLoad(go);
                go.AddComponent<PluginController>();
                Debug.Log("[GameHelper] GameObject created");
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameHelper] Load error: {e}");
            }
        }
    }
}
