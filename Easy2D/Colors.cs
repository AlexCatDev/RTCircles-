using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Easy2D
{
    public static class Colors
    {
        public static readonly Vector4 Red = new Vector4(1f, 0f, 0f, 1f);
        public static readonly Vector4 Green = new Vector4(0f, 1f, 0f, 1f);
        public static readonly Vector4 Blue = new Vector4(0f, 0f, 1f, 1f);
        public static readonly Vector4 Yellow = new Vector4(1f, 1f, 0f, 1f);

        public static readonly Vector4 White = new Vector4(1f, 1f, 1f, 1f);
        public static readonly Vector4 LightGray = new Vector4(0.8f, 0.8f, 0.8f, 1f);
        public static readonly Vector4 Black = new Vector4(0f, 0f, 0f, 1f);

        public static readonly Vector4 Pink = new Vector4(255f / 255f, 102f / 255f, 170f / 255f, 1f);
        public static readonly Vector4 DarkPink = new Vector4(255f / 255f, 77f / 255f, 157f / 255f, 1f);

        public static Vector4 From255RGBA(float r, float g, float b, float a) => new Vector4(r / 255f, g / 255f, b / 255f, a / 255f);

        public static Vector4 Tint(Vector4 color, float maxValue)
        {
            float ratio = 1.0f;
            if(color.X > color.Y && color.X > color.Z)
                ratio = maxValue / color.X;
            else if (color.Y > color.X && color.Y > color.Z)
                ratio = maxValue / color.Y;
            else if (color.Z > color.X && color.Z > color.Y)
                ratio = maxValue / color.Z;
            else
                ratio = maxValue / color.X;

            color.X *= ratio;
            color.Y *= ratio;
            color.Z *= ratio;

            return color;
        }
    }
}
