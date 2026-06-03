using System.Reflection;
using Assets.Sources.Components.Interface.Info.Weapon;
using Assets.Sources.Utils.Player;
using Assets.Sources.Utils.Weapon;
using data;
using Plugins.Hooks;
using SSJJUserCmd;
using UnityEngine;

[Obfuscation(Feature = "Virtualization", Exclude = false)]
public static class AntiAim
{
    public static void KeySetPitch(ref float pitch)
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            Menu.HeadPitch = Menu.Z;
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            Menu.HeadPitch = Menu.X;
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            Menu.HeadPitch = Menu.C;
        }
    }

    public static float CalculateWeaponSpread(UserCmd userCommand)
    {
        var context = Contexts.sharedInstance;
        var isInvalidContext = context.weapon?.currentWeaponEntity == null
                            || context.battleRoom == null
                            || context.player == null;

        if (isInvalidContext) return 0f;

        var weaponInfo = context.weapon.currentWeaponEntity.basicInfo.Info;
        var playerEntity = context.player.myPlayerEntity;
        var currentWeapon = context.weapon.currentWeaponEntity;

        var sceneMoveData = context.battleRoom.pyEngine.PyEngine.GetWorld().GetSceneMoveData() as SceneMoveData;
        bool isWeightlessness = sceneMoveData?.isWeightlessness ?? false;

        if (!userCommand.PredicatedOnce
            && weaponInfo.AccuracyLogic != null
            && weaponInfo.SpreadLogic != null)
        {
            weaponInfo.SpreadLogic.BeforeFire(
                out currentWeapon.spread.Spread,
                playerEntity,
                currentWeapon,
                userCommand,
                isWeightlessness
            );
            weaponInfo.AccuracyLogic.BeforeFire(
                userCommand.Seq,
                playerEntity,
                currentWeapon,
                playerEntity.clientTime.ClientTime
            );
        }

        var (accuracyFactor, baseSpread) = CalculateAccuracyFactors(weaponInfo, currentWeapon, playerEntity);
        return Mathf.Clamp(accuracyFactor - baseSpread, 0f, 1f);
    }

    private static (float accuracyFactor, float baseSpread) CalculateAccuracyFactors(
        IEntitsWeaponInfo weaponInfo,
        WeaponEntity weapon,
        PlayerEntity player)
    {
        float baseSpread = weapon.spread.Spread;

        float accuracyFactor;
        switch (weaponInfo.WeaponType)
        {
            case 0:
                accuracyFactor = (weapon.accuracy.Accuracy * 100f) / 92f;
                break;

            case 1:
            case 6:
            case 14:
                accuracyFactor = CalculateStandardAccuracy(weapon, weaponInfo);
                break;

            case 5:
                accuracyFactor = HandleSniperAccuracy(player);
                baseSpread = CalculateSniperSpread(player);
                break;

            case 10:
            case 12:
                accuracyFactor = CalculateSpecialAccuracy(weapon, weaponInfo);
                baseSpread = weapon.spread.Spread;
                break;

            default:
                accuracyFactor = 0f;
                break;
        }

        return (accuracyFactor, baseSpread);
    }

    private static float CalculateStandardAccuracy(WeaponEntity weapon, IEntitsWeaponInfo info)
    {
        var accuracyRange = info.MaxInaccuracy - info.DefaultAccuracy;
        return 1f - ((weapon.accuracy.Accuracy - info.DefaultAccuracy) * 100f) / (accuracyRange * 100f);
    }

    private static float CalculateSpecialAccuracy(WeaponEntity weapon, IEntitsWeaponInfo info)
    {
        var accuracyRange = info.MaxInaccuracy - info.AccuracyOffset;
        return 1f - ((weapon.accuracy.Accuracy - info.AccuracyOffset) * 100f) / (accuracyRange * 100f);
    }

    private static float HandleSniperAccuracy(PlayerEntity player)
    {
        return player.fov.IsZoom() ? 1f : 0f;
    }

    private static float CalculateSniperSpread(PlayerEntity player)
    {
        var moveDistance = PlayerUtility.PlayerLength2D(player);

        if (moveDistance > 350f) return 0.4f;
        if (moveDistance > 25f) return 0.7f;
        return 0f;
    }

    public static void ExecuteAntiAim(ref float pitch, UserCmd userCmd, ref float _pitch, ref float _yaw, ref float _moveforward, ref float _moveright, ref int _buttons, ref bool _silenting)
    {
        float randomOffset = 0f;
        if (Menu.AntiIndex == 2)
        {
            randomOffset = Random.Range(Menu.Min, Menu.Max);
        }
        float processedYaw = userCmd.CameraYaw / 100f; float targetYaw = ((180f + processedYaw - Menu.HeadPitch + randomOffset) % 360f) - 180f; float targetPitch = Menu.HeadYaw;
        if (Menu.AntiIndex == 1)
        {
            float dynamicRotation = (processedYaw + 360f + (userCmd.Seq * Menu.Rpm % 360)) % 360f;
            targetYaw = dynamicRotation - 180f;
        }
        float originalForward = userCmd.MoveForward;
        float originalRight = userCmd.MoveRight;
        int originalButtons = userCmd.Buttons;
        bool isSilentAiming = false;
        float weaponSpread = AntiAim.CalculateWeaponSpread(userCmd);
        bool hasValidWeapon = Contexts.sharedInstance != null
                            && Contexts.sharedInstance.weapon != null
                            && Contexts.sharedInstance.weapon.currentWeaponEntity != null;
        bool canAttack = false;
        if (hasValidWeapon)
        {
            int predictionTime = Contexts.sharedInstance.player.cameraOwnerEntity.GetClientTime() + userCmd.FrameInterval;
            canAttack = WeaponUtility.CanAttack(Contexts.sharedInstance.weapon.currentWeaponEntity, predictionTime)
                        && weaponSpread >= Menu.HitRate / 100f;
        }
        bool silentAimActivated = canAttack
                        && SilentAim.ActivateSilentAimbot(
                            Contexts.sharedInstance.player.GetEntities(),
                            Contexts.sharedInstance.player.myPlayerEntity,
                            ref _yaw,
                            ref _pitch);
        if (silentAimActivated)
        {
            if (!userCmd.IsAttackOn)
            {
                userCmd.Buttons |= 64;
                originalButtons |= 64;
            }
            targetYaw = _yaw;
            targetPitch = _pitch;
            isSilentAiming = true;
        }
        AntiAim.AdjustMovementBasedOnCamera(targetYaw, processedYaw, ref originalForward, ref originalRight);
        bool shouldDisableAntiAim = Contexts.sharedInstance.player.myPlayerEntity == null
|| Contexts.sharedInstance.player.myPlayerEntity.IsDead()
|| (!Menu.Anti && !isSilentAiming)
|| (!isSilentAiming && canAttack && (userCmd.IsAttackOn || userCmd.IsSecondaryAttackOn))
|| Contexts.sharedInstance.player.myPlayerEntity.basicInfo.Current.CurrentWeapon == 4;
        if (shouldDisableAntiAim)
        {
            targetYaw = processedYaw;
            targetPitch = userCmd.CameraPitch / 100f;
            originalForward = userCmd.MoveForward;
            originalRight = userCmd.MoveRight;
        }
        AntiAim.sharedYawAngle = targetYaw;
        AntiAim.sharedPitchAngle = targetPitch;
        _pitch = targetPitch;
        _yaw = targetYaw;
        _buttons = originalButtons;
        _moveforward = originalForward;
        _moveright = originalRight;
        _silenting = isSilentAiming;
        AntiAim.isSilentAiming = isSilentAiming;
    }

    public static void AdjustMovementBasedOnCamera(float adjustedYaw, float cameraYaw, ref float moveForward, ref float moveRight)
    {
        float angleDifference = AntiAim.NormalizeAngle(adjustedYaw - cameraYaw);
        float cosAngle = Mathf.Cos(angleDifference * 0.017453292f);
        float sinAngle = Mathf.Sin(angleDifference * 0.017453292f);
        float originalMoveForward = moveForward;
        float originalMoveRight = moveRight;
        moveForward = (cosAngle * originalMoveForward) - (sinAngle * originalMoveRight);
        moveRight = (sinAngle * originalMoveForward) + (cosAngle * originalMoveRight);
        moveForward = Mathf.Clamp(moveForward, -100f, 100f);
        moveRight = Mathf.Clamp(moveRight, -100f, 100f);
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle > 180f)
        {
            angle -= 360f;
        }
        else if (angle < -180f)
        {
            angle += 360f;
        }
        return angle;
    }

    public static int antiAimMode = 1;
    public static int antiAimType = 2;
    public static int antiAimOption = 4;
    public static int antiAimFlag = 8;
    public static float baseAccuracy = 0f;
    public static float sharedYawAngle = 0f;
    public static float sharedPitchAngle = 0f;
    public static float movementAdjustment = 0f;
    public static bool isSilentAiming = false;
    public static long timestamp = 0L;
    public static double precisionValue = 0.0;
}