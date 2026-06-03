using System.Reflection;
using UnityEngine;

namespace Plugins.Utils
{
    [Obfuscation(Feature = "Virtualization", Exclude = false)]
    public struct Rectangle
    {
        public Vector2 Center
        {
            get => new Vector2(x, y);
            set
            {
                x = value.x;
                y = value.y;
            }
        }

        public Vector2 Size
        {
            get => new Vector2(width, height);
            set
            {
                width = value.x;
                height = value.y;
            }
        }

        public float Left => x - (width * 0.5f);

        public float Right => x + (width * 0.5f);

        public float Top => y + (height * 0.5f);

        public float Bottom => y - (height * 0.5f);

        public Rectangle(float x, float y, float width, float height)
        {
            this.x = x;
            this.y = y;
            this.width = width;
            this.height = height;
        }

        public float x { get; set; }
        public float y { get; set; }
        public float width { get; set; }
        public float height { get; set; }
    }
}