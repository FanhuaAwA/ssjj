using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Sources.Components.Camera;
using Assets.Sources.Components.Interface;
using Assets.Sources.Components.Interface.Info.Camera;
using Assets.Sources.Components.Snapshot;
using Assets.Sources.Components.UserComand;
using Assets.Sources.Config;
using Assets.Sources.Info.Camera.CameraLogic;
using Assets.Sources.Modules.Player.EntityCreate;
using Assets.Sources.Modules.Player.Orientation;
using Assets.Sources.Modules.Ui.UiEventCondition;
using Assets.Sources.Modules.WorldCamera;
using Assets.Sources.Networking.Server;
using Assets.Sources.Snapshots;
using Assets.Sources.Systems.PacketHandle.Handlers;
using Assets.Sources.Systems.Snapshot;
using Assets.Sources.Systems.UserCommand;
using Assets.Sources.Utils;
using Assets.Sources.Utils.Player;
using config;
using I2.Loc;
using MonoMod.RuntimeDetour;
using NetData;
using physics;
using Plugins.Hacks;
using Plugins.Unity.Extension;
using share;
using SSJJBase.Utility;
using SSJJMath;
using SSJJNetworking.Packet;
using SSJJUserCmd;
using UnityEngine;

[Obfuscation(Feature = "Virtualization", Exclude = false)]
public static class HookManager
{
    public static void UnHook()
    {
        foreach (HookManager.DetourEntry detourEntry in HookManager.detourEntries)
        {
            detourEntry.idetour_0?.Undo();
        }
    }

    public static bool CreateHook(Type targetType, string originalMethodName, Delegate hookMethod, IDetour detourObject = null)
    {
        if (HookManager.detourEntries == null)
        {
            return false;
        }
        HookManager.DetourEntry detourEntry = new HookManager.DetourEntry
        {
            idetour_0 = detourObject ?? new Hook(targetType.GetMethod(originalMethodName, HookManager.bindingFlags), hookMethod.Method)
        };
        HookManager.detourEntries.Add(detourEntry);
        return true;
    }

