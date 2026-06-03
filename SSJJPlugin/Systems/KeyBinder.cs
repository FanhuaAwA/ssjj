using UnityEngine;

namespace SSJJPlugin.Systems
{
    public class KeyBinder
    {
        public KeyCode AimKey = KeyCode.Mouse1;
        public string AimKeyName = "Mouse1";
        public bool WaitingForInput;

        public KeyCode MenuKey = KeyCode.Home;
        public int MenuKeyIndex;

        private static readonly string[] MenuKeyNames = {
            "Home","Insert","F1","F2","F3","F4","F5","F6","F7","F8","F9","F10","F11","F12",
            "Delete","End","PageUp","PageDown"
        };
        private static readonly KeyCode[] MenuKeyCodes = {
            KeyCode.Home,KeyCode.Insert,KeyCode.F1,KeyCode.F2,KeyCode.F3,KeyCode.F4,
            KeyCode.F5,KeyCode.F6,KeyCode.F7,KeyCode.F8,KeyCode.F9,KeyCode.F10,
            KeyCode.F11,KeyCode.F12,KeyCode.Delete,KeyCode.End,KeyCode.PageUp,KeyCode.PageDown
        };

        public void CycleMenuKey()
        {
            MenuKeyIndex = (MenuKeyIndex + 1) % MenuKeyNames.Length;
            MenuKey = MenuKeyCodes[MenuKeyIndex];
        }

        public string GetMenuKeyName() => MenuKeyNames[MenuKeyIndex];

        public void CheckKeyBinding()
        {
            if (!WaitingForInput) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { WaitingForInput = false; return; }

            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (key == KeyCode.None || key == KeyCode.Home) continue;
                if (Input.GetKeyDown(key))
                {
                    AimKey = key;
                    AimKeyName = key.ToString();
                    WaitingForInput = false;
                    DebugSystem.Log("KEY", $"Aim key bound: {key}");
                    return;
                }
            }
            for (int i = 0; i < 3; i++)
            {
                if (Input.GetMouseButtonDown(i))
                {
                    AimKey = (KeyCode)(323 + i);
                    AimKeyName = $"Mouse{i}";
                    WaitingForInput = false;
                    DebugSystem.Log("KEY", $"Aim key bound: Mouse{i}");
                    return;
                }
            }
        }
    }
}
