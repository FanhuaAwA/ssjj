using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Input;
using UnityEngine;

namespace Plugins.Hacks.Functions
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class MouseSimulater : IDeviceInput
    {
        public bool AnyKey()
        {
            MouseSimulater.preInput?.Invoke();
            if (!MouseSimulater.forceKey.Any((KeyValuePair<int, int> kvp) => kvp.Value != 0))
            {
                if (!MouseSimulater.forceMouse.Any((KeyValuePair<int, int> kvp) => kvp.Value != 0))
                {
                    return Input.anyKey;
                }
            }
            return true;
        }

        public static void ForceMouse(int mouseButton, MouseSimulater.InputST st)
        {
            MouseSimulater.forceMouse[mouseButton] = (int)st;
        }

        public static void ForceKey(KeyCode keyCode, MouseSimulater.InputST st)
        {
            MouseSimulater.forceKey[(int)keyCode] = (int)st;
        }

        public bool AnyKeyDown()
        {
            return MouseSimulater.forceKey.Any((KeyValuePair<int, int> kvp) => kvp.Value == 2) || Input.anyKeyDown;
        }

        public bool GetMouseButtonUp(int button)
        {
            return Input.GetMouseButtonUp(button);
        }

        public float GetAxis(string axis)
        {
            if (axis == "Mouse X")
            {
                float x = MouseSimulater.forceAxisOnce.x;
                MouseSimulater.forceAxisOnce.x = 0f;
                return Input.GetAxis(axis) + x;
            }
            if (axis == "Mouse Y")
            {
                float y = MouseSimulater.forceAxisOnce.y;
                MouseSimulater.forceAxisOnce.y = 0f;
                return Input.GetAxis(axis) + y;
            }
            return Input.GetAxis(axis);
        }

        public bool GetKey(KeyCode keyCode)
        {
            if (MouseSimulater.forceKey.TryGetValue((int)keyCode, out int num) && num > 0)
            {
                if (num == 1)
                {
                    return true;
                }
                if (num == 3)
                {
                    return false;
                }
                if (num == 2)
                {
                    MouseSimulater.forceKey[(int)keyCode] = 0;
                    return true;
                }
                if (num == 4)
                {
                    MouseSimulater.forceKey[(int)keyCode] = 0;
                    return false;
                }
            }
            return Input.GetKey(keyCode);
        }

        public bool GetKeyDown(KeyCode keyCode)
        {
            if (MouseSimulater.forceKey.TryGetValue((int)keyCode, out int num) && num > 0)
            {
                if (num == 2)
                {
                    return true;
                }
                if (num == 4)
                {
                    return false;
                }
            }
            return Input.GetKeyDown(keyCode);
        }

        public bool GetMouseButton(int button)
        {
            if (MouseSimulater.forceMouse.TryGetValue(button, out int num) && num > 0)
            {
                if (num == 1)
                {
                    return true;
                }
                if (num == 3)
                {
                    return false;
                }
                if (num == 2)
                {
                    MouseSimulater.forceMouse[button] = 0;
                    return true;
                }
                if (num == 4)
                {
                    MouseSimulater.forceMouse[button] = 0;
                    return false;
                }
            }
            return !Menu.NoKnife || Contexts.sharedInstance.player.myPlayerEntity.currentWeapon.Weapon >= 3 || button != 1 ? Input.GetMouseButton(button) : button == 0;
        }

        public bool GetMouseButtonDown(int button)
        {
            return Input.GetMouseButtonDown(button);
        }

        public static Action preInput = null;
        public static Vector2 forceAxisOnce = Vector2.zero;
        private static readonly Dictionary<int, int> forceKey = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> forceMouse = new Dictionary<int, int>();
        public static Vector2 forceAxis = Vector2.zero;

        public enum InputST
        {
            None,
            TrueKeep,
            TrueOnce,
            FalseKeep,
            FalseOnce
        }
    }
}