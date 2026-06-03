using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Components
{
    public class CameraController
    {
        public bool Enabled;
        public object TargetPlayer;
        private Camera _cam;

        public void Compute(List<EntityInfo> entities, Camera cam, KeyCode aimKey,
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

            if (_cam == null || !Enabled || entities.Count == 0) return;

            var center = fovCenter;
            TargetPlayer = null;
            float bestDist = float.MaxValue;
            Vector3 bestWorld = Vector3.zero;

            foreach (var p in entities)
            {
                try
                {
                    var aimWorld = CalcAimPos(p, bodyIndex);
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
                    ApplyAim(bestWorld, speed, recoil);
            }
        }

        private float GetFovRadius(float fovDeg)
        {
            float sf = _cam != null ? _cam.fieldOfView : 90f;
            float r = Mathf.Tan(fovDeg * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(sf * 0.5f * Mathf.Deg2Rad);
            return r * Screen.height * 0.5f;
        }

        private Vector3 CalcAimPos(EntityInfo p, int bodyIndex)
        {
            try
            {
                if (p.Head != null && bodyIndex == 0) return p.Head.position;
                if (p.Spine != null && bodyIndex == 1) return p.Spine.position;
                if (p.Pelvis != null && bodyIndex == 2) return p.Pelvis.position;

                float ph = 180f;
                if (bodyIndex == 0) return p.Position + new Vector3(0, ph * 0.9f, 0);
                if (bodyIndex == 1) return p.Position + new Vector3(0, ph * 0.7f, 0);
                if (bodyIndex == 2) return p.Position + new Vector3(0, ph * 0.5f, 0);

                return GetNearestBone(p.Root, p.Position, p.HeadPosition);
            }
            catch { return Vector3.zero; }
        }

        private Vector3 GetNearestBone(Transform root, Vector3 fb, Vector3 hf)
        {
            if (root == null) return hf;
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
                var t = FB(root, bn); if (t == null) continue;
                var sp = _cam.WorldToScreenPoint(t.position); if (sp.z <= 0) continue;
                float d = Vector2.Distance(center, new Vector2(sp.x, Screen.height - sp.y));
                if (d < minDist) { minDist = d; best = t.position; }
            }
            return best != Vector3.zero ? best : hf;
        }

        private void ApplyAim(Vector3 target, float speed, bool recoil)
        {
            try
            {
                var ct = ComponentManager.FT("Contexts");
                var cx = ct != null ? ComponentManager.GS(ct, "sharedInstance") : null;
                if (cx == null) return;
                var uc = ComponentManager.GI(cx, "userCommand");
                if (uc == null) return;
                var input = ComponentManager.GI(uc, "input");
                if (input == null) return;

                Vector3 dir = (target - _cam.transform.position).normalized;
                var euler = Quaternion.FromToRotation(dir, Vector3.forward).eulerAngles;
                if (euler.x > 180f) euler.x -= 360f;

                if (recoil)
                {
                    var pl = ComponentManager.GI(cx, "player");
                    var my = pl != null ? ComponentManager.GI(pl, "myPlayerEntity") : null;
                    if (my != null)
                    {
                        float py = ComponentManager.GF(my, "_punchYaw");
                        float pp = ComponentManager.GF(my, "_punchPitch");
                        euler.y -= py * 2f; euler.x -= pp * 2f;
                    }
                }

                float cy = System.Convert.ToSingle(ComponentManager.GI(input, "Yaw"));
                float cp = System.Convert.ToSingle(ComponentManager.GI(input, "Pitch"));
                float t = (speed + Random.Range(-speed * 0.15f, speed * 0.15f)) / 100f;

                var ny = Mathf.LerpAngle(cy, euler.y + Random.Range(-0.1f, 0.1f), t);
                var np = Mathf.Lerp(cp, euler.x + Random.Range(-0.05f, 0.05f), t);

                ComponentManager.SF(input, "Yaw", ny);
                ComponentManager.SF(input, "Pitch", np);
            }
            catch { }
        }

        private Transform FB(Transform root, string name)
        {
            if (root == null) return null;
            var t = root.Find(name); if (t != null) return t;
            for (int i = 0; i < root.childCount; i++) { t = root.GetChild(i).Find(name); if (t != null) return t; }
            return null;
        }
    }
}