    public static void InitializeHooks()
    {
        SafeRegisterHook("PostProcessUserCommandSystem.InterceptNew", typeof(PostProcessUserCommandSystem), "InterceptNew", new HookManager.PostProcessUserCommandDelegate(HookManager.InterceptUserCommand));
        SafeRegisterHook("SendUserCommandSystem.GetUserCmdBytes", typeof(SendUserCommandSystem), "GetUserCmdBytes", new HookManager.SendUserCommandDelegate(HookManager.GenerateUserCommandBytes));
        SafeRegisterHook("BattleServer.SendUdpData", typeof(BattleServer), "SendUdpData", new HookManager.BattleServerMethodDelegate(HookManager.SendUdpData));
        SafeRegisterHook("ComputeUserCommandSystem.MakeCommand", typeof(ComputeUserCommandSystem), "MakeCommand", new HookManager.ComputeUserCommandDelegate(HookManager.ExecuteUserCommand));
        SafeRegisterHook("TpsCameraLogic.IsActive", typeof(TpsCameraLogic), "IsActive", new HookManager.TpsCameraLogicBoolDelegate(HookManager.UpdateCameraActivationStatus));
        SafeRegisterHook("TpsCameraLogic.Update", typeof(TpsCameraLogic), "Update", new HookManager.TpsCameraLogicUpdateDelegate(HookManager.UpdateTpsCameraLogic));
        SafeRegisterHook("CameraFunction.GetCurrentCmdYaw", typeof(CameraFunction), "GetCurrentCmdYaw", new HookManager.CameraLogicFloatDelegate(HookManager.GetCurrentCameraYaw));
        SafeRegisterHook("CameraFunction.GetCurrentCmdPitch", typeof(CameraFunction), "GetCurrentCmdPitch", new HookManager.CameraLogicFloatDelegate(HookManager.GetCurrentCameraPitch));
        SafeRegisterHook("UiIEventCondition.Get_ControlEntityData_Yaw", typeof(UiIEventCondition), "Get_ControlEntityData_Yaw", new HookManager.FloatDelegate(HookManager.GetControlEntityYaw));
        SafeRegisterHook("UiIEventCondition.Get_cameraOwnerData_Yaw", typeof(UiIEventCondition), "Get_cameraOwnerData_Yaw", new HookManager.FloatDelegate(HookManager.GetCameraOwnerYaw));
        SafeRegisterHook("PlayerOrientationPredicationSystem.OnPredicate", typeof(PlayerOrientationPredicationSystem), "OnPredicate", new HookManager.PlayerOrientationPredictionDelegate(HookManager.HookOnPlayerOrientation));
        SafeRegisterHook("PlayerOrientationPlabackSystem.OnPlayback", typeof(PlayerOrientationPlabackSystem), "OnPlayback", new HookManager.PlayerOrientationPlaybackDelegate(HookManager.ExecutePlayerPlaybackModification));
        SafeRegisterHook("PlayerOrientationPredicationSystem.PredictCmdOnCamera", typeof(PlayerOrientationPredicationSystem), "PredictCmdOnCamera", new HookManager.PlayerOrientationPredictionDelegate(HookManager.HookPredictCommandOnCamera));
        SafeRegisterHook("CommandsComponent.LastCameraYaw", typeof(CommandsComponent), "LastCameraYaw", new HookManager.CommandsComponentDelegate(HookManager.RetrieveCameraYaw));
        SafeRegisterHook("CommandsComponent.LastCameraPitch", typeof(CommandsComponent), "LastCameraPitch", new HookManager.CommandsComponentDelegate(HookManager.RetrieveCameraPitch));
        SafeRegisterHook("CameraLogicToTransformSystem.OnAfterPredication", typeof(CameraLogicToTransformSystem), "OnAfterPredication", new HookManager.CameraLogicToTransformDelegate(HookManager.UpdateCameraAfterPrediction));
        SafeRegisterHook("PlayerSpeedUtil.GetPlayerSpeed", typeof(PlayerSpeedUtil), "GetPlayerSpeed", new HookManager.PlayerMoveCommandDelegate(HookManager.GetPlayerSpeed));
        SafeRegisterHook("HitPlayerHandler.Handle", typeof(HitPlayerHandler), "Handle", new HookManager.HitPlayerSetupDelegate(HookManager.ExecuteHookHandle));
        SafeRegisterHook("LocalizationManager.SelectStartupLanguage", typeof(LocalizationManager), "SelectStartupLanguage", new HookManager.SelectStartupLanguageDelegate(HookManager.SelectStartupLanguage));
        SafeRegisterHook("TplManager.GetBootConfig", typeof(TplManager), "GetBootConfig", new HookManager.GameBootConfigDelegate(HookManager.RetrieveBootConfig));
    }

    private static void SafeRegisterHook(string label, Type targetType, string methodName, Delegate hookDelegate)
    {
        try
        {
            _ = HookManager.CreateHook(targetType, methodName, hookDelegate, null);
            DebugModule.Log("Hook", $"{label}: OK");
        }
        catch (Exception e)
        {
            DebugModule.LogError($"Hook.{label}", e);
        }
    }

    public static GameBootConfig RetrieveBootConfig(Func<TplManager, GameBootConfig> getConfigFunc, TplManager tplManager)
    {
        return getConfigFunc(tplManager);
    }

    public static void SelectStartupLanguage()
    {
        string @string = PlayerPrefs.GetString("I2 Language", string.Empty);
        string fallbackLanguage = HookManager.GetFallbackLanguage(@string);
        if (LocalizationManager.HasLanguage(@string, true, false))
        {
            LocalizationManager.CurrentLanguage = @string;
            return;
        }
        string supportedLanguage = LocalizationManager.GetSupportedLanguage(fallbackLanguage);
        if (string.IsNullOrEmpty(supportedLanguage))
        {
            HookManager.SetFirstAvailableLanguage();
            return;
        }
        LocalizationManager.SetLanguageAndCode(supportedLanguage, LocalizationManager.GetLanguageCode(supportedLanguage), false, false);
    }

