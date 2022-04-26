// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.

using OpenTK.Mathematics;
using System;

namespace Easy2D
{
    public static class Interpolation
    {
        public static double Damp(double start, double final, double smoothing, double delta)
        {
            return MathHelper.Lerp(start, final, 1 - (float)Math.Pow(smoothing, delta));
        }

        public static Vector4 ValueAt(double time, Vector4 val1, Vector4 val2, double startTime, double endTime, EasingTypes easing = EasingTypes.None)
        {
            float progress = ValueAt(time, 0, 1, startTime, endTime, easing);

            return Vector4.Lerp(val1, val2, progress);
        }

        public static Vector2 ValueAt(double time, Vector2 val1, Vector2 val2, double startTime, double endTime, EasingTypes easing = EasingTypes.None)
        {
            float progress = ValueAt(time, 0, 1, startTime, endTime, easing);

            return Vector2.Lerp(val1, val2, progress);
        }

        public static float ValueAt(double time, float val1, float val2, double startTime, double endTime, EasingTypes easing = EasingTypes.None)
        {
            double current = time - startTime;
            double duration = endTime - startTime;

            return (float)ApplyEasing(easing, current, val1, val2 - val1, duration);
        }

        public static double ValueAt(double time, double val1, double val2, double startTime, double endTime, EasingTypes easing = EasingTypes.None)
        {
            double current = time - startTime;
            double duration = endTime - startTime;

            return ApplyEasing(easing, current, val1, val2 - val1, duration);
        }

