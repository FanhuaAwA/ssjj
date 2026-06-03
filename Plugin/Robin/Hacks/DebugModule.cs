using System;
using System.Collections.Generic;
using System.Reflection;
using Plugins.Unity;
using Plugins.Utils;
using UnityEngine;

namespace Plugins.Hacks
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class DebugModule : ModuleBase
    {
        public static List<string> logs = new List<string>();
        private static readonly int maxLogs = 200;
        private Vector2 scrollPos;
        private bool apiChecked;

        public static void Log(string tag, string message)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] [{tag}] {message}";
            logs.Add(entry);
            if (logs.Count > maxLogs)
            {
                logs.RemoveAt(0);
            }
        }

        public override void Awake()
        {
            Log("System", "DebugModule 初始化完成");
        }

        public override void Start()
        {
            CheckApiCompatibility();
        }

        public override void Update()
        {
            if (!apiChecked && GameManager.IsGameActive)
            {
                CheckApiCompatibility();
            }
        }

        private void CheckApiCompatibility()
        {
            apiChecked = true;
            Log("API", "=== 开始API兼容性检查 ===");

            CheckType("Contexts", "Contexts");
            CheckType("PlayerContext", "PlayerContext");
            CheckType("PlayerEntity", "PlayerEntity");
            CheckType("PlayerEntityData", "PlayerEntityData");
            CheckType("BasicInfoComponent", "BasicInfoComponent");
            CheckType("FposComponent", "FposComponent");
            CheckType("HitBoxComponent", "HitBoxComponent");
            CheckType("OrientationComponent", "OrientationComponent");
            CheckType("LifeComponent", "LifeComponent");
            CheckType("MoveComponent", "MoveComponent");
            CheckType("FovComponent", "FovComponent");
            CheckType("WeaponEntity", "WeaponEntity");
            CheckType("Snapshot", "Snapshot");
            CheckType("GameController", "GameController");
            CheckType("TplManager", "TplManager");

            CheckMethod("SSJJBase.Obscure.Seed", "Instance", "Seed单例");
            CheckMethod("SSJJMath.VectorCoordConverter", "SsjjToUnity", "坐标转换");
            CheckMethod("Assets.Sources.Utils.Weapon.FireUtility", "BulletTrace", "弹道追踪");
            CheckMethod("Assets.Sources.Utils.Player.PlayerUtility", "PlayerLength2D", "玩家距离");

            CheckAntiCheat();

            Log("API", "=== API兼容性检查完成 ===");
        }

        private void CheckType(string typeName, string label)
        {
            try
            {
                Type type = Type.GetType(typeName);
                if (type == null)
                {
                    foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = asm.GetType(typeName);
                        if (type != null) break;
                    }
                }
                if (type != null)
                {
                    Log("API", $"{label}: OK ({type.FullName})");
                }
                else
                {
                    Log("WARN", $"{label}: 未找到类型 {typeName}");
                }
            }
            catch (Exception e)
            {
                Log("ERROR", $"{label}: 检查失败 - {e.Message}");
            }
        }

        private void CheckMethod(string typeName, string methodName, string label)
        {
            try
            {
                Type type = null;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = asm.GetType(typeName);
                    if (type != null) break;
                }
                if (type == null)
                {
                    Log("WARN", $"{label}: 类型 {typeName} 未找到");
                    return;
                }
                MethodInfo method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                if (method != null)
                {
                    Log("API", $"{label}: OK ({method.ReturnType.Name} {methodName})");
                }
                else
                {
                    Log("WARN", $"{label}: 方法 {methodName} 未找到");
                }
            }
            catch (Exception e)
            {
                Log("ERROR", $"{label}: 检查失败 - {e.Message}");
            }
        }

        private void CheckAntiCheat()
        {
            try
            {
                Type tplManagerType = null;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    tplManagerType = asm.GetType("TplManager");
                    if (tplManagerType != null) break;
                }
                if (tplManagerType != null)
                {
                    Log("AC", "TplManager: 存在");
                    PropertyInfo instanceProp = tplManagerType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                    if (instanceProp != null)
                    {
                        object instance = instanceProp.GetValue(null);
                        if (instance != null)
                        {
                            PropertyInfo bootConfigProp = instance.GetType().GetProperty("GameBootConfig");
                            if (bootConfigProp != null)
                            {
                                object config = bootConfigProp.GetValue(instance);
                                if (config != null)
                                {
                                    FieldInfo npOpen = config.GetType().GetField("NpOpen");
                                    if (npOpen != null)
                                    {
                                        Log("AC", $"NpOpen 字段: 存在, 当前值={npOpen.GetValue(config)}");
                                    }
                                    else
                                    {
                                        Log("WARN", "NpOpen 字段: 未找到, 可能已重命名");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    Log("WARN", "TplManager: 未找到");
                }

                Type executeGgType = null;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    executeGgType = asm.GetType("ExecuteGG");
                    if (executeGgType != null) break;
                }
                Log("AC", executeGgType != null ? "ExecuteGG: 存在" : "ExecuteGG: 未找到(可能已移除)");
            }
            catch (Exception e)
            {
                Log("ERROR", $"反作弊检查失败: {e.Message}");
            }
        }

        public static void LogEspRead(string playerName, Vector3 pos, float hp, float maxHp, int team)
        {
            Log("ESP", $"读取: {playerName} Pos=({pos.x:F1},{pos.y:F1},{pos.z:F1}) HP={hp}/{maxHp} Team={team}");
        }

        public static void LogAimbot(string targetName, float distance, string bone)
        {
            Log("AIM", $"锁定: {targetName} Dist={distance:F1}m Bone={bone}");
        }

        public static void LogError(string context, Exception e)
        {
            Log("ERROR", $"{context}: {e.GetType().Name} - {e.Message}");
        }

        public void DrawDebugPanel()
        {
            GUILayout.Label($"<color=cyan>=== Debug 日志 ({logs.Count}/{maxLogs}) ===</color>");

            if (GUILayout.Button("清除日志"))
            {
                logs.Clear();
                Log("System", "日志已清除");
            }

            if (GUILayout.Button("重新检查API"))
            {
                apiChecked = false;
                CheckApiCompatibility();
            }

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(400));
            for (int i = logs.Count - 1; i >= 0; i--)
            {
                string log = logs[i];
                if (log.Contains("[ERROR]"))
                    GUILayout.Label($"<color=red>{log}</color>");
                else if (log.Contains("[WARN]"))
                    GUILayout.Label($"<color=yellow>{log}</color>");
                else if (log.Contains("[API]"))
                    GUILayout.Label($"<color=lime>{log}</color>");
                else if (log.Contains("[AC]"))
                    GUILayout.Label($"<color=magenta>{log}</color>");
                else
                    GUILayout.Label($"<color=white>{log}</color>");
            }
            GUILayout.EndScrollView();
        }
    }
}
