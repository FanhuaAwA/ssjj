using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Input;
using UnityEngine;

namespace Bhop02;

/// <summary>
/// Pass-through IDeviceInput wrapper extracted from the original AutoJump FakeInput.
/// Only Space forcing is used by GroundAutoJump, but the full interface methods are kept so
/// normal keyboard/mouse input continues to pass through to UnityEngine.Input.
/// </summary>
internal sealed class FakeInput : IDeviceInput
{
    public enum InputST
    {
        None,
        TrueKeep,
        TrueOnce,
        FalseKeep,
        FalseOnce
    }

    public static Action preInput = null;

    public static Vector2 forceAxisOnce = Vector2.zero;

    private static readonly Dictionary<KeyCode, InputST> forceKey = new Dictionary<KeyCode, InputST>();

    private static readonly Dictionary<int, InputST> forceMouse = new Dictionary<int, InputST>();

    public static Vector2 forceAxis = Vector2.zero;

    public bool AnyKey()
    {
        preInput?.Invoke();
        if (!forceKey.Any((KeyValuePair<KeyCode, InputST> it) => it.Value != InputST.None) &&
            !forceMouse.Any((KeyValuePair<int, InputST> it) => it.Value != InputST.None))
        {
            return Input.anyKey;
        }
        return true;
    }

    public static void ForceMouse(int mouseButton, InputST st)
    {
        forceMouse[mouseButton] = st;
    }

    public static void ForceKey(KeyCode keyCode, InputST st)
    {
        forceKey[keyCode] = st;
    }

    public bool AnyKeyDown()
    {
        return forceKey.Any((KeyValuePair<KeyCode, InputST> it) => it.Value == InputST.TrueOnce) || Input.anyKeyDown;
    }

    public bool GetMouseButtonUp(int button)
    {
        return Input.GetMouseButtonUp(button);
    }

    public float GetAxis(string axis)
    {
        if (axis == "Mouse X")
        {
            float x = forceAxisOnce.x;
            forceAxisOnce.x = 0f;
            return Input.GetAxis(axis) + x;
        }
        if (axis == "Mouse Y")
        {
            float y = forceAxisOnce.y;
            forceAxisOnce.y = 0f;
            return Input.GetAxis(axis) + y;
        }
        return Input.GetAxis(axis);
    }

    public bool GetKey(KeyCode keyCode)
    {
        if (forceKey.TryGetValue(keyCode, out InputST value) && value != InputST.None)
        {
            switch (value)
            {
                case InputST.TrueKeep:
                    return true;
                case InputST.TrueOnce:
                    forceKey[keyCode] = InputST.None;
                    return true;
                case InputST.FalseKeep:
                    return false;
                case InputST.FalseOnce:
                    forceKey[keyCode] = InputST.None;
                    return false;
            }
        }
        return Input.GetKey(keyCode);
    }

    public bool GetKeyDown(KeyCode keyCode)
    {
        if (forceKey.TryGetValue(keyCode, out InputST value) && value != InputST.None)
        {
            switch (value)
            {
                case InputST.TrueOnce:
                    return true;
                case InputST.FalseOnce:
                    return false;
            }
        }
        return Input.GetKeyDown(keyCode);
    }

    public bool GetMouseButton(int button)
    {
        if (forceMouse.TryGetValue(button, out InputST value) && value != InputST.None)
        {
            switch (value)
            {
                case InputST.TrueKeep:
                    return true;
                case InputST.TrueOnce:
                    forceMouse[button] = InputST.None;
                    return true;
                case InputST.FalseKeep:
                    return false;
                case InputST.FalseOnce:
                    forceMouse[button] = InputST.None;
                    return false;
            }
        }
        return Input.GetMouseButton(button);
    }

    public bool GetMouseButtonDown(int button)
    {
        return Input.GetMouseButtonDown(button);
    }
}

