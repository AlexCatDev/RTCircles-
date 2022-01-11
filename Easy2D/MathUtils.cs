using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Easy2D
{
    public static class MathUtils
    {
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

        public static float HypotF(float x, float y) => MathF.Sqrt((x * x) + (y * y));

        public static Vector2 Lerp(Vector2 start, Vector2 end, float t) => new Vector2(MathHelper.Lerp(start.X, end.X, t), MathHelper.Lerp(start.Y, end.Y, t));

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

        public static bool PositionInsideRadius(Vector2 point1, Vector2 point2, float radius)
        {
            var distance = HypotF(point1.X - point2.X , point1.Y - point2.Y);
            if (distance < 2 + radius / 2)
                return true;

            return false;
        }

        public static float GetAngleFromOrigin(Vector2 origin, Vector2 target)
        {
            var n = 270f - (MathF.Atan2(origin.Y - target.Y, origin.X - target.X)) * 180 / MathF.PI;
            return n % 360;
        }

        public static Vector2 RotateAroundOrigin(Vector2 point, Vector2 origin, float degrees)
        {
            float radians = (float)ToRadians(degrees);
            float sin = MathF.Sin(radians);
            float cos = MathF.Cos(radians);

            // Translate point back to origin
            point.X -= origin.X;
            point.Y -= origin.Y;

            // Rotate point
            float xnew = point.X * cos - point.Y * sin;
            float ynew = point.X * sin + point.Y * cos;

            // Translate point back
            Vector2 newPoint = new Vector2(xnew + origin.X, ynew + origin.Y);
            return newPoint;
        }

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
        public static float Map(this float value, float fromSource, float toSource, float fromTarget, float toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

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
