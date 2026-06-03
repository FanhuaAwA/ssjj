using System.Collections.Generic;
using SSJJPlugin.Utils;
using UnityEngine;

namespace SSJJPlugin.Systems
{
    public class AimbotSystem
    {
        public bool Enabled;
        public object TargetPlayer;
        private Camera _cam;
        private static readonly string[] BodyNames = { "Head", "Chest", "Abdomen", "Nearest" };

        public string GetBodyName(int idx) => idx < BodyNames.Length ? BodyNames[idx] : "Head";

        /// <summary>
        /// Compute aimbot state in Update() - no GUI calls
        /// </summary>
        public void Compute(List<PlayerData> players, Camera cam, KeyCode aimKey,
            float speed, float fov, int bodyIndex, bool recoil,
            bool showFov, bool showLine,
            out bool hasTarget, out Vector2 targetScreen, out string targetName, out float targetDist,
            out Vector2 fovCenter, out float fovRadius)
        {
            _cam = cam;
            fovCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            fovRadius = GetFovRadius(fov);
            hasTarget = false;
            targetScreen = Vector2.zero;
            targetName = "";
            targetDist = 0;

            if (_cam == null || !Enabled || players.Count == 0) return;

            Vector2 center = fovCenter;
            TargetPlayer = null;
            float bestDist = float.MaxValue;
            Vector3 bestWorld = Vector3.zero;

            foreach (var p in players)
            {
                try
                {
                    Vector3 aimWorld = CalcAimPos(p, bodyIndex);
                    if (aimWorld == Vector3.zero) continue;

                    var sp = _cam.WorldToScreenPoint(aimWorld);
                    if (sp.z <= 0) continue;

                    var screenP = new Vector2(sp.x, Screen.height - sp.y);
                    float dist = Vector2.Distance(center, screenP);

                    if (dist < fovRadius && dist < bestDist)
                    {
                        bestDist = dist;
                        targetScreen = screenP;
                        bestWorld = aimWorld;
                        TargetPlayer = p.Entity;
                        targetName = p.Name;
                        targetDist = Vector3.Distance(_cam.transform.position, aimWorld) * 0.01f;
                    }
                }
                catch { }
            }

            if (TargetPlayer != null)
            {
                hasTarget = true;

                if (aimKey == KeyCode.None || Input.GetKey(aimKey))
                {
                    ApplyAim(bestWorld, speed, recoil);
                }
            }
        }

        private float GetFovRadius(float fovDeg)
        {
            float screenFov = _cam != null ? _cam.fieldOfView : 90f;
            float ratio = Mathf.Tan(fovDeg * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(screenFov * 0.5f * Mathf.Deg2Rad);
            return ratio * Screen.height * 0.5f;
        }

        private Vector3 CalcAimPos(PlayerData p, int bodyIndex)
        {
            try
            {
                Transform root = p.Root;
                Transform head = p.Head;
                Transform spine = p.Spine;
                Transform pelvis = p.Pelvis;
                Vector3 pos = p.Position;
                Vector3 headPos = p.HeadPosition;

                // If head bone found, use it
                if (head != null && bodyIndex == 0)
                    return head.position;

                // If spine found, use for chest
                if (spine != null && bodyIndex == 1)
                    return spine.position;

                // If pelvis found, use for abdomen
                if (pelvis != null && bodyIndex == 2)
                    return pelvis.position;

                // Fallback: calculate from distance
                float dist = Vector3.Distance(_cam.transform.position, pos);
                float playerHeight = 180f; // SSJJ units

                if (bodyIndex == 0) // Head - 90% up
                    return pos + new Vector3(0, playerHeight * 0.9f, 0);
                else if (bodyIndex == 1) // Chest - 70% up
                    return pos + new Vector3(0, playerHeight * 0.7f, 0);
                else if (bodyIndex == 2) // Abdomen - 50% up
                    return pos + new Vector3(0, playerHeight * 0.5f, 0);
                else // Nearest
                    return GetNearestBone(root, pos, headPos);
            }
            catch { return Vector3.zero; }
        }

        private Vector3 GetNearestBone(Transform root, Vector3 fallback, Vector3 headFallback)
        {
            if (root == null) return headFallback;
            var center = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            float minDist = float.MaxValue;
            Vector3 best = Vector3.zero;

            string[] bones = {"Bip01_Head","Bip01_Neck","Bip01_Spine","Bip01_Pelvis",
                "Bip01_L_Clavicle","Bip01_R_Clavicle","Bip01_L_UpperArm","Bip01_R_UpperArm",
                "Bip01_L_Forearm","Bip01_R_Forearm","Bip01_L_Hand","Bip01_R_Hand",
                "Bip01_L_Thigh","Bip01_R_Thigh","Bip01_L_Calf","Bip01_R_Calf",
                "Bip01_L_Foot","Bip01_R_Foot"};

            foreach (var bn in bones)
            {
                var t = FindBone(root, bn);
                if (t == null) continue;
                var sp = _cam.WorldToScreenPoint(t.position);
                if (sp.z <= 0) continue;
                float d = Vector2.Distance(center, new Vector2(sp.x, Screen.height - sp.y));
                if (d < minDist) { minDist = d; best = t.position; }
            }
            return best != Vector3.zero ? best : headFallback;
        }

        private void ApplyAim(Vector3 target, float speed, bool recoil)
        {
            try
            {
                var ctxType = ReflectionHelper.FindType("Contexts");
                var contexts = ctxType != null ? ReflectionHelper.GetStatic(ctxType, "sharedInstance") : null;
                if (contexts == null) return;
                var ucCtx = ReflectionHelper.GetInstance(contexts, "userCommand");
                if (ucCtx == null) return;
                var input = ReflectionHelper.GetInstance(ucCtx, "input");
                if (input == null) return;

                Vector3 dir = (target - _cam.transform.position).normalized;
                var euler = Quaternion.FromToRotation(dir, Vector3.forward).eulerAngles;
                if (euler.x > 180f) euler.x -= 360f;

                if (recoil)
                {
                    var pl = ReflectionHelper.GetInstance(contexts, "player");
                    var my = pl != null ? ReflectionHelper.GetInstance(pl, "myPlayerEntity") : null;
                    if (my != null)
                    {
                        float py = ReflectionHelper.GetFloat(my, "_punchYaw");
                        float pp = ReflectionHelper.GetFloat(my, "_punchPitch");
                        euler.y -= py * 2f;
                        euler.x -= pp * 2f;
                    }
                }

                float curYaw = System.Convert.ToSingle(ReflectionHelper.GetInstance(input, "Yaw"));
                float curPitch = System.Convert.ToSingle(ReflectionHelper.GetInstance(input, "Pitch"));
                float t = speed / 100f;

                ReflectionHelper.SetField(input, "Yaw", Mathf.LerpAngle(curYaw, euler.y, t));
                ReflectionHelper.SetField(input, "Pitch", Mathf.Lerp(curPitch, euler.x, t));
            }
            catch (System.Exception e) { DebugSystem.LogError("ApplyAim", e); }
        }

        private Transform FindBone(Transform root, string name)
        {
            if (root == null) return null;
            var t = root.Find(name);
            if (t != null) return t;
            for (int i = 0; i < root.childCount; i++) { t = root.GetChild(i).Find(name); if (t != null) return t; }
            return null;
        }
    }
}
