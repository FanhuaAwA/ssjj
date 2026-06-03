using System;
using Assets.Sources.Config;
using Assets.Sources.Utils;
using SSJJBase.Singleton;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Bhop02;

/// <summary>
/// Minimal BypassNp logic copied from the original AutoJump branch.
/// Keeps the feature narrow: disable the NP boot flags and remove ExecuteGG if it is present.
/// </summary>
internal static class NpBypass
{
    private const float ErrorLogIntervalSeconds = 3f;

    private static bool _appliedLogged;
    private static float _nextErrorLogTime;

    public static void Update()
    {
        try
        {
            bool changed = DisableNpBootFlags();
            changed |= DestroyExecuteGG();

            if (changed && !_appliedLogged)
            {
                _appliedLogged = true;
                Debug.Log("[Bhop] BypassNp applied.");
            }
        }
        catch (Exception ex)
        {
            LogErrorThrottled("[Bhop] BypassNp failed: " + ex.Message);
        }
    }

    private static bool DisableNpBootFlags()
    {
        if (TplManager.Instance == null || TplManager.Instance.GameBootConfig == null)
        {
            return false;
        }

        bool changed = false;

        if (TplManager.Instance.GameBootConfig.NpOpen)
        {
            TplManager.Instance.GameBootConfig.NpOpen = false;
            changed = true;
        }

        if (TplManager.Instance.GameBootConfig.UnityNp != 0)
        {
            TplManager.Instance.GameBootConfig.UnityNp = 0;
            changed = true;
        }

        return changed;
    }

    private static bool DestroyExecuteGG()
    {
        if ((UnityObject)(object)GameController.Instance == null)
        {
            return false;
        }

        ExecuteGG component = ((Component)GameController.Instance).gameObject.GetComponent<ExecuteGG>();
        if ((UnityObject)(object)component == null)
        {
            return false;
        }

        UnityObject.Destroy((UnityObject)(object)((Component)component).gameObject);
        return true;
    }

    private static void LogErrorThrottled(string message)
    {
        float now = Time.realtimeSinceStartup;
        if (now < _nextErrorLogTime)
        {
            return;
        }

        _nextErrorLogTime = now + ErrorLogIntervalSeconds;
        Debug.LogError(message);
    }
}