    private static string GetFallbackLanguage(string language)
    {
        return language == "ChineseSimplified"
            ? "Chinese (Simplified)"
            : language == "ChineseTraditional" ? "Chinese (Traditional)" : language;
    }

    private static void SetFirstAvailableLanguage()
    {
        for (int i = 0; i < LocalizationManager.Sources.Count; i++)
        {
            LanguageSource languageSource = LocalizationManager.Sources[i];
            if (languageSource.mLanguages.Count > 0)
            {
                LanguageData languageData = languageSource.mLanguages[0];
                LocalizationManager.SetLanguageAndCode(languageData.Name, languageData.Code, false, false);
                return;
            }
        }
    }

    public static void ExecuteHookHandle(Action<HitPlayerHandler, GameServerSetupData> action, HitPlayerHandler playerHandler, GameServerSetupData serverSetupData)
    {
        action?.Invoke(playerHandler, serverSetupData);
    }

    public static void SendUdpData(Action<BattleServer, int, byte[]> sendAction, BattleServer server, int packetId, byte[] data = null)
    {
        bool shouldSendImmediately = !Menu.FakeLag || Input.GetKey(KeyCode.LeftControl) ||
                                  Contexts.sharedInstance.player.myPlayerEntity == null ||
                                  Contexts.sharedInstance.player.cameraOwnerEntity == null ||
                                  Contexts.sharedInstance.player.myPlayerEntity.IsDead();
        if (shouldSendImmediately)
        {
            sendAction(server, packetId, data);
            return;
        }
        UdpPacket newUdpPacket = UdpPacket.CreateUdpPacket(server.ConnectionId, packetId, data);
        HookManager.udpPackets.Add(newUdpPacket);
        bool isSilentAimingAndSpecificSettings = AntiAim.isSilentAiming && Menu.AntiIndex == 1 && Menu.Sil;
        if (isSilentAimingAndSpecificSettings)
        {
            HookManager.isProcessing = true;
            return;
        }
        HookManager.tempTick = Menu.rdm ? HookManager.tempTick : Menu.Tick;
        bool shouldProcessPackets;
        if (HookManager.udpPackets.Count < HookManager.tempTick && !HookManager.isProcessing)
        {
            float playerDistance2D = PlayerUtility.PlayerLength2D(Contexts.sharedInstance.player.cameraOwnerEntity);
            shouldProcessPackets = playerDistance2D <= 0.1f;
        }
        else
        {
            shouldProcessPackets = true;
        }
        if (shouldProcessPackets)
        {
            foreach (UdpPacket packet in HookManager.udpPackets)
            {
                server.UdpSocket.Send(packet.FinalData, packet.FinalLength);
            }
            HookManager.isProcessing = false;
            HookManager.udpPackets.Clear();
        }
    }

    public static int GetPlayerSpeed(Func<IPyPlayerMove, IPyUserCmd, int> calculateSpeedFunc, IPyPlayerMove playerMovement, IPyUserCmd userCommand)
    {
        return calculateSpeedFunc(playerMovement, userCommand);
    }

    public static void InterceptUserCommand(Action<PostProcessUserCommandSystem, UserCmd> commandAction, PostProcessUserCommandSystem commandSystem, UserCmd userCommand)
    {
        PlayerEntity myPlayerEntity = Contexts.sharedInstance.player.myPlayerEntity;
        if (myPlayerEntity == null || myPlayerEntity.IsDead())
        {
            commandAction(commandSystem, userCommand);
        }
    }

