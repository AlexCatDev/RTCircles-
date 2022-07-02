using Easy2D;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace RTCircles
{
    public class AnimFloat : Anim<float>
    {
        protected override float GetValueAt(double currentTime, double startTime, double endTime, float start, float end, EasingTypes easing)
        {
            return Interpolation.ValueAt(currentTime, start, end, startTime, endTime, easing);
        }
    }

    public class AnimVector2 : Anim<Vector2>
    {
        protected override Vector2 GetValueAt(double currentTime, double startTime, double endTime, Vector2 start, Vector2 end, EasingTypes easing)
        {
            return Interpolation.ValueAt(currentTime, start, end, startTime, endTime, easing);
        }
    }

    public class AnimVector4 : Anim<Vector4>
    {
        protected override Vector4 GetValueAt(double currentTime, double startTime, double endTime, Vector4 start, Vector4 end, EasingTypes easing)
        {
            return Interpolation.ValueAt(currentTime, start, end, startTime, endTime, easing);
        }
    }


    public abstract class Anim<T>
    {
        private delegate (bool hasCompleted, Action onComplete) TransformFunction(double currentTime);

        private T currentValue;

        /// <summary>
        /// From 0 to 1 how far along is the current transform
        /// </summary>
        public double PercentageCompleted { get; private set; }
        /// <summary>
        /// This gets incremented by one everytime a new transform has begun, not when added
        /// </summary>
        public int CurrentIndex { get; private set; }

        /// <summary>
        /// Get the current value, setting this value will clear all pending transforms, and instantaneously set the new value
        /// </summary>
        public T Value
        {
            get
            {
                return currentValue;
            }
            set
            {
                ClearTransforms();
                currentValue = value;
            }
        }

        private double time;
        public double Time
        {
            get
            {
                return time;
            }
            set
            {
                time = value;

                if (currentTransform is null && pendingTransforms.Count > 0)
                    currentTransform = pendingTransforms.Dequeue();

                if (currentTransform is not null)
                {
                    var status = currentTransform(time);

                    if (status.hasCompleted)
                    {
                        status.onComplete?.Invoke();
                        PercentageCompleted = 0;
                        currentTransform = null;
                    }
                }
            }
        }

        public static implicit operator T(Anim<T> t) => t.currentValue;

        protected abstract T GetValueAt(double currentTime, double startTime, double endTime, T start, T end, EasingTypes easing);

        public int PendingTransformCount => pendingTransforms.Count;

        public bool HasCompleted => PendingTransformCount == 0 && currentTransform == null;

        private Queue<TransformFunction> pendingTransforms = new Queue<TransformFunction>();
        private TransformFunction currentTransform;

        public Anim<T> TransformTo(T value, double startTime, double endTime, EasingTypes easing = EasingTypes.None, Action onComplete = null)
        {
            bool initStart = false;
            T start = currentValue;

            pendingTransforms.Enqueue((currentTime) => {
                if (!initStart)
                {
                    start = currentValue;
                    initStart = true;
                    ++CurrentIndex;
                }

                currentTime = currentTime.Clamp(startTime, endTime);
                currentValue = GetValueAt(currentTime, startTime, endTime, start, value, easing);
                PercentageCompleted = currentTime.Map(startTime, endTime, 0, 1);

                return (currentTime == endTime, onComplete);
            });

            return this;
        }

        public Anim<T> Wait(double time, Action onComplete = null)
        {
            pendingTransforms.Enqueue((currentTime) =>
            {
                return (currentTime >= time, onComplete);
            });

            return this;
        }

        public void ClearTransforms()
        {
            pendingTransforms.Clear();
            currentTransform = null;
        }

        public override string ToString()
        {
            return
                $"Pending: {pendingTransforms.Count} Value: {currentValue}";
        }
    }


    public class SmoothVector2 : Smooth<Vector2>
    {
        protected override Vector2 GetValueAt(float elapsed, Vector2 start, Vector2 end, float duration, EasingTypes easing)
        {
            return Interpolation.ValueAt(elapsed, start, end, 0, duration, easing);
        }
    }

    public class SmoothVector4 : Smooth<Vector4>
    {
        protected override Vector4 GetValueAt(float elapsed, Vector4 start, Vector4 end, float duration, EasingTypes easing)
        {
            return Interpolation.ValueAt(elapsed, start, end, 0, duration, easing);
        }
    }

    public class SmoothFloat : Smooth<float>
    {
        protected override float GetValueAt(float elapsed, float start, float end, float duration, EasingTypes easing)
        {
            return Interpolation.ValueAt(elapsed, start, end, 0, duration, easing);
        }
    }

    public abstract class Smooth<T>
    {
        private delegate (bool hasCompleted, Action onComplete) TransformFunction(float delta);

        private T currentValue;

        /// <summary>
        /// Get the current value, setting this value will clear all pending transforms, and instantaneously set the new value
        /// </summary>
        public T Value
        {
            get
            {
                return currentValue;
            }
            set
            {
                ClearTransforms();
                currentValue = value;
            }
        }

        public static implicit operator T(Smooth<T> t) => t.currentValue;

        protected abstract T GetValueAt(float elapsed, T start, T end, float duration, EasingTypes easing);

        public int PendingTransformCount => pendingTransforms.Count;

        public bool HasCompleted => PendingTransformCount == 0 && currentTransform == null;

        private Queue<TransformFunction> pendingTransforms = new Queue<TransformFunction>();
        private TransformFunction currentTransform;

        public void EndCurrentTransform()
        {
            if (currentTransform is not null)
            {
                var status = currentTransform(float.MaxValue);

                System.Diagnostics.Debug.Assert(status.hasCompleted);

                status.onComplete?.Invoke();
                currentTransform = null;
            }
        }

        public Smooth<T> TransformTo(T value, float duration = 0, EasingTypes easing = EasingTypes.None, Action onComplete = null)
        {
            float elapsed = 0;

            bool initStart = false;
            T start = currentValue;

            pendingTransforms.Enqueue((delta) =>
            {
                if (!initStart)
                {
                    start = currentValue;
                    initStart = true;
                }

                elapsed += delta;
                elapsed = elapsed.Clamp(0, duration);

                currentValue = GetValueAt(elapsed, start, value, duration, easing);

                return (elapsed == duration, onComplete);
            });

            return this;
        }

        public Smooth<T> Wait(float duration, Action onComplete = null)
        {
            float elapsed = 0;

            pendingTransforms.Enqueue((delta) =>
            {
                elapsed += delta;
                return (elapsed >= duration, onComplete);
            });

            return this;
        }

        public void ClearTransforms()
        {
            pendingTransforms.Clear();
            currentTransform = null;
        }

        public void Update(float delta)
        {
            if (currentTransform is null && pendingTransforms.Count > 0)
                currentTransform = pendingTransforms.Dequeue();

            if (currentTransform is not null)
            {
                var status = currentTransform(delta);

                if (status.hasCompleted)
                {
                    status.onComplete?.Invoke();
                    currentTransform = null;
                }
            }
        }

        public override string ToString()
        {
            return
                $"Pending: {pendingTransforms.Count} Value: {currentValue}";
        }
    }
}
