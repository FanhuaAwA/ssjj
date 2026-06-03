using UnityEngine;

namespace SSJJPlugin.Systems
{
    public class MenuSystem
    {
        public bool Visible = true;
        private int _tab;
        private readonly string[] _tabs = { "ESP", "Aim", "Debug" };
        private readonly EspSystem _esp;
        private readonly AimbotSystem _aim;
        private readonly DebugSystem _debug;
        private readonly KeyBinder _keyBinder;

        public MenuSystem(EspSystem esp, AimbotSystem aim, DebugSystem debug, KeyBinder keyBinder)
        {
            _esp = esp;
            _aim = aim;
            _debug = debug;
            _keyBinder = keyBinder;
        }

        public void Draw()
        {
            if (!Visible) return;

            GUILayout.BeginArea(new Rect(10, 10, 480, 700));
            GUI.Box(new Rect(0, 0, 480, 700), "");
            GUILayout.BeginVertical();

            GUILayout.Label($"<color=cyan>GameHelper v2.0</color>  [{_keyBinder.MenuKey} Toggle]");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _tabs.Length; i++)
            {
                if (GUILayout.Button(_tabs[i])) _tab = i;
            }
            GUILayout.EndHorizontal();

            switch (_tab)
            {
                case 0: DrawEspTab(); break;
                case 1: DrawAimTab(); break;
                case 2: _debug.DrawPanel(); break;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawEspTab()
        {
            _esp.Enabled = GUILayout.Toggle(_esp.Enabled, "ESP On");
            _esp.ShowBox = GUILayout.Toggle(_esp.ShowBox, "Box");
            _esp.ShowHealth = GUILayout.Toggle(_esp.ShowHealth, "Health Bar");
            _esp.ShowName = GUILayout.Toggle(_esp.ShowName, "Name");
            _esp.ShowDistance = GUILayout.Toggle(_esp.ShowDistance, "Distance");
            _esp.ShowBone = GUILayout.Toggle(_esp.ShowBone, "Skeleton");
            _esp.ShowLine = GUILayout.Toggle(_esp.ShowLine, "Tracer Line");
            if (GUILayout.Button($"Box Type: {(_esp.BoxType == 0 ? "Full" : "Corner")}"))
                _esp.BoxType = 1 - _esp.BoxType;
        }

        private void DrawAimTab()
        {
            _aim.Enabled = GUILayout.Toggle(_aim.Enabled, "Aimbot On");
            _esp.ShowFov = GUILayout.Toggle(_esp.ShowFov, "Show FOV Circle");
            _esp.ShowAimLine = GUILayout.Toggle(_esp.ShowAimLine, "Show Aim Line");
            _esp.RecoilControl = GUILayout.Toggle(_esp.RecoilControl, "Recoil Control");
            _esp.VisibilityCheck = GUILayout.Toggle(_esp.VisibilityCheck, "Visibility Check");
            _esp.IgnoreInvincible = GUILayout.Toggle(_esp.IgnoreInvincible, "Ignore Invincible");

            GUILayout.Label($"Speed: {_esp.Speed:F0}");
            _esp.Speed = GUILayout.HorizontalSlider(_esp.Speed, 1, 300);
            GUILayout.Label($"FOV: {_esp.Fov:F1}");
            _esp.Fov = GUILayout.HorizontalSlider(_esp.Fov, 1, 90);

            if (GUILayout.Button($"Body: {_aim.GetBodyName(_esp.BodyIndex)}"))
                _esp.BodyIndex = (_esp.BodyIndex + 1) % 4;

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (_keyBinder.WaitingForInput)
                GUILayout.Label("<color=yellow>Press any key...</color>");
            else
                GUILayout.Label($"Aim Key: <color=cyan>{_keyBinder.AimKeyName}</color>");
            if (GUILayout.Button("Set", GUILayout.Width(60)))
                _keyBinder.WaitingForInput = true;
            if (GUILayout.Button("Always", GUILayout.Width(60)))
            {
                _keyBinder.AimKey = KeyCode.None;
                _keyBinder.AimKeyName = "Always";
                _keyBinder.WaitingForInput = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (GUILayout.Button($"Menu Key: {_keyBinder.GetMenuKeyName()}"))
                _keyBinder.CycleMenuKey();
        }
    }
}
