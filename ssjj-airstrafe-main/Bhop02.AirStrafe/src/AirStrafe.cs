using System;
using SSJJUserCmd;
using UnityEngine;

namespace Bhop02;

internal static class AirStrafe
{
    private static bool _strafeFlip;
    private static float _lastForwardWish = 100f;
    private const float MinSpeedThreshold = 1f;

    public static void Reset()
    {
        _strafeFlip = false;
        _lastForwardWish = 100f;
    }

    public static void Apply(PlayerEntity player, ref UserCmd cmd)
    {
        if (player == null || player.OnGround())
        {
            return;
        }

        // Do not keep jump bit latched while already airborne.
        if ((cmd.Buttons & 0x20) != 0)
        {
            cmd.Buttons &= ~0x20;
        }

        Vector3 velocity = player.move.Velocity;
        velocity.z = 0f;
        float speed = HorizontalSpeedXY(velocity);

        float forwardWish = 0f;
        float rightWish = 0f;

        if (Input.GetKey((KeyCode)119)) forwardWish += 100f; // W
        if (Input.GetKey((KeyCode)115)) forwardWish -= 100f; // S
        if (Input.GetKey((KeyCode)100)) rightWish += 100f;   // D
        if (Input.GetKey((KeyCode)97)) rightWish -= 100f;    // A

        if (forwardWish != 0f)
        {
            _lastForwardWish = forwardWish;
        }

        bool hasWishInput = forwardWish != 0f || rightWish != 0f;
        float baseForwardWish = forwardWish != 0f ? forwardWish : (rightWish != 0f ? _lastForwardWish : _lastForwardWish);
        float baseRightWish = hasWishInput ? rightWish : 0f;

        float cameraYaw = cmd.CameraYaw / 100f;
        float desiredYaw = cameraYaw;
        if (baseForwardWish != 0f || baseRightWish != 0f)
        {
            desiredYaw += Mathf.Atan2(0f - baseRightWish, baseForwardWish) * Mathf.Rad2Deg;
        }

        cmd.MoveForward = 0f;
        cmd.MoveRight = 0f;

        Vector3 velocityAngles = default(Vector3);
        DirectionToPitchYaw(velocity, ref velocityAngles);
        float velocityYaw = velocityAngles.y;
        float delta = NormalizeAngle180(desiredYaw - velocityYaw);
        float absDelta = Mathf.Abs(delta);

        // Yinyi-style dynamic accel: stronger at low angle error, softer at large error.
        float accel = Mathf.Lerp(60f, 30f, Mathf.Clamp01(absDelta / 90f));
        float maxStrafeAngle = GetMaxStrafeAngle(speed, accel);

        if (absDelta > maxStrafeAngle && speed > 15f)
        {
            float sign = Mathf.Sign(delta);
            desiredYaw = velocityYaw - sign * maxStrafeAngle;
            cmd.MoveRight = delta > 0f ? -100f : 100f;
        }
        else
        {
            _strafeFlip = !_strafeFlip;
            float side = _strafeFlip ? 1f : -1f;
            desiredYaw = velocityYaw + maxStrafeAngle * side;
            cmd.MoveRight = 100f * side;
        }

        Vector3 moveDir = new Vector3(cmd.MoveForward, cmd.MoveRight, 0f);
        Vector3 moveAngles = default(Vector3);
        DirectionToPitchYaw(moveDir, ref moveAngles);
        float finalMoveAngle = NormalizeAngle180(cameraYaw - desiredYaw + moveAngles.y) * Mathf.Deg2Rad;

        cmd.MoveForward = Mathf.Clamp(Mathf.Cos(finalMoveAngle) * 100f, -100f, 100f);
        cmd.MoveRight = Mathf.Clamp(Mathf.Sin(finalMoveAngle) * 100f, -100f, 100f);
    }

    private static float HorizontalSpeedXY(Vector3 v)
    {
        return Mathf.Sqrt(v.x * v.x + v.y * v.y);
    }

    private static float GetMaxStrafeAngle(float speed, float accel)
    {
        speed = Mathf.Max(speed, MinSpeedThreshold);
        return Mathf.Clamp(Mathf.Atan(accel / speed) * Mathf.Rad2Deg, 0f, 90f);
    }

    private static float NormalizeAngle180(float angleDeg)
    {
        if (float.IsInfinity(angleDeg) || float.IsNaN(angleDeg) || angleDeg > 9999999f || angleDeg < -9999999f)
        {
            return 0f;
        }

        while (angleDeg < -180f) angleDeg += 360f;
        while (angleDeg > 180f) angleDeg -= 360f;
        return angleDeg;
    }

    private static void DirectionToPitchYaw(Vector3 dir, ref Vector3 outAnglesDeg)
    {
        Vector3 angles = default(Vector3);
        if (dir.x == 0f && dir.y == 0f)
        {
            angles.x = 0f;
            angles.y = 0f;
        }
        else
        {
            angles.y = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            if (angles.y < 0f)
            {
                angles.y += 360f;
            }

            angles.z = Mathf.Sqrt(dir.x * dir.x + dir.y * dir.y);
            angles.x = Mathf.Atan2(dir.z, angles.z) * Mathf.Rad2Deg;
        }

        outAnglesDeg.x = 0f - angles.x;
        outAnglesDeg.y = angles.y;
        outAnglesDeg.z = 0f;
    }
}