    public static void UpdateCameraAfterPrediction(Action<CameraLogicToTransformSystem> cameraAction, CameraLogicToTransformSystem cameraLogicSystem)
    {
        WorldCameraContext worldCameraContext = cameraLogicSystem.GetFieldValue<WorldCameraContext>("_worldCameraContext");
        ICameraLogic cameraLogic = worldCameraContext.cameraLogic.CameraLogic;
        if (cameraLogic != null && Contexts.sharedInstance.player.myPlayerEntity != null)
        {
            PlayerEntity myPlayerEntity = Contexts.sharedInstance.player.myPlayerEntity;
            float viewPitch = myPlayerEntity.GetViewPitch();
            float punchPitchAdjustment = Menu.NoShake ? (Menu.RecoilControl ? myPlayerEntity.GetPunchPitch() : 0f) : myPlayerEntity.GetPunchPitch();
            float totalPitch = viewPitch + punchPitchAdjustment;
            float viewYaw = myPlayerEntity.GetViewYaw();
            float punchYawAdjustment = Menu.NoShake ? (Menu.RecoilControl ? myPlayerEntity.GetPunchYaw() : 0f) : myPlayerEntity.GetPunchYaw();
            Vector3 orientationVector = new Vector3(totalPitch, viewYaw + punchYawAdjustment, 0f);
            worldCameraContext.cameraMode.Mode = cameraLogic.CameraMode();
            worldCameraContext.cameraTransform.Fov = cameraLogic.Fov();
            CameraTransformComponent cameraTransform = worldCameraContext.cameraTransform;
            cameraTransform.Pitch = myPlayerEntity.IsDead() ? cameraLogic.Pitch() : orientationVector.x;
            cameraTransform.Roll = cameraLogic.Roll();
            cameraTransform.Yaw = myPlayerEntity.IsDead() ? cameraLogic.Yaw() : orientationVector.y;
            cameraTransform.position = cameraLogic.Position();
        }
    }

    public static short RetrieveCameraYaw(Func<CommandsComponent, short> fallbackFunction, CommandsComponent commandsComponent)
    {
        bool isPlayerAlive = Contexts.sharedInstance.player.myPlayerEntity != null && !Contexts.sharedInstance.player.myPlayerEntity.IsDead();
        short yawValue;
        if (isPlayerAlive)
        {
            float yaw = Contexts.sharedInstance.worldCamera.cameraTransform.Yaw;
            yawValue = (short)(yaw * 100f);
        }
        else
        {
            yawValue = fallbackFunction(commandsComponent);
        }
        return yawValue;
    }

    public static short RetrieveCameraPitch(Func<CommandsComponent, short> fallbackFunction, CommandsComponent commandsComponent)
    {
        bool isPlayerAlive = Contexts.sharedInstance.player.myPlayerEntity != null && !Contexts.sharedInstance.player.myPlayerEntity.IsDead();
        short pitchValue;
        if (isPlayerAlive)
        {
            float pitch = Contexts.sharedInstance.worldCamera.cameraTransform.Pitch;
            pitchValue = (short)(pitch * 100f);
        }
        else
        {
            pitchValue = fallbackFunction(commandsComponent);
        }
        return pitchValue;
    }

    public static void ExecuteUserCommand(Action<ComputeUserCommandSystem, UserCmd, PlayerEntity> commandAction, ComputeUserCommandSystem commandSystem, UserCmd userCommand, PlayerEntity playerEntity)
    {
        commandAction(commandSystem, userCommand, playerEntity);
    }

    public static void ExecutePlayerPlaybackModification(Action<PlayerOrientationPlabackSystem> modificationAction, PlayerOrientationPlabackSystem playerOrientationPlaybackSystem)
    {
        modificationAction(playerOrientationPlaybackSystem);
        Contexts sharedInstance = Contexts.sharedInstance;
        PlayerEntity myPlayerEntity = Contexts.sharedInstance.player.myPlayerEntity;
        if (myPlayerEntity != null && !myPlayerEntity.IsDead())
        {
            bool isCameraOwnerValid;
            if (sharedInstance == null)
            {
                isCameraOwnerValid = null != null;
            }
            else
            {
                PlayerContext player = sharedInstance.player;
                isCameraOwnerValid = (player?.cameraOwnerEntity) != null;
            }
            if (isCameraOwnerValid)
            {
                PlayerEntity cameraOwnerEntity = sharedInstance.player.cameraOwnerEntity;
                if (cameraOwnerEntity.orientation != null && cameraOwnerEntity.basicInfo != null && cameraOwnerEntity.punchOrientation != null)
                {
                    PlayerEntityData next = cameraOwnerEntity.basicInfo.Next;
                    cameraOwnerEntity.orientation.Pitch = AntiAim.sharedPitchAngle;
                    cameraOwnerEntity.orientation.Yaw = AntiAim.sharedYawAngle;
                    cameraOwnerEntity.punchOrientation.PunchPitch = next.PunchPitch;
                    cameraOwnerEntity.punchOrientation.PunchYaw = next.PunchYaw;
                    cameraOwnerEntity.orientation.MoveYaw = AntiAim.sharedYawAngle;
                    cameraOwnerEntity.orientation.ActThirdMoveInterYaw = AntiAim.sharedYawAngle;
                }
            }
        }
    }

