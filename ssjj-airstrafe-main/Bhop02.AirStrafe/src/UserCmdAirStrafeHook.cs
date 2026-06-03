using System;
using System.Collections.Generic;
using System.Reflection;
using Assets.Sources.Components.Snapshot;
using Assets.Sources.Snapshots;
using Assets.Sources.Systems.UserCommand;
using Assets.Sources.Utils;
using Sharpen;
using SSJJBase.Utility;
using SSJJUserCmd;
using UnityEngine;

namespace Bhop02;

internal static class UserCmdAirStrafeHook
{
    private static MethodHook _getUserCmdBytesHook;
    private static readonly BinaryDataWriter Writer = new BinaryDataWriter();
    private static bool _installed;

    public static void Install()
    {
        if (_installed)
        {
            return;
        }

        MethodInfo target = typeof(SendUserCommandSystem).GetMethod("GetUserCmdBytes", BindingFlags.Instance | BindingFlags.Public);
        MethodInfo hook = typeof(UserCmdAirStrafeHook).GetMethod(nameof(HookGetUserCmdBytes), BindingFlags.Static | BindingFlags.NonPublic);
        MethodInfo proxy = typeof(UserCmdAirStrafeHook).GetMethod(nameof(OriginalGetUserCmdBytes), BindingFlags.Static | BindingFlags.NonPublic);

        if (target == null || hook == null || proxy == null)
        {
            Debug.LogError("[Bhop02] GetUserCmdBytes hook method not found.");
            return;
        }

        _getUserCmdBytesHook = new MethodHook(target, hook, proxy);
        _getUserCmdBytesHook.Install();
        _installed = true;
        Debug.Log("[Bhop02] GetUserCmdBytes hook installed.");
    }

    public static void Uninstall()
    {
        try
        {
            _getUserCmdBytesHook?.Uninstall();
        }
        catch (Exception ex)
        {
            Debug.LogError("[Bhop02] GetUserCmdBytes hook uninstall failed: " + ex.Message);
        }
        finally
        {
            _getUserCmdBytesHook = null;
            _installed = false;
        }
    }

    private static byte[] HookGetUserCmdBytes(SendUserCommandSystem self, LinkedList<UserCmd> sendCmdList, SnapshotsComponent snapshots, out int datalen, bool isTcp)
    {
        if (!RuntimeState.Enabled || RuntimeState.SelfPlayer == null)
        {
            return OriginalGetUserCmdBytes(self, sendCmdList, snapshots, out datalen, isTcp);
        }

        datalen = 0;
        if (sendCmdList == null || sendCmdList.Count == 0)
        {
            return null;
        }

        try
        {
            return BuildUserCmdBytes(sendCmdList, snapshots, out datalen, isTcp);
        }
        catch (Exception ex)
        {
            Debug.LogError("[Bhop02] HookGetUserCmdBytes failed, fallback original: " + ex.Message);
            return OriginalGetUserCmdBytes(self, sendCmdList, snapshots, out datalen, isTcp);
        }
    }

    private static byte[] BuildUserCmdBytes(LinkedList<UserCmd> sendCmdList, SnapshotsComponent snapshots, out int datalen, bool isTcp)
    {
        datalen = 0;
        LinkedListNode<UserCmd> first = sendCmdList.First;
        UserCmd cmd = first.Value;
        short cameraYaw = cmd.CameraYaw;

        PlayerEntity player = RuntimeState.SelfPlayer;
        if (player != null && RuntimeState.Enabled)
        {
            AirStrafe.Apply(player, ref cmd);
        }

        IRecord record = SendUserCommandSystem.Record;
        bool isSelfMove = record == null || record.IsSelfMove();

        Writer.Reset();
        if (isTcp)
        {
            Writer.WriteByte((byte)31);
        }

        int latency = Math.Min(snapshots.ReceiveSnapshotLatency, 255);
        Writer.WriteByte((byte)latency);
        Writer.WriteInt(cmd.Seq);
        Writer.WriteInt(cmd.RenderTime);
        Writer.WriteInt(snapshots.LatestSnapshotSeqId);
        Writer.WriteByte((byte)191);
        Writer.WriteByte((byte)cmd.FrameInterval);
        Writer.WriteByte((byte)(isSelfMove ? (uint)(int)cmd.MoveForward : 0u));
        Writer.WriteByte((byte)(isSelfMove ? (uint)(int)cmd.MoveRight : 0u));
        Writer.WriteInt(cmd.Buttons);
        Writer.WriteByte((byte)((cmd.BagId << 4) | cmd.Weapon));
        Writer.WriteShort(cmd.CameraYaw);
        Writer.WriteShort(cmd.CameraPitch);
        cmd.CameraYaw = cameraYaw;

        UserCmd previous = cmd;
        for (first = first.Next; first != null; first = first.Next)
        {
            byte flags = 0;
            UserCmd cmd2 = first.Value;
            cameraYaw = cmd2.CameraYaw;

            if (player != null && RuntimeState.Enabled)
            {
                AirStrafe.Apply(player, ref cmd2);
            }

            int flagPosition = Writer.Position;
            Writer.WriteByte(flags);

            if (cmd2.FrameInterval != previous.FrameInterval)
            {
                flags |= 1;
                Writer.WriteByte((byte)cmd2.FrameInterval);
            }

            if (isSelfMove && (cmd2.MoveForward != previous.MoveForward || cmd2.MoveRight != previous.MoveRight))
            {
                flags |= 2;
                Writer.WriteByte((byte)(int)cmd2.MoveForward);
                Writer.WriteByte((byte)(int)cmd2.MoveRight);
            }

            if (cmd2.Buttons != previous.Buttons)
            {
                flags |= 4;
                Writer.WriteInt(cmd2.Buttons);
            }

            if (cmd2.Weapon != previous.Weapon || cmd2.BagId != previous.BagId)
            {
                flags |= 8;
                Writer.WriteByte((byte)((cmd2.BagId << 4) | cmd2.Weapon));
            }

            if (cmd2.CameraYaw != previous.CameraYaw)
            {
                flags |= 0x10;
                Writer.WriteShort(cmd2.CameraYaw);
            }

            if (cmd2.CameraPitch != previous.CameraPitch)
            {
                flags |= 0x20;
                Writer.WriteShort(cmd2.CameraPitch);
            }

            if (cmd2.RenderTime != previous.RenderTime + cmd2.FrameInterval)
            {
                int delta = cmd2.RenderTime - previous.RenderTime;
                if (Math.Abs(delta) <= 127)
                {
                    flags |= 0x40;
                    Writer.WriteByte((byte)delta);
                }
                else
                {
                    flags |= 0x80;
                    Writer.WriteInt(cmd2.RenderTime);
                }
            }

            cmd2.CameraYaw = cameraYaw;
            previous = cmd2;
            int endPosition = Writer.Position;
            Writer.Position = flagPosition;
            Writer.WriteByte(flags);
            Writer.Position = endPosition;
        }

        byte[] output = NetByteFactory.Instance.GetOrCreateNormalByte(Writer.Length, true);
        Writer.SetBytes(output);
        datalen = output.Length;
        return output;
    }

    private static byte[] OriginalGetUserCmdBytes(SendUserCommandSystem self, LinkedList<UserCmd> sendCmdList, SnapshotsComponent snapshots, out int datalen, bool isTcp)
    {
        datalen = 0;
        return null;
    }
}
