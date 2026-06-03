using UnityEngine;

namespace UnityEngine.Components
{
    public static class DrawingHelper
    {
        private static Texture2D _whiteTex;
        private static GUIStyle _labelStyle;

        public static Texture2D WhiteTex
        {
            get
            {
                if (_whiteTex == null)
                {
                    _whiteTex = new Texture2D(1, 1);
                    _whiteTex.SetPixel(0, 0, Color.white);
                    _whiteTex.Apply();
                }
                return _whiteTex;
            }
        }

        public static void DrawBox(float x, float y, float w, float h, Color color, float thickness = 2f)
        {
            GUI.color = color;
            GUI.DrawTexture(new Rect(x, y, w, thickness), WhiteTex);
            GUI.DrawTexture(new Rect(x, y + h - thickness, w, thickness), WhiteTex);
            GUI.DrawTexture(new Rect(x, y, thickness, h), WhiteTex);
            GUI.DrawTexture(new Rect(x + w - thickness, y, thickness, h), WhiteTex);
        }

        public static void DrawCornerBox(float x, float y, float w, float h, Color color, float cornerLen = 10f, float thickness = 2f)
        {
            GUI.color = color;
            float cl = Mathf.Min(cornerLen, w * 0.3f, h * 0.3f);
            DrawHLine(x, y, cl, thickness);
            DrawVLine(x, y, cl, thickness);
            DrawHLine(x + w - cl, y, cl, thickness);
            DrawVLine(x + w, y, cl, thickness);
            DrawHLine(x, y + h, cl, thickness);
            DrawVLine(x, y + h - cl, cl, thickness);
            DrawHLine(x + w - cl, y + h, cl, thickness);
            DrawVLine(x + w, y + h - cl, cl, thickness);
        }

        public static void DrawHLine(float x, float y, float len, float thickness)
        {
            GUI.DrawTexture(new Rect(x, y, len, thickness), WhiteTex);
        }

        public static void DrawVLine(float x, float y, float len, float thickness)
        {
            GUI.DrawTexture(new Rect(x, y, thickness, len), WhiteTex);
        }

        public static void DrawLine(Vector2 from, Vector2 to, Color color, float thickness = 1f)
        {
            GUI.color = color;
            var d = to - from;
            float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float length = d.magnitude;

            GUIUtility.RotateAroundPivot(angle, from);
            GUI.DrawTexture(new Rect(from.x, from.y, length, thickness), WhiteTex);
            GUIUtility.RotateAroundPivot(-angle, from);
        }

        public static void DrawFilledRect(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, WhiteTex);
        }

        public static void DrawLabel(Rect rect, string text, Color color, bool shadow = true)
        {
            if (shadow)
            {
                GUI.color = Color.black;
                GUI.Label(new Rect(rect.x + 1, rect.y + 1, rect.width, rect.height), text);
            }
            GUI.color = color;
            GUI.Label(rect, text);
        }

        public static void DrawLabelCentered(Rect rect, string text, Color color)
        {
            var old = GUI.skin.label.alignment;
            GUI.skin.label.alignment = TextAnchor.MiddleCenter;
            DrawLabel(rect, text, color);
            GUI.skin.label.alignment = old;
        }

        public static void DrawCircle(Vector2 center, float radius, Color color, int segments = 32)
        {
            GUI.color = color;
            float step = 360f / segments;
            for (int i = 0; i < segments; i++)
            {
                float a1 = i * step * Mathf.Deg2Rad;
                float a2 = (i + 1) * step * Mathf.Deg2Rad;
                var p1 = center + new Vector2(Mathf.Cos(a1), Mathf.Sin(a1)) * radius;
                var p2 = center + new Vector2(Mathf.Cos(a2), Mathf.Sin(a2)) * radius;
                DrawLine(p1, p2, color);
            }
        }
    }
}
