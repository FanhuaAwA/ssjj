using System.Reflection;
using UnityEngine;

namespace Plugins.Utils
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public struct LineSegment
    {
        public LineSegment(Vector2 start, Vector2 end)
        {
            if (start.x == end.x)
            {
                A = 1f;
                B = 0f;
                C = -start.x;
                Start = start;
                End = end;
                return;
            }

            float num = (start.y - end.y) / (start.x - end.x);
            A = -num;
            B = 1f;
            C = (num * start.x) - start.y;
            Start = start;
            End = end;
        }

        public float A;
        public float B;
        public float C;
        public Vector2 Start;
        public Vector2 End;
    }
}