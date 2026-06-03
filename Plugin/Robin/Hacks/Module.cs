using System.Reflection;
using Assets.Sources.Modules.Ui.UiEventCondition;
using Plugins.Hacks;
using Plugins.Hacks.Functions;
using Plugins.Unity;
using UnityEngine;

namespace Plugins.Hacks
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class Module : ModuleBase
    {
        public override void Update()
        {
            SyncMenuKey();

            if (Menu.waitingForAimKey)
            {
                DetectAimKeyBinding();
                return;
            }

            if (Input.GetKeyDown(Menu.menuToggleKey))
            {
                Menu.show = !Menu.show;
            }
            if (Input.GetKeyDown(KeyCode.Alpha7))
            {
                Menu.HeadYaw = Menu.ForWard;
            }
            if (Input.GetKeyDown(KeyCode.Alpha8))
            {
                Menu.HeadYaw = Menu.Up;
            }
            if (Input.GetKeyDown(KeyCode.Alpha9))
            {
                Menu.HeadYaw = Menu.Down;
            }
            if (Menu.Sniper && Input.GetKeyDown(KeyCode.Mouse1))
            {
                MouseSimulater.ForceMouse(0, MouseSimulater.InputST.TrueOnce);
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                Menu.Act = !Menu.Act;
            }
            Contexts.sharedInstance.battleRoom.roomData.IsObserver = Menu.Observer;
            if (Menu.rdm)
            {
                this.timer += Time.deltaTime; if (this.timer >= this.interval)
                {
                    this.timer = 0f; HookManager.tempTick = new System.Random().Next(0, 300);
                }
            }

            UpdateFpsDisplay();

            static void UpdateFpsDisplay()
            {
                FpsDisplay fpsDisplayInstance = FpsDisplay.GetInstance();
                int averageFps = Mathf.RoundToInt(fpsDisplayInstance.GetFpsAverage());
                bool c4Active = UiIEventCondition.Get_c4Message_Active();
                int c4Time = (int)(UiIEventCondition.Get_c4Message_RemainTime() / 1000f);
                fpsDisplayInstance._text.text = "";
                string c4Str = c4Active ? $"剩余时间:{c4Time}" : "未安装";
                fpsDisplayInstance._text2.text = $"平均帧:{averageFps}FPS  C4信息:{c4Str}";
            }
        }

        private void SyncMenuKey()
        {
            if (Menu.menuKeyIndex >= 0 && Menu.menuKeyIndex < menuKeyCodes.Length)
            {
                Menu.menuToggleKey = menuKeyCodes[Menu.menuKeyIndex];
            }
        }

        private void DetectAimKeyBinding()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Menu.waitingForAimKey = false;
                return;
            }

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None) continue;
                if (Input.GetKeyDown(key))
                {
                    Menu.aimKey = key;
                    Menu.aimKeyName = key.ToString();
                    Menu.waitingForAimKey = false;
                    DebugModule.Log("KeyBind", $"自瞄热键绑定: {key}");
                    return;
                }
            }

            for (int i = 0; i < 3; i++)
            {
                if (Input.GetMouseButtonDown(i))
                {
                    Menu.aimKey = (KeyCode)(323 + i);
                    Menu.aimKeyName = $"Mouse{i}";
                    Menu.waitingForAimKey = false;
                    DebugModule.Log("KeyBind", $"自瞄热键绑定: Mouse{i}");
                    return;
                }
            }
        }

        private static readonly KeyCode[] menuKeyCodes = new KeyCode[]
        {
            KeyCode.Home, KeyCode.Insert, KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4,
            KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10,
            KeyCode.F11, KeyCode.F12, KeyCode.Delete, KeyCode.End, KeyCode.PageUp, KeyCode.PageDown
        };

        private float timer; private readonly float interval = 2.5f;
    }
}
