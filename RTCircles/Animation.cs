using Easy2D;

namespace RTCircles
{
    public class Animation
    {
        public EasingTypes Easing { get; set; } = EasingTypes.None;

        private float elapsedTime;

        public float Output { get; private set; }

        public float From { get; set; }
        public float To { get; set; }

        public float Duration { get; set; }

        public bool IsCompleted => IsReversed ? elapsedTime == 0 : elapsedTime == Duration;

        public bool IsReversed { get; set; }

        public bool IsPaused { get; set; }

        public void Reset()
        {
            elapsedTime = 0;
        }

        public void Update(float delta)
        {
            if (!IsPaused)
            {
                if (IsReversed)
                    delta *= -1f;
                elapsedTime = MathUtils.Clamp(elapsedTime + delta, 0, Duration);
            }

            Output = Interpolation.ValueAt(elapsedTime, From, To, 0, Duration, Easing);
        }
    }
}
