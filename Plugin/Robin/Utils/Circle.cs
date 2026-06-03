using System.Reflection;
using UnityEngine;

namespace Plugins.Utils
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public struct Circle
    {
        public Vector2 Center
        {
            get => new Vector2(this.centerX, this.centerY);
            set
            {
                this.centerX = value.x;
                this.centerY = value.y;
            }
        }

        public float Radius { get; set; }

        public Circle(Vector2 centerPos, float radius)
        {
            this.centerX = centerPos.x;
            this.centerY = centerPos.y;
            this.Radius = radius;
        }

        public static Circle operator *(Circle circle, float scalar)
        {
            return new Circle(circle.Center, circle.Radius * scalar);
        }

        public float centerX;
        public float centerY;
    }
}