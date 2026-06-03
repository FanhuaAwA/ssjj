using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Plugins.Hacks;
using Plugins.Hacks.Functions;
using Plugins.Hacks.Players;
using Plugins.Hacks.Visuals;
using Plugins.Unity;
using Plugins.Utils;
using UnityEngine;

namespace Plugins.Init
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class Loop : MonoBehaviour
    {
        private void Awake()
        {
            try
            {
                global::UnityEngine.Object.DontDestroyOnLoad(this);
                this.InitPlugins();
            }
            catch
            {
            }
            this.ExecuteModuleMethod(delegate (ModuleBase module)
{
    module.Awake();
});
        }

        private void Start()
        {
            this.ExecuteModuleMethod(delegate (ModuleBase module)
{
    module.Start();
});
        }

        private void OnGUI()
        {
            this.ExecuteModuleMethod(delegate (ModuleBase module)
{
    this.BeginWatch(module.GetType().Name + " OnGUI");
    module.OnGUI();
    this.EndWatch();
});
            GizmosPro.InvokeOnGUI();
        }

        private void Update()
        {
            this.ExecuteModuleMethod(delegate (ModuleBase module)
{
    this.BeginWatch(module.GetType().Name + " Update");
    module.Update();
    this.EndWatch();
});
        }

        private void FixedUpdate()
        {
            this.ExecuteModuleMethod(delegate (ModuleBase module)
{
    this.BeginWatch(module.GetType().Name + " FixedUpdate");
    module.FixedUpdate();
    this.EndWatch();
});
        }

        private void LateUpdate()
        {
            this.ExecuteModuleMethod(delegate (ModuleBase module)
{
    this.BeginWatch(module.GetType().Name + " LateUpdate");
    module.LateUpdate();
    this.EndWatch();
});
        }

        private void OnDestroy()
        {
            this.ExecuteModuleMethod(delegate (ModuleBase module)
{
    module.OnDestroy();
});
            global::UnityEngine.Object.Destroy(this);
            HookManager.UnHook();
        }

        public void AddPlugin<T>() where T : ModuleBase, new()
        {
            if (!this.modules.ContainsKey(typeof(T)))
            {
                this.modules.Add(typeof(T), new T());
            }
        }

        public static T GetPlugin<T>() where T : ModuleBase
        {
            if (Loop.ins == null)
            {
                Loop.ins = GameObject.Find("Release").GetComponent<Loop>();
            }
            return Loop.ins.modules.TryGetValue(typeof(T), out ModuleBase moduleBase) ? moduleBase as T : default;
        }

        public void InitPlugins()
        {
            this.AddPlugin<DebugModule>();
            this.AddPlugin<Hacks.Module>();
            this.AddPlugin<Menu>();
            this.AddPlugin<PlayerCollector>();
            this.AddPlugin<Esp>();
            this.AddPlugin<Glow>();
            this.AddPlugin<SmoothRecoilControl>();
            this.AddPlugin<Aimbot>();
            this.AddPlugin<AutoFire>();
            this.AddPlugin<ChatModule>();
            this.AddPlugin<FakeScreenshotSender>();
            this.AddPlugin<PlayBackSystem>();
        }

        private void ExecuteModuleMethod(Action<ModuleBase> action)
        {
            foreach (ModuleBase moduleBase in this.modules.Values)
            {
                try
                {
                    action(moduleBase);
                }
                catch
                {
                }
            }
        }

        private void BeginWatch(string name)
        {
            this._currentWatchName = name;
            this.watch.Restart();
        }

        private void EndWatch()
        {
            this.watch.Stop();
        }

        private string _currentWatchName = "";
        public static Loop ins;
        private readonly Stopwatch watch = new Stopwatch();
        private readonly Dictionary<Type, ModuleBase> modules = new Dictionary<Type, ModuleBase>();
    }
}