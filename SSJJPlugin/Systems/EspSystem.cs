using System.Collections.Generic;
using SSJJPlugin.Utils;
using UnityEngine;

namespace SSJJPlugin.Systems
{
    public class EspSystem
    {
        public bool Enabled = true;
        public bool ShowBox = true;
        public bool ShowHealth = true;
        public bool ShowName = true;
        public bool ShowDistance = true;
        public bool ShowBone;
        public bool ShowLine;
        public bool ShowFov;
        public bool ShowAimLine;
        public bool RecoilControl;
        public bool VisibilityCheck;
        public bool IgnoreInvincible;
        public int BoxType;
        public float Speed = 100f;
        public float Fov = 15f;
        public int BodyIndex;

        public void Draw(List<PlayerData> players, Camera cam)
        {
            if (!Enabled || cam == null || players.Count == 0) return;

            foreach (var p in players)
            {
                try { DrawPlayer(p, cam); }
                catch { }
            }
            GUI.color = Color.white;
        }

        private void DrawPlayer(PlayerData p, Camera cam)
        {
            Vector3 pos = p.Position;
            Vector3 headPos = p.HeadPosition;

            var sp = cam.WorldToScreenPoint(pos);
            var sh = cam.WorldToScreenPoint(headPos);

            // Must be in front of camera
            if (sp.z <= 0 || sh.z <= 0) return;

            float feetY = Screen.height - sp.y;
            float headY = Screen.height - sh.y;
            float h = feetY - headY;

            // If head bone not found, calculate from distance
            if (h < 3f)
            {
                float dist = sp.z;
                float playerWorldHeight = 180f;
                h = playerWorldHeight * Screen.height / (2f * dist * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));
                headY = feetY - h;
            }

            if (h < 3f) return;

            float w = h * 0.4f;
            float x = sp.x - w * 0.5f;

            // Skip if off screen
            if (x > Screen.width + 100 || x + w < -100 || headY > Screen.height + 100 || feetY < -100) return;

            Color color = Color.green;

            if (ShowBox)
            {
                if (BoxType == 0) DrawingHelper.DrawBox(x, headY, w, h, color);
                else DrawingHelper.DrawCornerBox(x, headY, w, h, color);
            }

            if (ShowHealth && p.MaxHP > 0)
            {
                float ratio = Mathf.Clamp01(p.HP / p.MaxHP);
                float barH = h * ratio;
                DrawingHelper.DrawFilledRect(new Rect(x - 6, headY, 3, h), Color.black);
                Color hpColor = ratio > 0.5f ? Color.green : ratio > 0.25f ? Color.yellow : Color.red;
                DrawingHelper.DrawFilledRect(new Rect(x - 6, feetY - barH, 3, barH), hpColor);
            }

            if (ShowName)
                DrawingHelper.DrawLabelCentered(new Rect(x - 50, headY - 18, w + 100, 18), p.Name, color);

            if (ShowDistance)
            {
                float dist = sp.z * 0.01f;
                DrawingHelper.DrawLabelCentered(new Rect(x - 50, feetY + 2, w + 100, 18), $"{(int)dist}m", Color.cyan);
            }

            if (ShowLine)
                DrawingHelper.DrawLine(new Vector2(Screen.width * 0.5f, Screen.height), new Vector2(sp.x, feetY), color);

            if (ShowBone && p.Root != null)
                DrawBones(p.Root, cam, color);
        }

        private void DrawBones(Transform root, Camera cam, Color color)
        {
            string[][] pairs = {
                new[]{"Bip01_Pelvis","Bip01_Spine"}, new[]{"Bip01_Spine","Bip01_Neck"},
                new[]{"Bip01_Neck","Bip01_L_Clavicle"}, new[]{"Bip01_Neck","Bip01_R_Clavicle"},
                new[]{"Bip01_L_Clavicle","Bip01_L_UpperArm"}, new[]{"Bip01_R_Clavicle","Bip01_R_UpperArm"},
                new[]{"Bip01_L_UpperArm","Bip01_L_Forearm"}, new[]{"Bip01_R_UpperArm","Bip01_R_Forearm"},
                new[]{"Bip01_L_Forearm","Bip01_L_Hand"}, new[]{"Bip01_R_Forearm","Bip01_R_Hand"},
                new[]{"Bip01_Pelvis","Bip01_L_Thigh"}, new[]{"Bip01_Pelvis","Bip01_R_Thigh"},
                new[]{"Bip01_L_Thigh","Bip01_L_Calf"}, new[]{"Bip01_R_Thigh","Bip01_R_Calf"},
                new[]{"Bip01_L_Calf","Bip01_L_Foot"}, new[]{"Bip01_R_Calf","Bip01_R_Foot"},
            };
            foreach (var pair in pairs)
            {
                var t1 = FindBone(root, pair[0]);
                var t2 = FindBone(root, pair[1]);
                if (t1 != null && t2 != null)
                {
                    var s1 = cam.WorldToScreenPoint(t1.position);
                    var s2 = cam.WorldToScreenPoint(t2.position);
                    if (s1.z > 0 && s2.z > 0)
                        DrawingHelper.DrawLine(new Vector2(s1.x, Screen.height - s1.y), new Vector2(s2.x, Screen.height - s2.y), color);
                }
            }
        }

        private Transform FindBone(Transform root, string name)
        {
            if (root == null) return null;
            var t = root.Find(name);
            if (t != null) return t;
            return FindBoneRecursive(root, name);
        }

        private Transform FindBoneRecursive(Transform parent, string name)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
                var found = FindBoneRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }
    }
}
