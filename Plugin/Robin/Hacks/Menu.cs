using System;
using System.IO;
using System.Reflection;
using Plugins.Hacks;
using Plugins.Hooks;
using Plugins.Unity;
using UnityEngine;

[Obfuscation(Feature = "Virtualization", Exclude = false)]
public class Menu : ModuleBase
{
    public override void OnGUI()
    {
        if (Menu.show)
        {
            if (show)
            {
                GUILayout.BeginArea(new Rect(10f, 10f, 520f, 750f));
                GUI.Box(new Rect(0f, 0f, 520f, 750f), "");
                GUILayout.BeginVertical();

                GUILayout.Label($"V2.0.0 [{menuToggleKey} - Show / Hide Menu]");
                RenderTabs();
                RenderSelectedTab();

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }
        }
    }

    private void RenderSelectedTab()
    {
        switch (selectedTab)
        {
            case 0: ShowEspTab(); break;
            case 1: ShowAimbotTab(); break;
            case 2: ShowExtTab(); break;
            case 3: ShowRageTab(); break;
            case 4: ShowDebugTab(); break;
        }
    }

    private void RenderTabs()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Esp"))
        {
            selectedTab = 0;
        }

        if (GUILayout.Button("Aim"))
        {
            selectedTab = 1;
        }

        if (GUILayout.Button("Ext"))
        {
            selectedTab = 2;
        }

        if (GUILayout.Button("Rage"))
        {
            selectedTab = 3;
        }

        if (GUILayout.Button("Debug"))
        {
            selectedTab = 4;
        }

