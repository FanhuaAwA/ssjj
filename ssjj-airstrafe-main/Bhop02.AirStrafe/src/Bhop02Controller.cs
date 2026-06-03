using System;
using Assets.Scripts.Input;
using UnityEngine;

namespace Bhop02;

internal sealed class Bhop02Controller : MonoBehaviour
{
    // Space toggles the feature. When enabled:
    // - FakeInput emits Space on the first grounded frames.
    // - UserCmd hook applies air-strafe while airborne.
    private const int GroundPressFrames = 2;

    private bool _inputInstalled;
    private bool _hookInstalled;
    private bool _wasOnGround;
    private int _groundFrames;

    private void Update()
    {
        try
        {
            NpBypass.Update();
            EnsureInputInstalled();
            EnsureUserCmdHookInstalled();
            HandleToggleHotkey();
            RuntimeState.SelfPlayer = ResolveSelfPlayer();

            PlayerEntity player = RuntimeState.SelfPlayer;
            if (!RuntimeState.Enabled || player == null || player.IsDead())
            {
                ResetJumpState();
                return;
            }

            HandleOnGroundFakeJump(player);
        }
        catch (Exception ex)
        {
            FakeInput.ForceKey(KeyCode.Space, FakeInput.InputST.None);
            Debug.LogError("[Bhop02] Update failed: " + ex.Message);
        }
    }

    private void EnsureInputInstalled()
    {
        if (_inputInstalled)
        {
            return;
        }

        InputCollector.Instance.SetDeviceInput((IDeviceInput)(object)new FakeInput());
        _inputInstalled = true;
        Debug.Log("[Bhop02] FakeInput installed. Press physical Space to toggle bhop + air-strafe.");
    }

    private void EnsureUserCmdHookInstalled()
    {
        if (_hookInstalled)
        {
            return;
        }

        UserCmdAirStrafeHook.Install();
        _hookInstalled = true;
    }

    private void HandleToggleHotkey()
    {
        // Use UnityEngine.Input directly so FakeInput's forced Space does not toggle itself.
        if (!Input.GetKeyDown(KeyCode.Space))
        {
            return;
        }

        RuntimeState.Enabled = !RuntimeState.Enabled;
        ResetJumpState();
        AirStrafe.Reset();
        Debug.Log("[Bhop02] " + (RuntimeState.Enabled ? "ENABLED" : "DISABLED") + " by Space.");
    }

    private static PlayerEntity ResolveSelfPlayer()
    {
        try
        {
            if (!Contexts.sharedInstance.player.isMyPlayer)
            {
                return null;
            }

            return Contexts.sharedInstance.player.myPlayerEntity;
        }
        catch
        {
            return null;
        }
    }

    private void HandleOnGroundFakeJump(PlayerEntity player)
    {
        bool onGround = player.OnGround();

        if (onGround)
        {
            if (!_wasOnGround)
            {
                _groundFrames = 0;
            }

            _groundFrames++;
            FakeInput.ForceKey(KeyCode.Space, _groundFrames <= GroundPressFrames ? FakeInput.InputST.TrueOnce : FakeInput.InputST.None);
        }
        else
        {
            _groundFrames = 0;
            FakeInput.ForceKey(KeyCode.Space, FakeInput.InputST.None);
        }

        _wasOnGround = onGround;
    }

    private void ResetJumpState()
    {
        _wasOnGround = false;
        _groundFrames = 0;
        FakeInput.ForceKey(KeyCode.Space, FakeInput.InputST.None);
    }

    private void OnDestroy()
    {
        RuntimeState.Reset();
        ResetJumpState();
        if (_hookInstalled)
        {
            UserCmdAirStrafeHook.Uninstall();
            _hookInstalled = false;
        }
    }
}
