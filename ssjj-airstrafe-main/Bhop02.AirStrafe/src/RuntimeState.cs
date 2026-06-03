using UnityEngine;

namespace Bhop02;

internal static class RuntimeState
{
    public static bool Enabled;
    public static PlayerEntity SelfPlayer;

    public static void Reset()
    {
        Enabled = false;
        SelfPlayer = null;
        AirStrafe.Reset();
    }
}