    public static void HookOnPlayerOrientation(Action<PlayerOrientationPredicationSystem, PlayerEntity, IUserCmd> modifyAction, PlayerOrientationPredicationSystem orientationSystem, PlayerEntity playerEntity, IUserCmd userCmd)
    {
        Contexts contexts = Contexts.sharedInstance;
        bool isCameraOwnerValid;
        if (contexts == null)
        {
            isCameraOwnerValid = null != null;
        }
        else
        {
            PlayerContext playerContext = contexts.player;
            isCameraOwnerValid = (playerContext?.cameraOwnerEntity) != null;
        }
        if (isCameraOwnerValid)
        {
            PlayerEntity cameraOwner = contexts.player.cameraOwnerEntity;
            if (cameraOwner.orientation != null)
            {
                PlayerEntity myPlayer = contexts.player.myPlayerEntity;
                if (myPlayer != null && !myPlayer.IsDead())
                {
                    cameraOwner.orientation.Pitch = AntiAim.sharedPitchAngle;
                    cameraOwner.orientation.Yaw = AntiAim.sharedYawAngle;
                }
                modifyAction(orientationSystem, playerEntity, userCmd);
            }
        }
    }

    public static void HookPredictCommandOnCamera(Action<PlayerOrientationPredicationSystem, PlayerEntity, IUserCmd> action, PlayerOrientationPredicationSystem playerOrientationPredictionSystem, PlayerEntity targetPlayer, IUserCmd userCommand)
    {
        PlayerEntity myPlayerEntity = Contexts.sharedInstance.player.myPlayerEntity;
        if (myPlayerEntity == null || myPlayerEntity.IsDead())
        {
            action(playerOrientationPredictionSystem, targetPlayer, userCommand);
        }
    }

    public static float GetCameraOwnerYaw(Func<float> fallbackYawFunc)
    {
        return Contexts.sharedInstance.player.myPlayerEntity != null && !Contexts.sharedInstance.player.myPlayerEntity.IsDead()
            ? Contexts.sharedInstance.worldCamera.cameraTransform.Yaw
            : fallbackYawFunc();
    }

    public static float GetControlEntityYaw(Func<float> fallbackYawFunc)
    {
        return Contexts.sharedInstance.player.myPlayerEntity != null && !Contexts.sharedInstance.player.myPlayerEntity.IsDead()
            ? UiIEventCondition.Get_cameraOwnerData_Yaw()
            : fallbackYawFunc();
    }

    public static float GetCurrentCameraYaw(Func<ICameraLogic, float> fallbackYawFunc, ICameraLogic cameraLogic)
    {
        return Contexts.sharedInstance.player.myPlayerEntity != null && !Contexts.sharedInstance.player.myPlayerEntity.IsDead()
            ? Contexts.sharedInstance.worldCamera.cameraTransform.Yaw
            : fallbackYawFunc(cameraLogic);
    }

    public static float GetCurrentCameraPitch(Func<ICameraLogic, float> fallbackPitchFunc, ICameraLogic cameraLogic)
    {
        return Contexts.sharedInstance.player.myPlayerEntity != null && !Contexts.sharedInstance.player.myPlayerEntity.IsDead()
            ? Contexts.sharedInstance.worldCamera.cameraTransform.Pitch
            : fallbackPitchFunc(cameraLogic);
    }

