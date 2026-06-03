using UnityEngine;

namespace UnityEngine.Components
{
    public class MenuSystem
    {
        public bool Visible = true;
        private int _tab;
        private readonly string[] _tabs = { "View", "Ctrl", "Log" };
        private readonly RenderHelper _rh;
        private readonly CameraController _cc;
        private readonly DebugSystem _dbg;
        private readonly KeyBinder _kb;

        public MenuSystem(RenderHelper rh, CameraController cc, DebugSystem dbg, KeyBinder kb)
        {
            _rh = rh; _cc = cc; _dbg = dbg; _kb = kb;
        }

        public void Draw()
        {
            if (!Visible) return;

            GUILayout.BeginArea(new Rect(10, 10, 480, 700));
            GUI.Box(new Rect(0, 0, 480, 700), "");
            GUILayout.BeginVertical();

            GUILayout.Label($"<color=cyan>Unity Tools</color>  [{_kb.MenuKey} Toggle]");
            GUILayout.BeginHorizontal();
            for (int i = 0; i < _tabs.Length; i++)
                if (GUILayout.Button(_tabs[i])) _tab = i;
            GUILayout.EndHorizontal();

            switch (_tab)
            {
                case 0: DrawViewTab(); break;
                case 1: DrawCtrlTab(); break;
                case 2: _dbg.DrawPanel(); break;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void DrawViewTab()
        {
            _rh.Enabled = GUILayout.Toggle(_rh.Enabled, "Overlay On");
            _rh.ShowBox = GUILayout.Toggle(_rh.ShowBox, "Box");
            _rh.ShowHealth = GUILayout.Toggle(_rh.ShowHealth, "Health");
            _rh.ShowName = GUILayout.Toggle(_rh.ShowName, "Name");
            _rh.ShowDistance = GUILayout.Toggle(_rh.ShowDistance, "Dist");
            _rh.ShowBone = GUILayout.Toggle(_rh.ShowBone, "Skeleton");
            _rh.ShowLine = GUILayout.Toggle(_rh.ShowLine, "Tracers");
            if (GUILayout.Button($"Box: {(_rh.BoxType == 0 ? "Full" : "Corner")}"))
                _rh.BoxType = 1 - _rh.BoxType;
        }

        private void DrawCtrlTab()
        {
            _cc.Enabled = GUILayout.Toggle(_cc.Enabled, "Assist On");
            _rh.ShowFov = GUILayout.Toggle(_rh.ShowFov, "FOV Circle");
            _rh.ShowAimLine = GUILayout.Toggle(_rh.ShowAimLine, "Aim Line");
            _rh.RecoilControl = GUILayout.Toggle(_rh.RecoilControl, "Recoil Fix");
            _rh.VisibilityCheck = GUILayout.Toggle(_rh.VisibilityCheck, "Vis Check");
            _rh.IgnoreInvincible = GUILayout.Toggle(_rh.IgnoreInvincible, "Skip Invuln");

            GUILayout.Label($"Speed: {_rh.Speed:F0}");
            _rh.Speed = GUILayout.HorizontalSlider(_rh.Speed, 1, 300);
            GUILayout.Label($"FOV: {_rh.Fov:F1}");
            _rh.Fov = GUILayout.HorizontalSlider(_rh.Fov, 1, 90);

            if (GUILayout.Button($"Part: {(_rh.BodyIndex == 0 ? "Head" : _rh.BodyIndex == 1 ? "Chest" : _rh.BodyIndex == 2 ? "Belly" : "Auto")}"))
                _rh.BodyIndex = (_rh.BodyIndex + 1) % 4;

            GUILayout.Space(10);
            GUILayout.BeginHorizontal();
            if (_kb.WaitingForInput)
                GUILayout.Label("<color=yellow>Press key...</color>");
            else
                GUILayout.Label($"Key: <color=cyan>{_kb.AimKeyName}</color>");
            if (GUILayout.Button("Set", GUILayout.Width(60)))
                _kb.WaitingForInput = true;
            if (GUILayout.Button("Auto", GUILayout.Width(60)))
            {
                _kb.AimKey = KeyCode.None; _kb.AimKeyName = "Auto"; _kb.WaitingForInput = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);
            if (GUILayout.Button($"Menu Key: {_kb.GetMenuKeyName()}"))
                _kb.CycleMenuKey();
        }
    }
}
