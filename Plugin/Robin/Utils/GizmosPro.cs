using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Plugins.Utils
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class GizmosPro
    {
        public static GizmosPro Instance
        {
            get
            {
                GizmosPro._ins ??= new GizmosPro();
                return GizmosPro._ins;
            }
        }

        private GizmoDrawer CreateGraph()
        {
            if (GizmosPro.graphIndex >= 10000)
            {
                return null;
            }
            if (GizmosPro.graphIndex >= this.graphList.Count)
            {
                this.graphList.Add(new GizmoDrawer());
            }
            return this.graphList[GizmosPro.graphIndex++];
        }

        public static void DrawCircle(Circle circle, Color color)
        {
            if (GizmosPro.Instance != null)
            {
                GizmoDrawer gizmoDrawer = GizmosPro.Instance.CreateGraph();
                gizmoDrawer?.ShowCircle(circle, color);
            }
        }

        public static void DrawRect(Rectangle rect, Color color)
        {
            if (GizmosPro.Instance != null)
            {
                GizmoDrawer gizmoDrawer = GizmosPro.Instance.CreateGraph();
                gizmoDrawer?.ShowRectangle(rect, color);
            }
        }

        public static void DrawEllipse(Ellipse ellipse, Color color)
        {
            if (GizmosPro.Instance != null)
            {
                GizmoDrawer gizmoDrawer = GizmosPro.Instance.CreateGraph();
                gizmoDrawer?.ShowEllipse(ellipse, color);
            }
        }

        public static void DrawLine(Vector2 p1, Vector2 p2, Color color)
        {
            if (GizmosPro.Instance != null)
            {
                GizmoDrawer gizmoDrawer = GizmosPro.Instance.CreateGraph();
                gizmoDrawer?.ShowLine(p1, p2, color);
            }
        }

        public static void DrawTriangle(Vector2[] p, Color color)
        {
            if (GizmosPro.Instance != null)
            {
                GizmoDrawer gizmoDrawer = GizmosPro.Instance.CreateGraph();
                gizmoDrawer?.ShowTriangle(p, color);
            }
        }

        public static void DrawCube(Vector3 center, Vector3 size, Quaternion rotation, Color color)
        {
            if (GizmosPro.Instance != null)
            {
                GizmoDrawer gizmoDrawer = GizmosPro.Instance.CreateGraph();
                gizmoDrawer?.ShowCube(center, size, rotation, color);
            }
        }

        private void CallOnGUI()
        {
            if (GizmosPro.Instance == null)
            {
                return;
            }
            for (int i = 0; i < GizmosPro.graphIndex; i++)
            {
                this.graphList[i].Draw();
            }
            GizmosPro.graphIndex = 0;
        }

        public static void InvokeOnGUI()
        {
            GizmosPro.Instance.CallOnGUI();
        }

        private readonly List<GizmoDrawer> graphList = new List<GizmoDrawer>();
        private static int graphIndex;
        private static GizmosPro _ins;
    }
}