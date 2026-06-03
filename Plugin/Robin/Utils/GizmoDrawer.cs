using System.Reflection;
using UnityEngine;

namespace Plugins.Utils
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public class GizmoDrawer
    {
        public void ShowLine(Vector2 start, Vector2 end, Color color)
        {
            this.currentGraphType = GizmoDrawer.GraphType.Line;
            this.points = new Vector2[] { start, end };
            this.drawColor = color;
        }

        public void ShowCircle(Circle circle, Color color)
        {
            this.currentGraphType = GizmoDrawer.GraphType.Circle;
            this.circle = circle;
            this.drawColor = color;
        }

        public void ShowRectangle(Rectangle rect, Color color)
        {
            this.currentGraphType = GizmoDrawer.GraphType.Rectangle;
            this.rectangle = rect;
            this.drawColor = color;
        }

        public void ShowEllipse(Ellipse ellipse, Color color)
        {
            this.currentGraphType = GizmoDrawer.GraphType.Ellipse;
            this.ellipse = ellipse;
            this.drawColor = color;
        }

        public void ShowTriangle(Vector2[] vertices, Color color)
        {
            this.currentGraphType = GizmoDrawer.GraphType.Triangle;
            this.triangleVertices = vertices;
            this.drawColor = color;
        }

        public void ShowCube(Vector3 center, Vector3 size, Quaternion rotation, Color color)
        {
            this.currentGraphType = GraphType.Cube;
            this.boxCenter = center;
            this.boxSize = size;
            this.boxRotation = rotation;
            this.drawColor = color;
        }

        public void Draw()
        {
            switch (this.currentGraphType)
            {
                case GizmoDrawer.GraphType.Line:
                    this.DrawLine(this.points, this.drawColor);
                    return;

                case GizmoDrawer.GraphType.Rectangle:
                    this.DrawRectangle(this.rectangle, this.drawColor);
                    return;

                case GizmoDrawer.GraphType.Circle:
                    this.DrawCircle(this.circle, this.drawColor, 50);
                    return;

                case GizmoDrawer.GraphType.Ellipse:
                    this.DrawEllipse(this.ellipse, this.drawColor, 50);
                    return;

                case GizmoDrawer.GraphType.Triangle:
                    this.DrawTriangle(this.triangleVertices, this.drawColor);
                    return;

                case GizmoDrawer.GraphType.Cube:
                    this.DrawCube(this.boxCenter, this.boxSize, this.boxRotation, this.drawColor);
                    return;

                default:
                    return;
            }
        }

        private static void CreateLineMaterial()
        {
            if (GizmoDrawer.lineMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                GizmoDrawer.lineMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                GizmoDrawer.lineMaterial.SetInt("_SrcBlend", 5);
                GizmoDrawer.lineMaterial.SetInt("_DstBlend", 10);
                GizmoDrawer.lineMaterial.SetInt("_Cull", 0);
                GizmoDrawer.lineMaterial.SetInt("_ZWrite", 0);
            }
        }

        private static void CreateTriangleMaterial()
        {
            if (GizmoDrawer.triangleMaterial == null)
            {
                Shader shader = Shader.Find("Hidden/Internal-Colored");
                GizmoDrawer.triangleMaterial = new Material(shader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                GizmoDrawer.triangleMaterial.SetInt("_SrcBlend", 5);
                GizmoDrawer.triangleMaterial.SetInt("_DstBlend", 10);
                GizmoDrawer.triangleMaterial.SetInt("_Cull", 0);
                GizmoDrawer.triangleMaterial.SetInt("_ZWrite", 0);
            }
        }

        private void BeginDrawing(Color color, int mode = 1)
        {
            GizmoDrawer.CreateLineMaterial();
            _ = GizmoDrawer.lineMaterial.SetPass(0);
            GizmoDrawer.CreateTriangleMaterial();
            _ = GizmoDrawer.triangleMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadOrtho();
            GL.Begin(mode);
            GL.Color(color);
        }

        private void EndDrawing()
        {
            GL.End();
            GL.PopMatrix();
        }

        private void DrawLine(Vector2[] points, Color color)
        {
            this.BeginDrawing(color);
            for (int i = 0; i < points.Length; i++)
            {
                GL.Vertex3(points[i].x / Screen.width, points[i].y / Screen.height, 0f);
            }
            this.EndDrawing();
        }

        private void DrawRectangle(Rectangle rect, Color color)
        {
            this.BeginDrawing(color);
            GL.Vertex3(rect.Left / Screen.width, rect.Top / Screen.height, 0f);
            GL.Vertex3(rect.Left / Screen.width, rect.Bottom / Screen.height, 0f);
            GL.Vertex3(rect.Left / Screen.width, rect.Bottom / Screen.height, 0f);
            GL.Vertex3(rect.Right / Screen.width, rect.Bottom / Screen.height, 0f);
            GL.Vertex3(rect.Right / Screen.width, rect.Bottom / Screen.height, 0f);
            GL.Vertex3(rect.Right / Screen.width, rect.Top / Screen.height, 0f);
            GL.Vertex3(rect.Right / Screen.width, rect.Top / Screen.height, 0f);
            GL.Vertex3(rect.Left / Screen.width, rect.Top / Screen.height, 0f);
            this.EndDrawing();
        }

        private void DrawEllipse(Vector2 center, float xRadius, float yRadius, Color color, int smooth = 50)
        {
            this.BeginDrawing(color);
            for (int i = 0; i < smooth; i++)
            {
                int num = (i + 1) % smooth;
                GL.Vertex3((center.x + (xRadius * Mathf.Cos(6.2831855f / smooth * i))) / Screen.width, (center.y + (yRadius * Mathf.Sin(6.2831855f / smooth * i))) / Screen.height, 0f);
                GL.Vertex3((center.x + (xRadius * Mathf.Cos(6.2831855f / smooth * num))) / Screen.width, (center.y + (yRadius * Mathf.Sin(6.2831855f / smooth * num))) / Screen.height, 0f);
            }
            this.EndDrawing();
        }

        private void DrawCircle(Circle circle, Color color, int smooth = 50)
        {
            this.DrawEllipse(circle.Center, circle.Radius, circle.Radius, color, smooth);
        }

        private void DrawEllipse(Ellipse ellipse, Color color, int smooth = 50)
        {
            this.DrawEllipse(ellipse.Center, ellipse.XRadius, ellipse.YRadius, color, smooth);
        }

        private void DrawTriangle(Vector2[] vertices, Color color)
        {
            this.BeginDrawing(color, 4);
            foreach (Vector2 vertex in vertices)
            {
                GL.Vertex3(vertex.x / Screen.width, vertex.y / Screen.height, 0f);
            }
            this.EndDrawing();
        }

        private void DrawCube(Vector3 center, Vector3 size, Quaternion rotation, Color color)
        {
            Vector3[] localVertices = new Vector3[8];
            Vector3 extents = size * 0.5f;
            localVertices[0] = new Vector3(-extents.x, -extents.y, -extents.z);
            localVertices[1] = new Vector3(extents.x, -extents.y, -extents.z);
            localVertices[2] = new Vector3(extents.x, -extents.y, extents.z);
            localVertices[3] = new Vector3(-extents.x, -extents.y, extents.z);
            localVertices[4] = new Vector3(-extents.x, extents.y, -extents.z);
            localVertices[5] = new Vector3(extents.x, extents.y, -extents.z);
            localVertices[6] = new Vector3(extents.x, extents.y, extents.z);
            localVertices[7] = new Vector3(-extents.x, extents.y, extents.z);
            Vector3[] worldVertices = new Vector3[8];
            for (int i = 0; i < localVertices.Length; i++)
            {
                worldVertices[i] = (rotation * localVertices[i]) + center;
            }
            Vector2[] screenPoints = new Vector2[8];
            for (int i = 0; i < worldVertices.Length; i++)
            {
                Vector3 screenPos = Camera.current.WorldToScreenPoint(worldVertices[i]);
                screenPoints[i] = new Vector2(screenPos.x, screenPos.y);
            }
            this.BeginDrawing(color, 7);
            DrawLineSegment(screenPoints[0], screenPoints[1]);
            DrawLineSegment(screenPoints[1], screenPoints[2]);
            DrawLineSegment(screenPoints[2], screenPoints[3]);
            DrawLineSegment(screenPoints[3], screenPoints[0]);
            DrawLineSegment(screenPoints[4], screenPoints[5]);
            DrawLineSegment(screenPoints[5], screenPoints[6]);
            DrawLineSegment(screenPoints[6], screenPoints[7]);
            DrawLineSegment(screenPoints[7], screenPoints[4]);
            DrawLineSegment(screenPoints[0], screenPoints[4]);
            DrawLineSegment(screenPoints[1], screenPoints[5]);
            DrawLineSegment(screenPoints[2], screenPoints[6]);
            DrawLineSegment(screenPoints[3], screenPoints[7]);
            this.EndDrawing();
        }

        private void DrawLineSegment(Vector2 start, Vector2 end)
        {
            GL.Vertex3(start.x / Screen.width, start.y / Screen.height, 0f);
            GL.Vertex3(end.x / Screen.width, end.y / Screen.height, 0f);
        }

        private static Material lineMaterial;
        private Color drawColor = Color.white;
        private GizmoDrawer.GraphType currentGraphType;
        private Vector2[] points;
        private Rectangle rectangle;
        private Circle circle;
        private Ellipse ellipse;
        private Vector2[] triangleVertices;
        private static Material triangleMaterial;
        private Vector3 boxCenter;
        private Vector3 boxSize;
        private Quaternion boxRotation = Quaternion.identity;

        private enum GraphType
        {
            None,
            Line,
            Rectangle,
            Circle,
            Ellipse,
            Triangle,
            Cube
        }
    }
}