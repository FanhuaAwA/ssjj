using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Sources.Modules.Player.HitBox;
using Assets.Sources.Utils.Weapon;
using Entitas;
using Plugins.Unity.Extension;
using Plugins.Utils;
using share;
using SSJJBase.String;
using SSJJPhysics;
using UnityEngine;

namespace Plugins.Hooks
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class SilentAim
    {
        public static readonly Dictionary<int, int> bonePriorities = new Dictionary<int, int>
        {
            { GetBoneHash("Bip01_Head"), 20 },                   { GetBoneHash("Bip01_HeadNub"), 19 },                 { GetBoneHash("Bip01_Neck"), 18 },                    { GetBoneHash("Bip01_Spine"), 17 },                { GetBoneHash("Bip01_L_Clavicle"), 16 },              { GetBoneHash("Bip01_R_Clavicle"), 15 },              { GetBoneHash("Bip01_L_UpperArm"), 14 },              { GetBoneHash("Bip01_R_UpperArm"), 13 },              { GetBoneHash("Bip01_L_Forearm"), 12 },                { GetBoneHash("Bip01_R_Forearm"), 11 },                { GetBoneHash("Bip01_L_Hand"), 10 },                   { GetBoneHash("Bip01_R_Hand"), 9 },                   { GetBoneHash("Bip01_L_Finger0"), 8 },                { GetBoneHash("Bip01_R_Finger0"), 7 },                { GetBoneHash("Bip01_Pelvis"), 6 },                   { GetBoneHash("Bip01_L_Thigh"), 5 },                  { GetBoneHash("Bip01_R_Thigh"), 4 },                  { GetBoneHash("Bip01_L_Calf"), 3 },                   { GetBoneHash("Bip01_R_Calf"), 2 },                  { GetBoneHash("Bip01_L_Foot"), 1 },                  { GetBoneHash("Bip01_R_Foot"), 0 },                  { GetBoneHash("Bip01_L_Toe0"), -1 },                  { GetBoneHash("Bip01_R_Toe0"), -2 }               };

        private static string[] SetHitBoxList()
        {
            var hitBoxes = new Dictionary<string, (bool, string[])>
    {
        { "Head", (Menu.Head, new[] { "Bip01_Head" }) },
        { "HeadNub", (Menu.HeadNub, new[] { "Bip01_HeadNub" }) },
        { "Neck", (Menu.Neck, new[] { "Bip01_Neck" }) },
        { "Spine", (Menu.Spine, new[] { "Bip01_Spine" }) },
        { "Clavicle", (Menu.Clavicle, new[] { "Bip01_L_Clavicle", "Bip01_R_Clavicle" }) },
        { "UpperArm", (Menu.UpperArm, new[] { "Bip01_L_UpperArm", "Bip01_R_UpperArm" }) },
        { "Forearm", (Menu.Forearm, new[] { "Bip01_L_Forearm", "Bip01_R_Forearm" }) },
        { "Hand", (Menu.Hand, new[] { "Bip01_L_Hand", "Bip01_R_Hand" }) },
        { "Finger", (Menu.Finger, new[] { "Bip01_L_Finger0", "Bip01_R_Finger0" }) },
        { "Pelvis", (Menu.Pelvis, new[] { "Bip01_Pelvis" }) },
        { "Thigh", (Menu.Thigh, new[] { "Bip01_L_Thigh", "Bip01_R_Thigh" }) },
        { "Calf", (Menu.Calf, new[] { "Bip01_L_Calf", "Bip01_R_Calf" }) },
        { "Foot", (Menu.Foot, new[] { "Bip01_L_Foot", "Bip01_R_Foot" }) },
        { "Toe", (Menu.Toe, new[] { "Bip01_L_Toe0", "Bip01_R_Toe0" }) }
    };

            var list = new List<string>();
            foreach (var kvp in hitBoxes)
            {
                if (kvp.Value.Item1)
                {
                    list.AddRange(kvp.Value.Item2);
                }
            }

            return list.ToArray();
        }

        public static bool ActivateSilentAimbot(List<IEntity> entities, PlayerEntity currentPlayer, ref float aimX, ref float aimY)
        {
            if (!Menu.Sil)
            {
                return false;
            }

            List<AimCandidate> candidates = new List<AimCandidate>();
            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector3 currentPos = GetCurrentPlayerPosition(currentPlayer);
            Camera mainCam = Camera.main;
            foreach (IEntity entity in entities)
            {
                if (!ValidateTarget(entity, currentPlayer))
                {
                    continue;
                }

                PlayerEntity target = (PlayerEntity)entity;
                UpdateHitBoxIfNeeded(target);
                foreach (int boneId in SilentAim.boneList.OrderByDescending(b => bonePriorities.GetValueOrDefault(b, 0)))
                {
                    if (!target.hitBox.BonetTransform.TryGetValue(boneId, out Transform bone))
                    {
                        continue;
                    }

                    Vector3 screenPos = mainCam.WorldToScreenPoint(bone.position);
                    if (!SilentAim.CanHit(currentPlayer, target, currentPos, bone.position))
                    {
                        continue;
                    }

                    Vector2 screenPoint = new Vector2(screenPos.x, Screen.height - screenPos.y);
                    float distance = Vector2.Distance(screenCenter, screenPoint);
                    candidates.Add(new AimCandidate
                    {
                        Priority = bonePriorities.GetValueOrDefault(boneId, 20),
                        Distance = distance,
                        Bone = bone,
                        Target = target
                    });
                }
            }
            AimCandidate bestTarget = candidates
    .OrderByDescending(c => c.Priority)
    .ThenBy(c => c.Distance)
    .FirstOrDefault();
            if (bestTarget != null && ShouldActivateAim())
            {
                Vector3 fixedPos = SilentAim.FixPos(currentPlayer, bestTarget.Target, currentPos, bestTarget.Bone.position);
                (aimX, aimY) = (fixedPos.y, fixedPos.x);
                return true;
            }
            return false;
            Vector3 GetCurrentPlayerPosition(PlayerEntity player)
            {
                Vector3 pos = player.GetCompenstatePos(player.fpos.Change.GetPosIndex());
                pos.z += (float)player.move.PyPlayerMove.GetViewHeight();
                return SSJJMath.VectorCoordConverter.SsjjToUnity(pos);
            }
            bool ValidateTarget(IEntity entity, PlayerEntity current)
            {
                return entity is PlayerEntity target &&
                       !target.IsMySelf() &&
                       target.hasHitBox &&
                       target.hasThirdPersonUnityObjects &&
                       target.GetTeam() != current.GetTeam() &&
                       target.GetHpPercent() > 0 &&
                       !target.IsDead() &&
                       !GameManager.IsPlayerInInvincibleState(target);
            }
            bool ShouldActivateAim()
            {
                return Menu.FireIndex switch
                {
                    1 => true,
                    0 => Input.GetKey(KeyCode.Mouse2),
                    _ => false,
                };
            }
            void UpdateHitBoxIfNeeded(PlayerEntity target)
            {
                if (target.hitBox.HitBoxBrushDirty)
                {
                    PlayerHitBoxBrushUtility.UpdatePlayerAllHitBoxBrush(target);
                }
            }
        }

        private class AimCandidate
        {
            public int Priority { get; set; }
            public float Distance { get; set; }
            public Transform Bone { get; set; }
            public PlayerEntity Target { get; set; }
        }

        public static Vector3 CalculateDirectionAngles(Vector3 startPosition, Vector3 targetPosition)
        {
            Vector3 normalized = (targetPosition - startPosition).normalized;
            float num = (Mathf.Atan2(normalized.z, normalized.x) * 57.29578f) - 90f;
            if (num < -180f)
            {
                num += 360f;
            }
            if (num > 180f)
            {
                num -= 360f;
            }
            float num2 = Mathf.Asin(normalized.y) * 57.29578f;
            num = Mathf.Clamp(num, -180f, 180f);
            num2 = Mathf.Clamp(num2, -90f, 90f);
            return new Vector3(num2, num, 0f);
        }

        public static float GetMaxInRange(float value1, float value2, float limit)
        {
            return value1 <= value2 ? value2 : value1 >= limit ? limit : value1;
        }

        public static Vector3 ClampVector3(Vector3 vector)
        {
            vector.x = SilentAim.GetMaxInRange(vector.x, -90f, 90f);
            vector.y = SilentAim.GetMaxInRange(vector.y, -180f, 180f);
            vector.z = 0f;
            return vector;
        }

        public static Vector3 NormalizeVector3(Vector3 vector)
        {
            if (vector.x > 90f)
            {
                vector.x -= 180f;
            }
            else if (vector.x < -90f)
            {
                vector.x += 180f;
            }
            vector.y %= 360f;
            if (vector.y > 180f)
            {
                vector.y -= 360f;
            }
            return vector;
        }

        private static bool CanHit(PlayerEntity shooter, PlayerEntity target, Vector3 start, Vector3 end)
        {
            Vector3 vector = SSJJMath.VectorCoordConverter.UnityToSsjj((end - start).normalized);
            TraceResult traceResult = FireUtility.BulletTrace(Contexts.sharedInstance.battleRoom.pyEngine.PyEngine, shooter, Contexts.sharedInstance.player, 100000f, new Vector3D(vector.x, vector.y, vector.z), new float[3], new float[3], false);
            return traceResult.EntityId == target.GetId();
        }

        public static Vector3 FixPos(PlayerEntity shooter, PlayerEntity target, Vector3 start, Vector3 end)
        {
            float num = Contexts.sharedInstance.userCommand.commands.CommandToSendList.Last.Value.FrameInterval * 0.001f;
            Vector3 vector = start + (SSJJMath.VectorCoordConverter.SsjjToUnity(shooter.move.Velocity) * num);
            Vector3 vector2 = end + (SSJJMath.VectorCoordConverter.SsjjToUnity(target.move.Velocity) * num);
            Vector3 vector3 = SilentAim.CalculateDirectionAngles(vector, vector2);
            vector3 = SilentAim.NormalizeVector3(vector3);
            vector3 = SilentAim.ClampVector3(vector3);
            SilentAim.RecoilControl(shooter, ref vector3);
            return SilentAim.NormalizeVector3(vector3);
        }

        public static void RecoilControl(PlayerEntity player, ref Vector3 direction)
        {
            float punchPitch = player.GetPunchPitch();
            float punchYaw = player.GetPunchYaw();
            direction.x -= 2f * punchPitch;
            direction.y -= 2f * punchYaw;
        }

        public static int GetBoneHash(string boneName)
        {
            return new IgnoreCaseString(boneName).GetHashCode();
        }

        public static void GetBoneHashes()
        {
            SilentAim.boneList.Clear();
            hitboxList = SilentAim.SetHitBoxList();
            foreach (string text in hitboxList)
            {
                SilentAim.boneList.Add(SilentAim.GetBoneHash(text));
            }
        }

        private static string[] hitboxList;
        public static List<int> boneList = new List<int>();
    }
}