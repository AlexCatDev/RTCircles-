using Easy2D;
using System.Numerics;
using System;
using System.Collections.Generic;

namespace RTCircles
{
    //God im retarded
    public class AutoGenerator
    {
        public struct AutoFrame
        {
            public Vector2 Position;
            public double Time;
            public bool IsSliderSlide;
        }

        private List<AutoFrame> frames = new List<AutoFrame>();

        private int frameIndex = 0;
        private AutoFrame? first;

        public Vector2 CurrentPosition { get; private set; } = new Vector2(512, 384) / 2;

        private Vector2 onFrame(double time, AutoFrame from, AutoFrame to, int index)
        {
            bool cursorDance = GlobalOptions.AutoCursorDance.Value;
            
            if (!cursorDance && to.IsSliderSlide && DrawableSlider.SliderBallPositionForAuto.HasValue)
                return DrawableSlider.SliderBallPositionForAuto.Value;

            float blend = Interpolation.ValueAt(time, 0, 1, from.Time, to.Time);

            var easing = ((from.Position - to.Position).Length() < OsuContainer.Beatmap.CircleRadiusInOsuPixels * 2 || cursorDance) ? EasingTypes.None : EasingTypes.InOutQuad;

            var output = Vector2.Lerp(from.Position, to.Position, Interpolation.ValueAt(blend, 0, 1, 0, 1, easing));

            if (!cursorDance)
                return output;

            float length = (from.Position - to.Position).Length() / 2;

            if (length < OsuContainer.Beatmap.CircleRadiusInOsuPixels)
                return output;

            blend = Interpolation.ValueAt(time, 0, 1, from.Time, to.Time);

            Vector2 danceVec = new Vector2(
                (float)Math.Sin(blend.Map(0, 1, MathF.PI, 0)), 
                (float)Math.Cos(blend.Map(0, 1, MathF.PI / 2, -MathF.PI / 2))) * length;

            float angle = MathUtils.AtanVec(from.Position, to.Position);

            if (index % 2 == 0)
            {
                output += danceVec;
            }
            else
            {
               output -= danceVec;
            }
            return output;
        }

        public void Sort()
        {
            frames.Sort((x, y) => { return x.Time.CompareTo(y.Time); });
        }

        public void SyncToTime(double time)
        {
            first = null;

            if (time < frames[0].Time)
            {
                frameIndex = 0;
                return;
            }

            if (time > frames[frames.Count - 1].Time)
            {
                frameIndex = frames.Count - 1;
                return;
            }

            frameIndex = frames.FindIndex((o) => o.Time > time) - 1;

            if (frameIndex < 0)
                frameIndex = frames.Count - 1;
        }

        public void Reset()
        {
            frameIndex = 0;
            first = null;
        }

        public void End()
        {
            frameIndex = frames.Count;
            first = null;
        }

        public void Update()
        {
            double currentTime = OsuContainer.SongPosition;

            if (frameIndex >= frames.Count)
                return;

            if (first == null)
                first = new AutoFrame() { Time = currentTime, Position = CurrentPosition };

            while (currentTime > frames[frameIndex].Time)
            {
                ++frameIndex;

                if (frameIndex >= frames.Count)
                    return;
            }

            bool hasPreviousIndex = frameIndex - 1 > -1;

            currentTime = Math.Min(currentTime, frames[frameIndex].Time);

            CurrentPosition = onFrame(currentTime, hasPreviousIndex ? frames[frameIndex - 1] : first.Value, frames[frameIndex], frameIndex);
        }

        public void AddDestination(Vector2 destination, double time, bool isSliderSlide)
        {
            frames.Add(new AutoFrame() { Position = destination, Time = time, IsSliderSlide = isSliderSlide });
        }
    }
}