        public static double ApplyEasing(EasingTypes easing, double time, double value, double change, double duration)
        {
            if (change == 0 || time == 0 || duration == 0) return value;
            if (time == duration) return value + change;
            //if (time > Math.Abs(duration)) return value + change;

            switch (easing)
            {
                default:
                    return change * (time / duration) + value;
                case EasingTypes.In:
                case EasingTypes.InQuad:
                    return change * (time /= duration) * time + value;
                case EasingTypes.Out:
                case EasingTypes.OutQuad:
                    return -change * (time /= duration) * (time - 2) + value;
                case EasingTypes.InOutQuad:
                    if ((time /= duration / 2) < 1) return change / 2 * time * time + value;
                    return -change / 2 * ((--time) * (time - 2) - 1) + value;
                case EasingTypes.InCubic:
                    return change * (time /= duration) * time * time + value;
                case EasingTypes.OutCubic:
                    return change * ((time = time / duration - 1) * time * time + 1) + value;
                case EasingTypes.InOutCubic:
                    if ((time /= duration / 2) < 1) return change / 2 * time * time * time + value;
                    return change / 2 * ((time -= 2) * time * time + 2) + value;
                case EasingTypes.InQuart:
                    return change * (time /= duration) * time * time * time + value;
                case EasingTypes.OutQuart:
                    return -change * ((time = time / duration - 1) * time * time * time - 1) + value;
                case EasingTypes.InOutQuart:
                    if ((time /= duration / 2) < 1) return change / 2 * time * time * time * time + value;
                    return -change / 2 * ((time -= 2) * time * time * time - 2) + value;
                case EasingTypes.InQuint:
                    return change * (time /= duration) * time * time * time * time + value;
                case EasingTypes.OutQuint:
                    return change * ((time = time / duration - 1) * time * time * time * time + 1) + value;
                case EasingTypes.InOutQuint:
                    if ((time /= duration / 2) < 1) return change / 2 * time * time * time * time * time + value;
                    return change / 2 * ((time -= 2) * time * time * time * time + 2) + value;
                case EasingTypes.InSine:
                    return -change * Math.Cos(time / duration * (Math.PI / 2)) + change + value;
                case EasingTypes.OutSine:
                    return change * Math.Sin(time / duration * (Math.PI / 2)) + value;
                case EasingTypes.InOutSine:
                    return -change / 2 * (Math.Cos(Math.PI * time / duration) - 1) + value;
                case EasingTypes.InExpo:
                    return change * Math.Pow(2, 10 * (time / duration - 1)) + value;
                case EasingTypes.OutExpo:
                    return (time == duration) ? value + change : change * (-Math.Pow(2, -10 * time / duration) + 1) + value;
                case EasingTypes.InOutExpo:
                    if ((time /= duration / 2) < 1) return change / 2 * Math.Pow(2, 10 * (time - 1)) + value;
                    return change / 2 * (-Math.Pow(2, -10 * --time) + 2) + value;
                case EasingTypes.InCirc:
                    return -change * (Math.Sqrt(1 - (time /= duration) * time) - 1) + value;
                case EasingTypes.OutCirc:
                    return change * Math.Sqrt(1 - (time = time / duration - 1) * time) + value;
                case EasingTypes.InOutCirc:
                    if ((time /= duration / 2) < 1) return -change / 2 * (Math.Sqrt(1 - time * time) - 1) + value;
                    return change / 2 * (Math.Sqrt(1 - (time -= 2) * time) + 1) + value;
                case EasingTypes.InElastic:
                    {
                        if ((time /= duration) == 1) return value + change;

                        var p = duration * .3;
                        var a = change;
                        double s;
                        if (a < Math.Abs(change))
                        {
                            a = change;
                            s = p / 4;
                        }
                        else
                            s = p / (2 * Math.PI) * Math.Asin(change / a);
                        return -(a * Math.Pow(2, 10 * (time -= 1)) * Math.Sin((time * duration - s) * (2 * Math.PI) / p)) + value;
                    }
                case EasingTypes.OutElastic:
                    {
                        if ((time /= duration) == 1) return value + change;

                        var p = duration * .3;
                        var a = change;
                        double s;
                        if (a < Math.Abs(change))
                        {
                            a = change;
                            s = p / 4;
                        }
                        else s = p / (2 * Math.PI) * Math.Asin(change / a);
                        return a * Math.Pow(2, -10 * time) * Math.Sin((time * duration - s) * (2 * Math.PI) / p) + change + value;
                    }
                case EasingTypes.OutElasticHalf:
                    {
                        if ((time /= duration) == 1) return value + change;

                        var p = duration * .3;
                        var a = change;
                        double s;
                        if (a < Math.Abs(change))
                        {
                            a = change;
                            s = p / 4;
                        }
                        else s = p / (2 * Math.PI) * Math.Asin(change / a);
                        return a * Math.Pow(2, -10 * time) * Math.Sin((0.5f * time * duration - s) * (2 * Math.PI) / p) + change + value;
                    }
                case EasingTypes.OutElasticQuarter:
                    {
                        if ((time /= duration) == 1) return value + change;

                        var p = duration * .3;
                        var a = change;
                        double s;
                        if (a < Math.Abs(change))
                        {
                            a = change;
                            s = p / 4;
                        }
                        else s = p / (2 * Math.PI) * Math.Asin(change / a);
                        return a * Math.Pow(2, -10 * time) * Math.Sin((0.25f * time * duration - s) * (2 * Math.PI) / p) + change + value;
                    }
                case EasingTypes.InOutElastic:
                    {
                        if ((time /= duration / 2) == 2) return value + change;

                        var p = duration * (.3 * 1.5);
                        var a = change;
                        double s;
                        if (a < Math.Abs(change))
                        {
                            a = change;
                            s = p / 4;
                        }
                        else s = p / (2 * Math.PI) * Math.Asin(change / a);
                        if (time < 1) return -.5 * (a * Math.Pow(2, 10 * (time -= 1)) * Math.Sin((time * duration - s) * (2 * Math.PI) / p)) + value;
                        return a * Math.Pow(2, -10 * (time -= 1)) * Math.Sin((time * duration - s) * (2 * Math.PI) / p) * .5 + change + value;
                    }
                case EasingTypes.InBack:
                    {
                        var s = 1.70158;
                        return change * (time /= duration) * time * ((s + 1) * time - s) + value;
                    }
                case EasingTypes.OutBack:
                    {
                        var s = 1.70158;
                        return change * ((time = time / duration - 1) * time * ((s + 1) * time + s) + 1) + value;
                    }
                case EasingTypes.InOutBack:
                    {
                        var s = 1.70158;
                        if ((time /= duration / 2) < 1) return change / 2 * (time * time * (((s *= 1.525) + 1) * time - s)) + value;
                        return change / 2 * ((time -= 2) * time * (((s *= 1.525) + 1) * time + s) + 2) + value;
                    }
                case EasingTypes.InBounce:
                    return change - ApplyEasing(EasingTypes.OutBounce, duration - time, 0, change, duration) + value;
                case EasingTypes.OutBounce:
                    if ((time /= duration) < 1 / 2.75)
                    {
                        return change * (7.5625 * time * time) + value;
                    }
                    if (time < 2 / 2.75)
                    {
                        return change * (7.5625 * (time -= 1.5 / 2.75) * time + .75) + value;
                    }
                    if (time < 2.5 / 2.75)
                    {
                        return change * (7.5625 * (time -= 2.25 / 2.75) * time + .9375) + value;
                    }
                    return change * (7.5625 * (time -= 2.625 / 2.75) * time + .984375) + value;
                case EasingTypes.InOutBounce:
                    if (time < duration / 2) return ApplyEasing(EasingTypes.InBounce, time * 2, 0, change, duration) * .5 + value;
                    return ApplyEasing(EasingTypes.OutBounce, time * 2 - duration, 0, change, duration) * .5 + change * .5 + value;
            }
        }
    }
}