    public static void UpdateTpsCameraLogic(Action<TpsCameraLogic> originalAction, TpsCameraLogic cameraLogic)
    {
        originalAction(cameraLogic);
        if (Contexts.sharedInstance.player.myPlayerEntity == null || Contexts.sharedInstance.player.myPlayerEntity.IsDead())
        {
            return;
        }
        CameraDataComponent cameraData = Contexts.sharedInstance.worldCamera.cameraData;
        PlayerEntity myPlayerEntity = Contexts.sharedInstance.player.myPlayerEntity;
        Vector3 fieldValue = cameraLogic.GetFieldValue<Vector3>("_viewOrgPosition");
        Vector3 vector = default;
        if (cameraData.IsTps)
        {
            vector = cameraLogic.GetCalculateCameraEndPos(fieldValue, cameraData.CameraYawAddValue, cameraData.CameraPitchAddValue, cameraLogic.GetFieldValue<float>("_distance"), 10f);
            Vector3D vector3D = new Vector3D();
            Vector3D vector3D2 = new Vector3D();
            Vector3D vector3D3 = new Vector3D();
            AngleUtility.AnglesToVectors2(cameraLogic.GetFieldValue<float>("_yaw"), cameraLogic.GetFieldValue<float>("_pitch"), vector3D, vector3D2, vector3D3);
            _ = vector3D.Normalize();
            _ = vector3D2.Normalize();
            _ = vector3D3.Normalize();
            vector3D2.ScaleBy(50f);
            vector = cameraLogic.GetCalculateCameraEndPos(vector, cameraData.CameraYawAddValue, 0f, 50f, 10f);
            if (myPlayerEntity.fov.Fov != cameraData.Fov)
            {
                myPlayerEntity.fov.Fov = cameraData.Fov;
                myPlayerEntity.fov.DelayFov = cameraData.Fov;
            }
        }
        if (cameraData.TransTime != 0)
        {
            cameraLogic.InterpolateCamareDeadEndPos(fieldValue, vector, cameraData.TransTime);
        }
    }

    public static bool UpdateCameraActivationStatus(Func<TpsCameraLogic, bool> originalFunc, TpsCameraLogic cameraLogic)
    {
        _ = originalFunc(cameraLogic);
        CameraDataComponent cameraData = Contexts.sharedInstance.worldCamera.cameraData;
        cameraData.Fov = 90;
        cameraData.CameraYawAddValue = cameraLogic.GetFieldValue<float>("_yaw");
        cameraData.CameraPitchAddValue = cameraLogic.GetFieldValue<float>("_pitch") - 5f;
        cameraData.TransTime = Mathf.Max(230, cameraData.TransTime + Contexts.sharedInstance.time.time.FrameInterval);
        cameraData.IsTps = Menu.Act;
        return Menu.Act;
    }

