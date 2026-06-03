using System;
using System.Collections;
using System.Reflection;
using Plugins.Hacks.Players;
using Plugins.Unity;
using Plugins.Utils;
using UnityEngine;

namespace Plugins.Hacks.Visuals
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class Esp : ModuleBase
    {
        public PlayerCollector collector => ModuleBase.GetPlugin<PlayerCollector>();

        public override void OnGUI()
        {
            if (!GameManager.IsGameActive)
            {
                return;
            }
            try
            {
                this.UpdateDraw();
            }
            catch (Exception e)
            {
                DebugModule.LogError("ESP.OnGUI", e);
            }
        }

        public override void Update()
        {
            if (GameManager.IsGameActive && Menu.Radar && Menu.Esp)
            {
                try
                {
                    Vector3 position = Camera.main.transform.position;
                float num2 = 167.25f;
                Vector2 vector = new Vector2(Screen.width, Screen.height) * 0.5f;
                Quaternion quaternion = Quaternion.AngleAxis(Contexts.sharedInstance.player.cameraOwnerEntity.orientation.Yaw, Vector3.back);
                GizmosPro.DrawCircle(new Circle(vector, num2), Color.gray * 0.1f);
                int team = Contexts.sharedInstance.player.myPlayerEntity.GetTeam();
                foreach (Player player in this.collector.players)
                {
                    if (player.IsValid && !player.IsDead && player.entity != Contexts.sharedInstance.player.cameraOwnerEntity && player.TeamId != team)
                    {
                        PlayerModel model = player.model;
                        Vector3 vector2 = model.root.position - position;
                        Vector2 vector3 = new Vector2(vector2.x, vector2.z);
                        vector3 = quaternion * vector3;
                        Vector2 vector4 = vector3 * Screen.height * 2.4E-07f * num2;
                        vector4 = Vector2.ClampMagnitude(vector4, num2 - 8f) + vector;
                        Quaternion quaternion2 = Quaternion.AngleAxis(player.entity.orientation.Yaw, Vector3.forward);
                        Vector3 vector5 = quaternion * quaternion2 * Vector3.up;
                        this.DrawRadarItem(vector4, vector5, Color.cyan);
                    }
                }
                }
                catch (Exception e)
                {
                    DebugModule.LogError("ESP.Radar", e);
                }
            }
        }

        private void DrawRadarItem(Vector2 uipos, Vector2 uidir, Color color)
        {
            uipos -= uidir * 3.5f;
            GizmosPro.DrawCircle(new Circle(uipos, 7f), color);
            Vector2 vector = new Vector2(-uidir.y, uidir.x);
            Vector2 vector2 = uipos + (uidir * 12f);
            GizmosPro.DrawLine(vector2, uipos, color);
            for (int i = 1; i <= 4; i++)
            {
                Vector2 vector3 = uipos + (vector * i);
                Vector2 vector4 = uipos + (vector * (float)-(float)i);
                GizmosPro.DrawLine(vector2, vector3, color);
                GizmosPro.DrawLine(vector2, vector4, color);
            }
        }
        private bool isMousePressed = false;
        private void UpdateDraw()
        {
            PlayerEntity cameraOwnerEntity = Contexts.sharedInstance.player.cameraOwnerEntity;
            int team = cameraOwnerEntity.GetTeam();
            foreach (Player player in this.collector.players)
            {
                if (player.IsValid && !player.IsDead && player.entity != cameraOwnerEntity && player.TeamId != team)
                {
                    if (Input.GetKey(KeyCode.Mouse0)) 
                    {
                        if (!isMousePressed)
                        {
                            isMousePressed = true;
                            StartCoroutine(DelayedMethod(player));
                        }
                    }
                    else
                    {
                        isMousePressed = false;
                    } 
     
                    if (Menu.Esp)
                    {
                        this.DrawPlayerEsp(player);
                    }
                }
            }
            if (Menu.ZX && Menu.Esp)
            {
                this.DrawCrosshair();
            }

            static IEnumerator DelayedMethod(Player player)
            {
                yield return new WaitForSeconds(0.1f); 

                if (Menu.Parse && Menu.ParseIndex == 0)
                {
                    AdjustPlayerView(player.entity);
                }
                else if (Menu.Parse && Menu.ParseIndex == 1)
                {
                    AdjustPlayerViewPitch(player.entity);
                }
            }
        }

        private void DrawPlayerEsp(Player player)
        {
            try
            {
                PlayerModel model = player.model;
                Rectangle rect = model.GetRect();

                if (rect.height <= 0f)
                {
                    return;
                }
                Color color = Aimbot.targetPlayer == player ? Color.red : Color.green;
                bool flag = true;
                this.DrawEspName(flag, rect, color, player);
                this.DrawEspDistance(flag, rect, color, model, player);
                this.DrawEspGL(flag, rect, color, model, player);
                this.DrawEspWeapon(flag, rect, color, player);

                DebugModule.LogEspRead(player.CleanName, model.root.position, player.Hp, player.HpMax, (int)player.TeamId);
            }
            catch (Exception e)
            {
                DebugModule.LogError("ESP.DrawPlayer", e);
            }
        }

        private static void AdjustPlayerViewPitch(PlayerEntity player)
        {
            float viewPitch = player.basicInfo.Current.ViewPitch;
            if (viewPitch < 34f || viewPitch == 89f)
            {
                player.basicInfo.Current.ViewPitch = Menu.AddIndex == 0 ? -89f : viewPitch + 90f;
                return;
            }
            if (viewPitch <= -34f && viewPitch != -89f)
            {
                player.basicInfo.Current.ViewPitch = Menu.AddIndex == 0 ? 89f : viewPitch - 90f;
            }
        }

        private static void AdjustPlayerView(PlayerEntity player)
        {
            float viewPitch = player.basicInfo.Current.ViewPitch;
            if (viewPitch >= 25f && viewPitch <= 180f || viewPitch == -287.027f)
            {
                player.basicInfo.Current.ViewPitch = Menu.AddIndex == 0 ? -91.1f : viewPitch + 90f;
                return;
            }
            if (viewPitch >= -180f && viewPitch <= -25f && viewPitch != -91.1f)
            {
                player.basicInfo.Current.ViewPitch = Menu.AddIndex == 0 ? -287.027f : viewPitch - 90f;
            }
        }

        private void DrawEspDistance(bool isValidRect, Rectangle rect, Color color, PlayerModel model, Player player)
        {
            if (!Menu.Dis || !isValidRect)
            {
                return;
            }
            float num = (model.root.position - Camera.main.transform.position).magnitude * 0.01f;
            Rect rect2 = this.ConvertToScreenRect(rect);
            rect2.y += rect2.height * 0.5f;
            rect2.x -= 100f;
            rect2.height = 20f;
            rect2.width = 200f;
            this.D_Label(rect2, string.Format(" {0}m", (int)num), color);
        }

        private void DrawEspName(bool isValidRect, Rectangle rect, Color color, Player player)
        {
            if (!Menu.Name || !isValidRect)
            {
                return;
            }
            Rect rect2 = this.ConvertToScreenRect(rect);
            rect2.y -= (rect2.height * 0.5f) + 20f;
            rect2.x -= 100f;
            rect2.height = 20f;
            rect2.width = 200f;
            this.D_Label(rect2, player.CleanName, color);
        }

        private void DrawEspWeapon(bool isValidRect, Rectangle rect, Color color, Player player)
        {
            if (!Menu.Weapon || !isValidRect)
            {
                return;
            }
            Rect rect2 = this.ConvertToScreenRect(rect);
            rect2.y -= (rect2.height * 0.5f) + 35f;
            rect2.x -= 100f;
            rect2.height = 20f;
            rect2.width = 200f;
            this.D_Label(rect2, player.Weapon + "[" + this.GetWeaponText(player.WeaponLevel) + "]", color);
        }

        private void DrawEspGL(bool isValidRect, Rectangle rect, Color color, PlayerModel model, Player player)
        {
            if (!isValidRect)
            {
                return;
            }
            if (Menu.box)
            {
                this.D_R_corner_3D(rect, color);
            }
            if (Menu.boneLine)
            {
                float num = Mathf.Min(Mathf.Min(rect.width, rect.height) * 0.23f, 10f);
                float num2 = (rect.Top - rect.Bottom) * 0.3f;
                GizmosPro.DrawCircle(new Circle(new Vector2(rect.Center.x, rect.Top - num), num2) * 0.37f, color);
                foreach (LineSegment lineSegment in model.GetBoneLines())
                {
                    this.D_L(lineSegment, color);
                }
            }
            if (Menu.AirLine)
            {
                Vector2 vector = new Vector2(rect.x, rect.Top);
                Vector2 vector2 = new Vector2(Screen.width * 0.5f, Screen.height);
                this.D_L(new LineSegment(vector, vector2), color);
            }
            if (Menu.Health)
            {
                this.DrawHpBar(rect, player.HpRatio);
            }
            if (Menu.C4 && player.HasC4)
            {
                rect.height = (rect.height * 2f) + 50f;
                DrawFilledTriangle(rect, color);
            }
        }

        private void DrawHpBar(Rectangle rect, float hpRatio)
        {
            float num = rect.height * hpRatio;
            float num2 = rect.Bottom + 1f;
            float num3 = rect.Right + 4f;
            Vector2 vector = new Vector2(num3, num2);
            Vector2 vector2 = new Vector2(num3, num2 + num);
            this.D_L(new LineSegment(vector, vector2), Color.green);
            this.D_L(new LineSegment(vector + Vector2.right, vector2 + Vector2.right), Color.green);
            this.D_L(new LineSegment(vector + (Vector2.right * 2f), vector2 + (Vector2.right * 2f)), Color.green);
            Rectangle rectangle = new Rectangle(num3 + 1f, rect.Center.y, 5f, rect.height);
            this.D_R(rectangle, Color.black * 0.7f);
        }

        private void DrawCrosshair()
        {
            Vector2 vector = new Vector2(Screen.width * 0.5f, Screen.height);
            Vector2 vector2 = new Vector2(Screen.width * 0.5f, 0f);
            this.D_L(new LineSegment(vector2, vector), Color.black);
            Vector2 vector3 = new Vector2(Screen.width, Screen.height * 0.5f);
            Vector2 vector4 = new Vector2((float)-(float)Screen.width * 0.5f, Screen.height * 0.5f);
            this.D_L(new LineSegment(vector3, vector4), Color.black);
        }

        private Rect ConvertToScreenRect(Rectangle rect)
        {
            rect.y = Screen.height - rect.y;
            return new Rect(rect.Center, rect.Size);
        }

        private string GetWeaponText(int weaponLevel)
        {
            return weaponLevel switch
            {
                1 => "主武器",
                2 => "副武器",
                3 => "近战武器",
                4 => "投掷物",
                5 => "C4",
                _ => string.Empty,
            };
        }

        private void D_L(LineSegment l, Color color)
        {
            this.D_L(l.Start, l.End, color);
        }

        private void D_L(Vector2 from, Vector2 to, Color color)
        {
            GizmosPro.DrawLine(from, to, color);
        }

        private void D_R_corner_3D(Rectangle r, Color color)
        {
            if (Menu.boxTypeIndex == 0)
            {
                this.DrawRectangleEdges(r, color);
                return;
            }
            if (Menu.boxTypeIndex == 1)
            {
                this.DrawRectangleCorners(r, color);
                return;
            }
            if (Menu.boxTypeIndex == 2)
            {
                //Quaternion rotation = Quaternion.identity;
                //GizmosPro.DrawCube(r.Center, r.Size, rotation, Color.blue);
                //return;
            }
        }

        public void DrawFilledTriangle(Rectangle rect, Color color, bool position = false, float triangleHeight = 15f)
        {
            float apexY = position ? rect.Top - triangleHeight : rect.Bottom + triangleHeight;
            float baseY = position ? rect.Top : rect.Bottom;
            Vector2 apex = new Vector2((rect.Left + rect.Right) / 2, apexY);
            Vector2 leftBase = new Vector2(rect.Left, baseY);
            Vector2 rightBase = new Vector2(rect.Right, baseY);
            Vector2[] vertices = new Vector2[] { apex, leftBase, rightBase };
            GizmosPro.DrawTriangle(vertices, color);
        }

        private void DrawRectangleEdges(Rectangle r, Color color)
        {
            this.D_L(new Vector2(r.Left, r.Top), new Vector2(r.Right, r.Top), color);
            this.D_L(new Vector2(r.Left, r.Top), new Vector2(r.Left, r.Bottom), color);
            this.D_L(new Vector2(r.Right, r.Top), new Vector2(r.Right, r.Bottom), color);
            this.D_L(new Vector2(r.Left, r.Bottom), new Vector2(r.Right, r.Bottom), color);
        }

        private void DrawRectangleCorners(Rectangle r, Color color)
        {
            float num = Mathf.Min(Mathf.Min(r.width, r.height) * 0.3f, 10f);
            this.D_L(new Vector2(r.Left, r.Top), new Vector2(r.Left, r.Top - num), color);
            this.D_L(new Vector2(r.Left, r.Top), new Vector2(r.Left + num, r.Top), color);
            this.D_L(new Vector2(r.Right, r.Top), new Vector2(r.Right, r.Top - num), color);
            this.D_L(new Vector2(r.Right, r.Top), new Vector2(r.Right - num, r.Top), color);
            this.D_L(new Vector2(r.Left, r.Bottom), new Vector2(r.Left, r.Bottom + num), color);
            this.D_L(new Vector2(r.Left, r.Bottom), new Vector2(r.Left + num, r.Bottom), color);
            this.D_L(new Vector2(r.Right, r.Bottom), new Vector2(r.Right, r.Bottom + num), color);
            this.D_L(new Vector2(r.Right, r.Bottom), new Vector2(r.Right - num, r.Bottom), color);
        }

        private void D_R(Rectangle rect, Color color)
        {
            GizmosPro.DrawRect(rect, color);
        }

        private void D_Label(Rect rect, string text, Color color)
        {
            Color contentColor = GUI.contentColor;
            TextAnchor alignment = GUI.skin.label.alignment;
            GUI.contentColor = Color.black;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            GUI.Label(rect, text);
            rect.position -= Vector2.one;
            GUI.contentColor = color;
            GUI.Label(rect, text);
            GUI.contentColor = contentColor;
            GUI.skin.label.alignment = alignment;
        }

        public Color[] colors = new Color[]
{
            Color.red,
            new Color(1f, 0.5f, 0f),
            Color.yellow,
            Color.green,
            Color.blue,
            new Color(0.29f, 0f, 0.51f),
            Color.magenta
};

        private int currentColorIndex;
        private float t;
        public static Color clr;
    }
}