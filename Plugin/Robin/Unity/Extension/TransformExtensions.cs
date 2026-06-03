using System.Reflection;
using UnityEngine;

namespace Plugins.Unity.Extension
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public static class TransformExtensions
    {
        public static Transform FindChildDeep(this Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }
            Transform transform = parent.Find(childName);
            if (transform != null)
            {
                return transform;
            }
            foreach (object obj in parent)
            {
                Transform transform2 = (Transform)obj;
                transform = transform2.FindChildDeep(childName);
                if (transform != null)
                {
                    return transform;
                }
            }
            return null;
        }

        public static string GetHierarchyPath(this Transform transform)
        {
            return !(transform.parent != null) ? transform.name : transform.parent.GetHierarchyPath() + "/" + transform.name;
        }

        public static Vector3 GetUIPosition(this Transform transform)
        {
            return !(transform == null) ? transform.position.ToUIPosition() : Vector3.zero;
        }

        public static Vector3 ToUIPosition(this Vector3 worldPosition)
        {
            Camera main = Camera.main;
            return !(main != null) ? Vector3.zero : main.WorldToScreenPoint(worldPosition);
        }
    }
}