using UnityEngine;

namespace Bhop02;

public static class Entry
{
    private const string GameObjectName = "[Bhop02.AirStrafe]";

    public static void Load()
    {
        if (Object.FindObjectOfType<Bhop02Controller>() != null)
        {
            Debug.Log("[Bhop02] Already loaded.");
            return;
        }

        GameObject gameObject = new GameObject(GameObjectName);
        gameObject.AddComponent<Bhop02Controller>();
        Object.DontDestroyOnLoad(gameObject);
        Debug.Log("[Bhop02] FakeInput bhop + UserCmd air-strafe loaded.");
    }

    public static void Unload()
    {
        Bhop02Controller component = Object.FindObjectOfType<Bhop02Controller>();
        if (component != null)
        {
            Object.DestroyImmediate(component.gameObject);
        }

        UserCmdAirStrafeHook.Uninstall();
        RuntimeState.Reset();
        FakeInput.ForceKey(KeyCode.Space, FakeInput.InputST.None);
        Debug.Log("[Bhop02] Unloaded.");
    }
}
