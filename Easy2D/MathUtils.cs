using OpenTK.Mathematics;
using System;
using System.Runtime.InteropServices;

namespace Easy2D
{
    public static class MathUtils
    {
        public static Vector3 RainbowColor(double time, float saturation = 1, float brightness = 1)
        {
            time /= 10;
            float hue = (float)(time % 1);

            var color = (Vector4)Color4.FromHsv(new Vector4(hue, saturation, brightness, 1));
            return color.Xyz;
        }

        public static double ToDegrees(double radians)
        {
            return radians / 0.017453292519943295769236907684886;
        }

        public static double ToRadians(double degrees)
        {
            return degrees * 0.017453292519943295769236907684886;
        }

        public static int GetSize<T>(this T t) where T : unmanaged
        {
            return Marshal.SizeOf<T>();
        }

        public static int GetSize<T>(this T[] t) where T : unmanaged
        {
            return Marshal.SizeOf<T>() * t.Length;
        }

        public static float OscillateValue(float input, float min, float max)
        {
            var range = max - min;
            return min + MathF.Abs(((input + range) % (range * 2)) - range);
        }

        public static double OscillateValue(this double input, double min, double max)
        {
            var range = max - min;
            return min + Math.Abs(((input + range) % (range * 2)) - range);
        }

        public static bool IsPointInsideRadius(Vector2 point1, Vector2 point2, float radius) => (point1 - point2).LengthFast < radius;

        public static float AtanVec(Vector2 pos1, Vector2 pos2) => MathF.Atan2(pos1.Y - pos2.Y, pos1.X - pos2.X); 

        public static float GetAngleFromOrigin(Vector2 origin, Vector2 target, float degreesOffset = 0)
        {
            var n = 270f - (MathF.Atan2(origin.Y - target.Y, origin.X - target.X) + (float)ToRadians(degreesOffset)) * 180 / MathF.PI;
            return n % 360;
        }

        public static Vector2 RotateAroundOrigin(Vector2 point, Vector2 origin, float radians)
        {
            float sin = MathF.Sin(radians);
            float cos = MathF.Cos(radians);

            // Translate point back to origin
            point.X -= origin.X;
            point.Y -= origin.Y;

            Vector2 newPoint;

            // Rotate point, then translate it back
            newPoint.X = (point.X * cos - point.Y * sin) + origin.X;
            newPoint.Y = (point.X * sin + point.Y * cos) + origin.Y;

            return newPoint;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="vector2"></param>
        /// <returns>Returns the ratio x divided by y</returns>
        public static float AspectRatio(this Vector2 vector2) => vector2.X / vector2.Y;

        public static Vector2 Center(this Vector2 vector2) => vector2 / 2;

        /// <summary>
        /// The most useful math function on the planet HOLY SHIT
        /// </summary>
        /// <param name="value"></param>
        /// <param name="fromSource"></param>
        /// <param name="toSource"></param>
        /// <param name="fromTarget"></param>
        /// <param name="toTarget"></param>
        /// <returns></returns>
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static float Map(this float value, float fromSource, float toSource, float fromTarget, float toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static double Map(this double value, double fromSource, double toSource, double fromTarget, double toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        public static float Clamp(this float value, float min, float max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;

            return value;
        }


        public static void ClampRef(this ref float value, float min, float max)
        {
            if (value < min)
                value = min;
            if(value > max)
                value = max;
        }

        public static void MapRef(this ref float value, float fromSource, float toSource, float fromTarget, float toTarget)
        {
            value = (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        public static double Clamp(this double value, double min, double max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;

            return value;
        }

        public static int Clamp(this int value, int min, int max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;

            return value;
        }
    }
}
