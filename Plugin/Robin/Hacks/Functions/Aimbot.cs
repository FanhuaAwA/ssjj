using System;
using System.Reflection;
using Assets.Scripts.Input;
using Assets.Sources.Utils.Weapon;
using math;
using physics;
using Plugins.Hacks.Functions;
using Plugins.Hacks.Players;
using Plugins.Hacks.Visuals;
using Plugins.Unity;
using Plugins.Unity.Extension;
using Plugins.Utils;
using share;
using SSJJPhysics;
using UnityEngine;

namespace Plugins.Hacks
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class Aimbot : ModuleBase
    {
        public PlayerCollector collector => ModuleBase.GetPlugin<PlayerCollector>();

        private void AdjustInputDirection(Vector3 targetPosition)
        {
            try
            {
                Vector3 normalized = (targetPosition - Contexts.sharedInstance.worldCamera.unityObjects.mainCamera.transform.position).normalized;
                Vector3 eulerAngles = Quaternion.FromToRotation(normalized, Vector3.forward).eulerAngles;
                if (eulerAngles.x > 180f)
                {
                    eulerAngles.x -= 360f;
                }
                if (Menu.RecoilControl)
                {
                    eulerAngles.y -= Contexts.sharedInstance.player.myPlayerEntity.GetPunchYaw() * 2f;
                    eulerAngles.x -= Contexts.sharedInstance.player.myPlayerEntity.GetPunchPitch() * 2f;
                }
                Contexts.sharedInstance.userCommand.input.Pitch = eulerAngles.x;
                Contexts.sharedInstance.userCommand.input.Yaw = eulerAngles.y;
            }
            catch (Exception e)
            {
                DebugModule.LogError("Aimbot.AdjustInput", e);
            }
        }

        private void EnsureSimulatedInput()
        {
            if (!this.isUsingSimulatedInput)
            {
                InputCollector.Instance.SetDeviceInput(new MouseSimulater());
                this.isUsingSimulatedInput = true;
            }
        }

        public override void Update()
        {
            if (GameManager.IsGameActive && Menu.Aim)
            {
                try
                {
                    this.EnsureSimulatedInput();
                    this.ExecuteAimbotLogic();
                }
                catch (Exception e)
                {
                    DebugModule.LogError("Aimbot.Update", e);
                }
            }
        }

        private Vector3 CalculateAimPosition(Player playerEntity)
        {
            try
            {
                switch (Menu.BodyIndex)
                {
                    case 0:
                        return (playerEntity.model.u_head.position + playerEntity.model.d_head.position) / 2f;
                    case 1:
                        return (playerEntity.model.l_clavicle.position + playerEntity.model.r_clavicle.position) / 2f;
                    case 2:
                        return playerEntity.model.spine != null ? playerEntity.model.spine.position : playerEntity.model.pelvis.position;
                    case 3:
                        return GetNearestBonePosition(playerEntity);
                    default:
                        return (playerEntity.model.u_head.position + playerEntity.model.d_head.position) / 2f;
                }
            }
            catch
            {
                return playerEntity.model.root.position;
            }
        }

        private Vector3 GetNearestBonePosition(Player playerEntity)
        {
            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
            float minDist = float.MaxValue;
            Vector3 bestPos = playerEntity.model.root.position;

            Transform[] bones = new Transform[]
            {
                playerEntity.model.u_head, playerEntity.model.d_head,
                playerEntity.model.l_clavicle, playerEntity.model.r_clavicle,
                playerEntity.model.spine, playerEntity.model.pelvis,
                playerEntity.model.l_upperarm, playerEntity.model.r_upperarm,
                playerEntity.model.l_forearm, playerEntity.model.r_forearm,
                playerEntity.model.l_hand, playerEntity.model.r_hand,
                playerEntity.model.l_thigh, playerEntity.model.r_thigh,
                playerEntity.model.l_calf, playerEntity.model.r_calf,
                playerEntity.model.l_foot, playerEntity.model.r_foot
            };

            foreach (Transform bone in bones)
            {
                if (bone != null)
                {
                    Vector3 screenPos = bone.GetUIPosition();
                    if (screenPos.z > 0f)
                    {
                        float dist = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), screenCenter);
                        if (dist < minDist)
                        {
                            minDist = dist;
                            bestPos = bone.position;
                        }
                    }
                }
            }
            return bestPos;
        }

        private void ExecuteAimbotLogic()
        {
            Vector2 screenCenter = new Vector2(Screen.width, Screen.height) * 0.5f;
            float fovRadius = GetFovRadius();

            if (Menu.AimRange && Menu.Aim)
            {
                GizmosPro.DrawCircle(new Circle(screenCenter, fovRadius), Color.red);
            }

            if (this.GetTargetPoint(out Vector2 targetScreenPos, Menu.BodyIndex, Menu.Vis, fovRadius))
            {
                if (Menu.AimLine)
                {
                    GizmosPro.DrawLine(targetScreenPos, screenCenter, Color.magenta);
                }
                Vector2 delta = targetScreenPos - screenCenter;

                if (IsAimKeyPressed())
                {
                    int aimIndex = Menu.AimIndex;
                    if (aimIndex == 0)
                    {
                        this.AdjustInputDirection(this.CalculateAimPosition(targetPlayer));
                        DebugModule.LogAimbot(targetPlayer.CleanName, Vector2.Distance(targetScreenPos, screenCenter), Menu.BodyType[Menu.BodyIndex]);
                        return;
                    }
                    if (aimIndex != 1)
                    {
                        return;
                    }
                    this.ApplyAimLock(delta * Menu.Speed * 0.0012f);
                    DebugModule.LogAimbot(targetPlayer.CleanName, Vector2.Distance(targetScreenPos, screenCenter), Menu.BodyType[Menu.BodyIndex]);
                }
            }
        }

        private float GetFovRadius()
        {
            float fovDegrees = Menu.aimFov;
            float screenFov = Camera.main != null ? Camera.main.fieldOfView : 90f;
            float ratio = Mathf.Tan(fovDegrees * 0.5f * Mathf.Deg2Rad) / Mathf.Tan(screenFov * 0.5f * Mathf.Deg2Rad);
            return ratio * Screen.height * 0.5f;
        }

        private bool IsAimKeyPressed()
        {
            if (Menu.aimKey == KeyCode.None)
            {
                return true;
            }
            return Input.GetKey(Menu.aimKey);
        }

        private void ApplyAimLock(Vector2 delta)
        {
            MouseSimulater.forceAxisOnce += delta;
        }

        private bool GetTargetPoint(out Vector2 targetPoint, int aimPositionIndex, bool isAimBlocked, float fovRadius)
        {
            PlayerEntity myPlayerEntity = Contexts.sharedInstance.player.myPlayerEntity;
            int num = (myPlayerEntity != null) ? myPlayerEntity.GetTeam() : 0;
            Vector2 vector = new Vector2(Screen.width, Screen.height) * 0.5f;
            float num2 = -1f;
            bool result = false;
            targetPoint = Vector2.zero;
            foreach (Player player in this.collector.players)
            {
                if (player.IsValid && !player.IsDead && player.entity != Contexts.sharedInstance.player.cameraOwnerEntity && player.TeamId != num && (!Menu.CheckInvinciblePlayer || !GameManager.IsPlayerInInvincibleState(player.entity)))
                {
                    Vector3 vector2 = Vector3.zero;
                    Vector2 vector3 = Vector2.zero;

                    Vector3 aimWorldPos = CalculateAimPosition(player);
                    vector2 = aimWorldPos;

                    Vector3 screenPos = Camera.main.WorldToScreenPoint(aimWorldPos);
                    if (screenPos.z <= 0f)
                    {
                        continue;
                    }
                    vector3 = new Vector2(screenPos.x, Screen.height - screenPos.y);

                    if (isAimBlocked)
                    {
                        Vector3 vector4 = vector2 - Camera.main.transform.position;
                        Vector3 normalized = vector4.normalized;
                        Vector3 eulerAngles = Quaternion.FromToRotation(normalized, Vector3.forward).eulerAngles;
                        float[] array = new float[3];
                        AngleUtils.AnglesToVector(eulerAngles.y, eulerAngles.x, array);
                        Vector3D vector3D = new Vector3D(array[0], array[1], array[2]);
                        IPyEngine pyEngine = Contexts.sharedInstance.battleRoom.pyEngine.PyEngine;
                        TraceResult traceResult = FireUtility.BulletTrace(pyEngine, Contexts.sharedInstance.player.myPlayerEntity, Contexts.sharedInstance.player, vector4.magnitude, vector3D, new float[3], new float[3], false);
                        if (traceResult.EntityId < 0)
                        {
                            continue;
                        }
                    }
                    float num4 = Vector2.Distance(vector3, vector);
                    float num5 = fovRadius - num4;
                    if (num4 < fovRadius && num5 > num2)
                    {
                        targetPlayer = player;
                        num2 = num5;
                        targetPoint = vector3;
                        result = true;
                    }
                }
            }
            return result;
        }

        private bool isUsingSimulatedInput;
        public static Player targetPlayer;
    }
}