        GUILayout.EndHorizontal();
    }

    private void ShowEspTab()
    {
        RenderToggle("透视开关", ref Esp);
        RenderToggle("显示矩形", ref box);
        RenderToggle("显示射线", ref AirLine);
        RenderToggle("显示骨骼", ref boneLine);
        RenderToggle("显示血量", ref Health);
        RenderToggle("显示名称", ref Name);
        RenderToggle("显示武器", ref Weapon);
        RenderToggle("显示距离", ref Dis);
        RenderToggle("显示雷达", ref Radar);

        RenderToggle("人物发光", ref Glow);

        RenderToggle("C4箭头", ref C4);

        RenderToggle("全屏准星", ref ZX);

        RenderSlider("线宽", ref Ness, 0.1f, 3f);

        RenderSlider("覆盖", ref FillAmount, 0f, 5f);

        RenderButtonCycle("方框类型", ref boxTypeIndex, boxTyoe);
        RenderButtonCycle("颜色风格", ref GlowIndex, GlowTyoe);
    }

    private void ShowAimbotTab()
    {
        RenderToggle("自瞄开关", ref Aim);
        RenderToggle("屏蔽刺刀", ref NoKnife);
        RenderToggle("预瞄射线", ref AimLine);
        RenderToggle("平滑无后", ref RecoilControl);
        RenderToggle("可视检查", ref Vis);
        RenderToggle("显示范围", ref AimRange);
        RenderToggle("无敌不锁", ref CheckInvinciblePlayer);

        RenderButtonCycle("自瞄类型", ref AimIndex, AimType);
        RenderSlider("自瞄速度", ref Speed, 1, 2000);
        RenderSlider("FOV范围", ref aimFov, 1f, 90f);
        RenderButtonCycle("自瞄部位", ref BodyIndex, BodyType);

        GUILayout.BeginHorizontal();
        if (waitingForAimKey)
        {
            GUILayout.Label("<color=yellow>请按下任意按键绑定...</color>");
        }
        else
        {
            GUILayout.Label($"自瞄热键: <color=cyan>{aimKeyName}</color>");
        }
        if (GUILayout.Button("设置", GUILayout.Width(60)))
        {
            waitingForAimKey = true;
        }
        if (GUILayout.Button("始终", GUILayout.Width(60)))
        {
            aimKey = KeyCode.None;
            aimKeyName = "始终开启";
            waitingForAimKey = false;
        }
        GUILayout.EndHorizontal();
    }

    private void ShowExtTab()
    {
        RenderToggle("自动开火", ref AutoFire);
        RenderToggle("右键瞬狙", ref Sniper);
        RenderToggle("观察模式", ref Observer);
        RenderToggle("纠正视角", ref Parse);
        RenderToggle("自动举报", ref Report);
        RenderToggle("C4掉落位置", ref c4Position);

        RenderButtonCycle("纠正算法", ref ParseIndex, ParseType);
        RenderButtonCycle("纠正大小", ref AddIndex, AddType);

        RenderButtonCycle("菜单按键", ref menuKeyIndex, menuKeyNames);

        RenderTextField("配置路径", ref path);

        if (GUILayout.Button("保存配置"))
        {
            SaveConfig();
        }

        if (GUILayout.Button("读取配置"))
        {
            LoadConfig();
        }

        if (GUILayout.Button("清除配置"))
        {
            ClearConfig();
        }
    }

    private void ShowRageTab()
    {
        RenderToggle("空格连跳", ref BunnyHop);
        RenderToggle("静默自瞄", ref Sil);
        RenderToggle("反自瞄", ref Anti);
        RenderToggle("假卡", ref FakeLag);
        RenderToggle("随机", ref rdm);
        RenderToggle("无抖动", ref NoShake);
        RenderToggle("第三人称-V", ref Act);

        GUILayout.BeginHorizontal();
        RenderTextField("延迟(0 / 300)", ref Tick);
        RenderTextField("命中率(0 / 100)", ref HitRate);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        RenderTextField("旋转速度(1 / 999)", ref Rpm);
        RenderTextField("最小抖动(-180 / 180)", ref Min);
        RenderTextField("最大抖动(-180 / 180)", ref Max);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        RenderTextField("异常头轴X - ZXC", ref HeadPitch);
        RenderTextField("异常头轴Y - 789", ref HeadYaw);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        RenderButtonCycle("静默类型", ref SilentIndex, SilentType);
        RenderButtonCycle("反自瞄类型", ref AntiIndex, AntiType);
        RenderButtonCycle("开火模式", ref FireIndex, FireType);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        RenderTextField("预设Z", ref Z);
        RenderTextField("预设X", ref X);
        RenderTextField("预设C", ref C);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        RenderTextField("预设7", ref Up);
        RenderTextField("预设8", ref ForWard);
        RenderTextField("预设9", ref Down);
        GUILayout.EndHorizontal();

        RenderBoneToggles();

        if (GUILayout.Button("设定部位"))
        {
            SilentAim.GetBoneHashes();
        }
    }

    private void ShowDebugTab()
    {
        DebugModule debugModule = GetPlugin<DebugModule>();
        if (debugModule != null)
        {
            debugModule.DrawDebugPanel();
        }
        else
        {
            GUILayout.Label("<color=yellow>DebugModule 未加载</color>");
            if (GUILayout.Button("重新初始化"))
            {
                DebugModule.Log("System", "手动重新初始化请求");
            }
        }
    }

    private void RenderBoneToggles()
    {
        GUILayout.BeginHorizontal();
        RenderToggle("头心", ref Head);
        RenderToggle("头顶", ref HeadNub);
        RenderToggle("脖子", ref Neck);
        RenderToggle("腹部", ref Spine);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        RenderToggle("锁骨", ref Clavicle);
        RenderToggle("上臂", ref UpperArm);
        RenderToggle("前臂", ref Forearm);
        RenderToggle("手臂", ref Hand);
        RenderToggle("手指", ref Finger);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        RenderToggle("骨盆", ref Pelvis);
        RenderToggle("大腿", ref Thigh);
        RenderToggle("小腿", ref Calf);
        RenderToggle("脚", ref Foot);
        RenderToggle("脚趾", ref Toe);
        GUILayout.EndHorizontal();
    }

    private void RenderToggle(string label, ref bool value)
    {
        value = GUILayout.Toggle(value, label);
    }

    private void RenderSlider(string label, ref int value, int min, int max)
    {
        GUILayout.Label($"{label}: {value}");
        value = (int)GUILayout.HorizontalSlider(value, min, max);
    }

    private void RenderSlider(string label, ref float value, float min, float max)
    {
        GUILayout.Label($"{label}: {value}");
        value = GUILayout.HorizontalSlider(value, min, max);
    }

    private void RenderButtonCycle(string label, ref int index, string[] options)
    {
        if (GUILayout.Button($"{label}: {options[index]}"))
        {
            index = (index + 1) % options.Length;
        }
    }

    private void RenderTextField(string label, ref string value)
    {
        GUILayout.Label(label);
        value = GUILayout.TextField(value);
    }

    private void RenderTextField(string label, ref float value)
    {
        GUILayout.Label(label);
        string input = GUILayout.TextField(value.ToString());
        if (float.TryParse(input, out float newValue))
        {
            value = newValue;
        }
    }

    private void RenderTextField(string label, ref int value)
    {
        GUILayout.Label(label);
        string input = GUILayout.TextField(value.ToString());
        if (int.TryParse(input, out int newValue))
        {
            value = newValue;
        }
    }

    public void SaveConfig()
    {
        try
        {
            using StreamWriter writer = new StreamWriter(path);
            writer.WriteLine($"Esp={Esp}");
            writer.WriteLine($"box={box}");
            writer.WriteLine($"AirLine={AirLine}");
            writer.WriteLine($"boneLine={boneLine}");
            writer.WriteLine($"Health={Health}");
            writer.WriteLine($"Name={Name}");
            writer.WriteLine($"Weapon={Weapon}");
            writer.WriteLine($"Dis={Dis}");
            writer.WriteLine($"Radar={Radar}");
            writer.WriteLine($"Glow={Glow}");
            writer.WriteLine($"ZX={ZX}");
            writer.WriteLine($"C4={C4}");
            writer.WriteLine($"Ness={Ness}");
            writer.WriteLine($"FillAmount={FillAmount}");
            writer.WriteLine($"boxTypeIndex={boxTypeIndex}");
            writer.WriteLine($"GlowIndex={GlowIndex}");
            writer.WriteLine($"Aim={Aim}");
            writer.WriteLine($"NoKnife={NoKnife}");
            writer.WriteLine($"AimLine={AimLine}");
            writer.WriteLine($"RecoilControl={RecoilControl}");
            writer.WriteLine($"Vis={Vis}");
            writer.WriteLine($"AimRange={AimRange}");
            writer.WriteLine($"AimIndex={AimIndex}");
            writer.WriteLine($"Speed={Speed}");
            writer.WriteLine($"Range={Range}");
            writer.WriteLine($"BodyIndex={BodyIndex}");
            writer.WriteLine($"KeyIndex={KeyIndex}");
            writer.WriteLine($"CheckInfanticPlayer={CheckInvinciblePlayer}");
            writer.WriteLine($"AutoFire={AutoFire}");
            writer.WriteLine($"Sniper={Sniper}");
            writer.WriteLine($"Observer={Observer}");
            writer.WriteLine($"Parse={Parse}");
            writer.WriteLine($"ParseIndex={ParseIndex}");
            writer.WriteLine($"Report={Report}");
            writer.WriteLine($"c4Position={c4Position}");
            writer.WriteLine($"BunnyHop={BunnyHop}");
            writer.WriteLine($"Sil={Sil}");
            writer.WriteLine($"Anti={Anti}");
            writer.WriteLine($"Packet={FakeLag}");
            writer.WriteLine($"RTick={rdm}");
            writer.WriteLine($"NoShake={NoShake}");
            writer.WriteLine($"Act={Act}");
            writer.WriteLine($"Tick={Tick}");
            writer.WriteLine($"Rpm={Rpm}");
            writer.WriteLine($"HitRate={HitRate}");
            writer.WriteLine($"Min={Min}");
            writer.WriteLine($"Max={Max}");
            writer.WriteLine($"HeadPitch={HeadPitch}");
            writer.WriteLine($"HeadYaw={HeadYaw}");
            writer.WriteLine($"SilentIndex={SilentIndex}");
            writer.WriteLine($"AntiIndex={AntiIndex}");
            writer.WriteLine($"FireIndex={FireIndex}");
            writer.WriteLine($"Head={Head}");
            writer.WriteLine($"HeadNub={HeadNub}");
            writer.WriteLine($"Neck={Neck}");
            writer.WriteLine($"Spine={Spine}");
            writer.WriteLine($"Clavicle={Clavicle}");
            writer.WriteLine($"UpperArm={UpperArm}");
            writer.WriteLine($"Forearm={Forearm}");
            writer.WriteLine($"Hand={Hand}");
            writer.WriteLine($"Finger={Finger}");
            writer.WriteLine($"Pelvis={Pelvis}");
            writer.WriteLine($"Thigh={Thigh}");
            writer.WriteLine($"Calf={Calf}");
            writer.WriteLine($"Foot={Foot}");
            writer.WriteLine($"Toe={Toe}");

            writer.WriteLine($"Z={Z}");
            writer.WriteLine($"X={X}");
            writer.WriteLine($"C={C}");
            writer.WriteLine($"Up={Up}");
            writer.WriteLine($"ForWard={ForWard}");
            writer.WriteLine($"Down={Down}");

            writer.WriteLine($"AddIndex={AddIndex}");
            writer.WriteLine($"menuKeyIndex={menuKeyIndex}");
            writer.WriteLine($"aimFov={aimFov}");
            writer.WriteLine($"aimKeyName={aimKeyName}");
        }
        catch
        {
        }
    }

    public void LoadConfig()
    {
        try
        {
            if (File.Exists(path))
            {
                string[] lines = File.ReadAllLines(path);
                foreach (string line in lines)
                {
                    string[] parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        string fieldName = parts[0].Trim();
                        string fieldValue = parts[1].Trim();

                        if (fieldName == "aimKeyName")
                        {
                            aimKeyName = fieldValue;
                            try { aimKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), fieldValue); }
                            catch { aimKey = KeyCode.None; }
                            continue;
                        }

                        FieldInfo fieldInfo = typeof(Menu).GetField(fieldName, BindingFlags.Public | BindingFlags.Static);
                        if (fieldInfo != null)
                        {
                            object value = Convert.ChangeType(fieldValue, fieldInfo.FieldType);
                            fieldInfo.SetValue(null, value);
                        }
                    }
                }
            }
        }
        catch
        {
        }
    }

    public void ClearConfig()
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }

    private static string path = Path.Combine(Application.persistentDataPath, "config.txt");
    private int selectedTab;
    public static bool Esp;
    public static bool show = true;
    private readonly string[] boxTyoe = new string[] { "完整", "角框" };
    public static int boxTypeIndex = 0;
    private readonly string[] GlowTyoe = new string[] { "鲜艳", "暗淡" };
    public static int GlowIndex = 0;
    private readonly string[] AimType = new string[] { "角度", "平滑" };
    public static int AimIndex = 0;
    public static float Ness = 0.7f;
    public static int Speed = 50;
    public static float aimFov = 15f;
    public static readonly string[] BodyType = new string[] { "头", "胸", "腹", "最近" };
    public static int BodyIndex = 0;
    public static KeyCode aimKey = KeyCode.Mouse0;
    public static string aimKeyName = "Mouse0";
    public static bool waitingForAimKey = false;
    public static int KeyIndex = 0;
    public static KeyCode menuToggleKey = KeyCode.Home;
    private static readonly string[] menuKeyNames = new string[] { "Home", "Insert", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12", "Delete", "End", "PageUp", "PageDown" };
    private static readonly KeyCode[] menuKeyCodes = new KeyCode[] { KeyCode.Home, KeyCode.Insert, KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4, KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9, KeyCode.F10, KeyCode.F11, KeyCode.F12, KeyCode.Delete, KeyCode.End, KeyCode.PageUp, KeyCode.PageDown };
    public static int menuKeyIndex = 0;
    public static int Tick;
    public static int Rpm;
    public static int HitRate;
    public static float Min;
    public static float Max;
    public static float HeadPitch;
    public static float HeadYaw;
    private readonly string[] SilentType = new string[] { "正常", "力量" };
    public static int SilentIndex = 0;
    private readonly string[] AntiType = new string[] { "静态", "旋转", "抖动"};
    public static int AntiIndex = 0;
    private readonly string[] FireType = new string[] { "手动", "自动" };
    public static int FireIndex = 0;
    public static bool box;
    public static bool AirLine;
    public static bool boneLine;
    public static bool Health;
    public static bool Name;
    public static bool Weapon;
    public static bool Dis;
    public static bool Radar;
    public static bool Glow;
    public static bool ZX;
    public static bool C4;
    public static bool Aim;
    public static bool NoKnife;
    public static bool RecoilControl;
    public static bool Vis;
    public static bool CheckInvinciblePlayer;
    public static bool AutoFire;
    public static bool Sniper;
    public static bool Observer;
    public static bool BunnyHop;
    public static bool Sil;
    public static bool Anti;
    public static bool FakeLag;
    public static bool rdm;
    public static bool NoShake;
    public static bool Act;
    public static float FillAmount;
    public static bool AimRange;
    public static bool AimLine;
    public static int Range;
    public static bool Parse;
    private readonly string[] ParseType = new string[] { "1", "2" };
    public static int ParseIndex = 0;

    private readonly string[] AddType = new string[] { "默认", "一半" };
    public static int AddIndex = 0;
    public static bool Report;
    public static bool c4Position;
    public static bool Head;
    public static bool HeadNub;
    public static bool Neck;
    public static bool Spine;
    public static bool Clavicle;
    public static bool UpperArm;
    public static bool Forearm;
    public static bool Hand;
    public static bool Finger;
    public static bool Pelvis;
    public static bool Thigh;
    public static bool Calf;
    public static bool Foot;
    public static bool Toe;
    public static float Z = -89.9f;
    public static float X = 179.9f;
    public static float C = 89.9f;
    public static float Up = -271f;
    public static float ForWard = 0f;
    public static float Down = 271f;
}