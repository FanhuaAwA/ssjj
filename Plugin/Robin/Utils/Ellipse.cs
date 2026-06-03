using System.Reflection;
using UnityEngine;

namespace Plugins.Utils
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public struct Ellipse
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

        public Ellipse(Vector2 center, float width, float height)
        {
            this.centerX = center.x;
            this.centerY = center.y;
            this.ellipseWidth = width;
            this.ellipseHeight = height;
        }

        public float XRadius => 0.5f * this.ellipseWidth;
        public float YRadius => 0.5f * this.ellipseHeight;
        public float centerX;
        public float centerY;
        public float ellipseWidth;
        public float ellipseHeight;
    }
}