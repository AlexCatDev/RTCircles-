using System.Numerics;
using System;
using System.Runtime.InteropServices;

namespace Easy2D
{
    public static class MathUtils
    {
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 RainbowColor(double progress, float saturation = 1, float brightness = 1)
        {
            float hue = (float)(progress % 1);

            return Colors.FromHsv(new Vector3(hue, saturation, brightness));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static float ToDegrees(this float radians)
        {
            return radians * 57.2957795131f;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static float ToRadians(this float degrees)
        {
            return degrees * 0.01745329251f;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static double ToDegrees(this double radians)
        {
            return radians * 57.2957795131;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static double ToRadians(this double degrees)
        {
            return degrees * 0.01745329251;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static float OscillateValue(this float input, float min, float max)
        {
            var range = max - min;
            return min + MathF.Abs(((input + range) % (range * 2)) - range);
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static double OscillateValue(this double input, double min, double max)
        {
            var range = max - min;
            return min + Math.Abs(((input + range) % (range * 2)) - range);
        }

        public static bool IsPointInsideRadius(Vector2 point1, Vector2 point2, float radius) => (point1 - point2).Length() < radius;

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

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static float Clamp(this float value, float min, float max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;

            return value;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void ClampRef(this ref float value, float min, float max)
        {
            if (value < min)
                value = min;
            else if(value > max)
                value = max;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void MapRef(this ref float value, float fromSource, float toSource, float fromTarget, float toTarget)
        {
            value = (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static double Clamp(this double value, double min, double max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;

            return value;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static int Clamp(this int value, int min, int max)
        {
            if (value > max)
                return max;
            if (value < min)
                return min;

            return value;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static Vector3 Xyz(this Vector4 value) => new Vector3(value.X, value.Y, value.Z);
    }
}
