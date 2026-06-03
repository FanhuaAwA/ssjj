using System;
using System.Collections.Generic;
using UnityEngine;

namespace SSJJPlugin.Systems
{
    public class DebugSystem
    {
        private static readonly List<string> _logs = new List<string>();
        private const int MaxLogs = 200;
        private Vector2 _scrollPos;

        public static void Log(string tag, string msg)
        {
            _logs.Add($"[{DateTime.Now:HH:mm:ss}] [{tag}] {msg}");
            if (_logs.Count > MaxLogs) _logs.RemoveAt(0);
        }

        public static void LogError(string ctx, Exception e)
        {
            Log("ERROR", $"{ctx}: {e.GetType().Name} - {e.Message}");
        }

        public static void LogEsp(string name, Vector3 pos, float hp, float maxHp, int team)
        {
            Log("ESP", $"{name} Pos=({pos.x:F1},{pos.y:F1},{pos.z:F1}) HP={hp}/{maxHp} Team={team}");
        }

        public static void LogAim(string target, float dist, string bone)
        {
            Log("AIM", $"Lock:{target} Dist={dist:F1}m Bone={bone}");
        }

        public void DrawPanel()
        {
            GUILayout.Label($"<color=cyan>=== Debug Log ({_logs.Count}/{MaxLogs}) ===</color>");
            if (GUILayout.Button("Clear Log")) { _logs.Clear(); Log("SYS", "Log cleared"); }
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.Height(400));
            for (int i = _logs.Count - 1; i >= 0; i--)
            {
                var l = _logs[i];
                if (l.Contains("[ERROR]")) GUILayout.Label($"<color=red>{l}</color>");
                else if (l.Contains("[WARN]")) GUILayout.Label($"<color=yellow>{l}</color>");
                else if (l.Contains("[AIM]")) GUILayout.Label($"<color=magenta>{l}</color>");
                else if (l.Contains("[ESP]")) GUILayout.Label($"<color=lime>{l}</color>");
                else GUILayout.Label($"<color=white>{l}</color>");
            }
            GUILayout.EndScrollView();
        }
    }
}