    public static byte[] GenerateUserCommandBytes(SendUserCommandDataDelegate commandDelegate, SendUserCommandSystem commandSystem, LinkedList<UserCmd> userCommands, SnapshotsComponent snapshotComponent, out int byteCount, bool isSpecialCommand)
    {
        byteCount = 0;
        byte[] resultBytes;
        if (Contexts.sharedInstance.player.myPlayerEntity == null || Contexts.sharedInstance.player.myPlayerEntity.IsDead())
        {
            resultBytes = commandDelegate(commandSystem, userCommands, snapshotComponent, out byteCount, isSpecialCommand);
        }
        else if (userCommands.Count != 0)
        {
            UserCmd firstCommand = userCommands.First.Value;
            float yaw = 0f;
            float pitch = 0f;
            float forwardMove = 0f;
            float sideMove = 0f;
            int buttons = 0;
            bool isAntiAimActive = false;
            AntiAim.KeySetPitch(ref pitch);
            HookManager.ExecuteBunnyHop(Contexts.sharedInstance.player.myPlayerEntity, ref firstCommand);
            AntiAim.ExecuteAntiAim(ref pitch, firstCommand, ref pitch, ref yaw, ref forwardMove, ref sideMove, ref buttons, ref isAntiAimActive);
            IRecord movementRecord = SendUserCommandSystem.Record;
            bool isSelfMoving = movementRecord == null || movementRecord.IsSelfMove();
            LinkedListNode<UserCmd> currentCommandNode = userCommands.First;
            HookManager.binaryDataWriter.Reset();
            if (isSpecialCommand)
            {
                HookManager.binaryDataWriter.WriteByte(31);
            }
            int snapshotLatency = Mathf.Min(snapshotComponent.ReceiveSnapshotLatency, 255);
            HookManager.binaryDataWriter.WriteByte((byte)snapshotLatency);

            HookManager.binaryDataWriter.WriteInt(firstCommand.Seq);
            HookManager.binaryDataWriter.WriteInt(firstCommand.RenderTime);
            HookManager.binaryDataWriter.WriteInt(snapshotComponent.LatestSnapshotSeqId);
            int commandFlags = 191;
            HookManager.binaryDataWriter.WriteByte((byte)commandFlags);
            HookManager.binaryDataWriter.WriteByte((byte)firstCommand.FrameInterval);
            HookManager.binaryDataWriter.WriteByte((byte)(isSelfMoving ? ((byte)forwardMove) : 0));
            HookManager.binaryDataWriter.WriteByte((byte)(isSelfMoving ? ((byte)sideMove) : 0));
            HookManager.binaryDataWriter.WriteInt(buttons);
            int weaponAndBagId = ((0 | firstCommand.BagId) << 4) | firstCommand.Weapon;
            HookManager.binaryDataWriter.WriteByte((byte)weaponAndBagId);
            HookManager.binaryDataWriter.WriteShort((short)(yaw * 100f));
            HookManager.binaryDataWriter.WriteShort((short)(pitch * 100f));
            for (currentCommandNode = currentCommandNode.Next; currentCommandNode != null; currentCommandNode = currentCommandNode.Next)
            {
                UserCmd currentCommand = currentCommandNode.Value;
                HookManager.ExecuteBunnyHop(Contexts.sharedInstance.player.myPlayerEntity, ref currentCommand);
                AntiAim.ExecuteAntiAim(ref pitch, currentCommand, ref pitch, ref yaw, ref forwardMove, ref sideMove, ref buttons, ref isAntiAimActive);
                int currentPosition = HookManager.binaryDataWriter.Position;
                HookManager.binaryDataWriter.WriteByte(0);

                HookManager.binaryDataWriter.WriteByte((byte)currentCommand.FrameInterval);
                HookManager.binaryDataWriter.WriteByte((byte)(isSelfMoving ? ((int)forwardMove) : 0));
                HookManager.binaryDataWriter.WriteByte((byte)(isSelfMoving ? ((int)sideMove) : 0));
                HookManager.binaryDataWriter.WriteInt(buttons);
                weaponAndBagId = ((0 | currentCommand.BagId) << 4) | currentCommand.Weapon;
                HookManager.binaryDataWriter.WriteByte((byte)weaponAndBagId);
                commandFlags = 31;
                HookManager.binaryDataWriter.WriteShort((short)(yaw * 100f));
                commandFlags |= 32;
                HookManager.binaryDataWriter.WriteShort((short)(pitch * 100f));
                int newPosition = HookManager.binaryDataWriter.Position;
                HookManager.binaryDataWriter.Position = currentPosition;
                HookManager.binaryDataWriter.WriteByte((byte)commandFlags);
                HookManager.binaryDataWriter.Position = newPosition;
            }
            byte[] commandBytes = NetByteFactory.Instance.GetOrCreateNormalByte(HookManager.binaryDataWriter.Length, true);
            _ = HookManager.binaryDataWriter.SetBytes(commandBytes);
            byteCount = commandBytes.Length;
            resultBytes = commandBytes;
        }
        else
        {
            resultBytes = null;
        }
        return resultBytes;
    }

