using Easy2D;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace RTCircles
{
    public class AnimationTVec2 : AnimationT<Vector2>
    {
        protected override Vector2 GetValueAt(double time, Vector2 start, Vector2 end, double startTime, double endTime, EasingTypes easing)
        {
            return Interpolation.ValueAt(time, start, end, startTime, endTime, easing);
        }
    }

    public class AnimationTFloat: AnimationT<float>
    {
        protected override float GetValueAt(double time, float start, float end, double startTime, double endTime, EasingTypes easing)
        {
            return Interpolation.ValueAt(time, start, end, startTime, endTime, easing);
        }
    }

    public class AnimationTVec4 : AnimationT<Vector4>
    {
        protected override Vector4 GetValueAt(double time, Vector4 start, Vector4 end, double startTime, double endTime, EasingTypes easing)
        {
            return Interpolation.ValueAt(time, start, end, startTime, endTime, easing);
        }
    }


    public abstract class AnimationT<T>
    {
        protected abstract T GetValueAt(double time, T start, T end, double startTime, double endTime, EasingTypes easing);

        public T DefaultValue { get; set; } = default(T);

        public (double startTime, double endTime)? Duration
        {
            get
            {
                if (animStates.Count > 0)
                {
                    double startTime = animStates[0].Time;
                    double endTime = animStates[^1].Time;

                    return (startTime, endTime);
                }

                return null;
            }
        }

        public T GetOutputAtTime(double time)
        {
            if (animStates.Count < 2)
                return DefaultValue;

            for (int i = 1; i < animStates.Count; i++)
            {
                var prev = animStates[i - 1];
                var now = animStates[i];

                if (time > prev.Time && time < now.Time)
                    return GetValueAt(time, prev.EndValue, now.EndValue, prev.Time, now.Time, now.EasingType);
            }

            if (time >= animStates[^1].Time)
                return animStates[^1].EndValue;
            else
                return animStates[0].EndValue;
        }

        private struct AnimState
        {
            public double Time;
            public T EndValue;
            public EasingTypes EasingType;
        }

        private List<AnimState> animStates = new List<AnimState>();

        public void Add(double time, T endValue, EasingTypes easing = EasingTypes.None)
        {
            animStates.Add(new AnimState() { EasingType = easing, Time = time, EndValue = endValue });
            animStates.Sort((a, b) => a.Time.CompareTo(b.Time));
        }

        public void Wait(double time)
        {
            T endValue = animStates.Count > 0 ? animStates[^1].EndValue : DefaultValue;

            Add(time, endValue, EasingTypes.None);
        }

        public void Clear() => animStates.Clear();
    }
}