    public static void ExecuteBunnyHop(PlayerEntity player, ref UserCmd command)
    {
        if (Menu.BunnyHop && !player.IsOnGround())
        {
            if (command.IsJump)
            {
                command.Buttons &= -33;
            }
            float axisX = command.AxisX;
            if (axisX >= 2f)
            {
                command.MoveForward = 0f;
                command.MoveRight = 100f;
                return;
            }
            if (axisX <= -2f)
            {
                command.MoveForward = 0f;
                command.MoveRight = -100f;
            }
        }
    }

    private static readonly List<HookManager.DetourEntry> detourEntries = new List<HookManager.DetourEntry>();
    public static BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    public static float pitch = 0f;
    private static bool isProcessing = false;
    public static global::System.Random random = new global::System.Random();
    public static List<UdpPacket> udpPackets = new List<UdpPacket>();
    private static readonly BinaryDataWriter binaryDataWriter = new BinaryDataWriter();
    public static int tempTick;

    private class DetourEntry
    {
        public IDetour idetour_0;
    }

    public delegate GameBootConfig GameBootConfigDelegate(Func<TplManager, GameBootConfig> orig, TplManager self);

    public delegate void AssembleSnapshotDelegate(Action<AssembleSystem, SnapshotsComponent, ISnapshot> orig, AssembleSystem self, SnapshotsComponent comp, ISnapshot snapshot);

    public delegate PlayerEntity PlayerEntityCreateDelegate(Func<PlayerEntityCreateSystem, int, PlayerEntity> orig, PlayerEntityCreateSystem self, int entityId);

    public delegate void SelectStartupLanguageDelegate();

    public delegate void HitPlayerSetupDelegate(Action<HitPlayerHandler, GameServerSetupData> orig, HitPlayerHandler self, GameServerSetupData data);

    public delegate void BattleServerMethodDelegate(Action<BattleServer, int, byte[]> orig, BattleServer self, int methodId, byte[] data = null);

    public delegate int PlayerMoveCommandDelegate(Func<IPyPlayerMove, IPyUserCmd, int> orig, IPyPlayerMove player, IPyUserCmd cmd);

    public delegate void AbstractCaptureSnapshotDelegate(Action<AbstractCaptureSnapshot> orig, AbstractCaptureSnapshot self);

    public delegate void PostProcessUserCommandDelegate(Action<PostProcessUserCommandSystem, UserCmd> orig, PostProcessUserCommandSystem self, UserCmd userCmd);

    public delegate void CameraLogicToTransformDelegate(Action<CameraLogicToTransformSystem> orig, CameraLogicToTransformSystem self);

    public delegate short CommandsComponentDelegate(Func<CommandsComponent, short> orig, CommandsComponent self);

    public delegate void ComputeUserCommandDelegate(Action<ComputeUserCommandSystem, UserCmd, PlayerEntity> orig, ComputeUserCommandSystem self, UserCmd cmd, PlayerEntity myPlayer);

    public delegate void PlayerOrientationPlaybackDelegate(Action<PlayerOrientationPlabackSystem> orig, PlayerOrientationPlabackSystem self);

    public delegate void PlayerOrientationPredictionDelegate(Action<PlayerOrientationPredicationSystem, PlayerEntity, IUserCmd> orig, PlayerOrientationPredicationSystem self, PlayerEntity myPlayer, IUserCmd cmd);

    public delegate float FloatDelegate(Func<float> orig);

    public delegate float CameraLogicFloatDelegate(Func<ICameraLogic, float> orig, ICameraLogic cameraLogic);

    public delegate void TpsCameraLogicUpdateDelegate(Action<TpsCameraLogic> orig, TpsCameraLogic self);

    public delegate bool TpsCameraLogicBoolDelegate(Func<TpsCameraLogic, bool> orig, TpsCameraLogic self);

    public delegate byte[] SendUserCommandDelegate(SendUserCommandDataDelegate orig, SendUserCommandSystem self, LinkedList<UserCmd> sendCmdList, SnapshotsComponent snapshots, out int datalen, bool isTcp);

    public delegate byte[] SendUserCommandDataDelegate(SendUserCommandSystem self, LinkedList<UserCmd> sendCmdList, SnapshotsComponent snapshots, out int datalen, bool isTcp);
}