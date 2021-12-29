﻿using Easy2D;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps.Objects;
using OsuParsers.Enums.Beatmaps;
using Silk.NET.Input;
using System;
using System.Collections.Generic;

namespace RTCircles
{
    public class DrawableSlider : Drawable, IDrawableHitObject
    {
        public static float SliderResolution = 1f;

        public static bool SnakeIn = true;
        public static bool SnakeOut = true;
        public static bool Explode = true;

        private Slider slider;

        private Vector2 Size => new Vector2(OsuContainer.Beatmap.CircleRadius * 2);

        private Vector4 color => new Vector4(Skin.Config.ColorFromIndex(colorIndex), 1f);

        private BetterSlider sliderPath = new BetterSlider();

        private int repeatsDone = 0;
        private int lastRepeatsDone = 0;

        private SmoothFloat sliderBallScaleAnim = new SmoothFloat();

        public override Rectangle Bounds => sliderPath.Path.Bounds;

        public HitObject BaseObject => slider;

        public Vector4 CurrentColor => color;

        private int colorIndex;
        private int combo;

        public DrawableSlider(Slider slider, int colorIndex, int combo)
        {
            repeatsDone = 0;
            this.slider = slider;
            this.colorIndex = colorIndex;
            this.combo = combo;

            parseControlPoints();
        }

        private void parseControlPoints()
        {
            List<Vector2> fullPath = new List<Vector2>();

            List<Vector2> buffer = new List<Vector2>();

            void compileSubPath()
            {
                if (buffer.Count > 0)
                {
                    switch (slider.CurveType)
                    {
                        case CurveType.Catmull:
                            fullPath.AddRange(PathApproximator.ApproximateCatmull(buffer));
                            break;
                        case CurveType.Bezier:
                            fullPath.AddRange(PathApproximator.ApproximateBezier(buffer));
                            break;
                        case CurveType.Linear:
                            fullPath.AddRange(PathApproximator.ApproximateLinear(buffer));
                            break;
                        case CurveType.PerfectCurve:
                            if (buffer.Count != 3)
                                fullPath.AddRange(PathApproximator.ApproximateBezier(buffer));
                            else
                                fullPath.AddRange(PathApproximator.ApproximateCircularArc(buffer));
                            break;
                    }

                    buffer.Clear();
                }
            }

            buffer.Add(new Vector2(slider.Position.X, slider.Position.Y));

            //Go through each slider control points
            for (int i = 0; i < slider.SliderPoints.Count; i++)
            {
                buffer.Add(new Vector2(slider.SliderPoints[i].X, slider.SliderPoints[i].Y));

                var now = slider.SliderPoints[i];
                var next = slider.SliderPoints[Math.Min(i + 1, slider.SliderPoints.Count - 1)];
                //If now and next are the same points, its a reset-path-point, or it might be the end of the list!
                //Either way we have to "compile" it.
                if (next == now)
                    compileSubPath();
            }

            //if no points for some reason, aspire mapping :woozy_face: just add the start point
            if (fullPath.Count == 0)
                fullPath.Add(new Vector2(slider.Position.X, slider.Position.Y));

            //The radius in osu pixels
            float sizeInOsuSpace = 54.4f - 4.48f * OsuContainer.Beatmap.CS;

            //Defining the pointsize in path, makes it know how big to calculate the bounding box
            //The pointsize is in osu pixels from the current circlesize
            sliderPath.SetPoints(fullPath, sizeInOsuSpace);
        }

        private float snakeIn = 1f;

        private bool fadeout;

        private float circleAlpha;

        private float approachRing;

        private void drawSliderRepeat(Graphics g, Vector2 position, float angle, int index)
        {
            angle = MathHelper.RadiansToDegrees(angle);

            //basically offset the start time according to if it's the head of the tail of the slider
            //by offsetting the start time with what a slider repeat is worth in time
            //times an index
            float diff = (slider.EndTime - slider.StartTime) / slider.Repeats;

            Vector2 beatPulse = new Vector2(OsuContainer.Beatmap.CircleRadius * 0.75f) * (float)OsuContainer.GetBeatProgressAt(slider.StartTime + (diff * index));

            Vector2 size = new Vector2(Size.Y, Size.Y / Skin.SliderReverse.Texture.Size.AspectRatio()) * Skin.GetScale(Skin.SliderReverse);

            g.DrawRectangleCentered(position, size + beatPulse, new Vector4(1f, 1f, 1f, circleAlpha), Skin.SliderReverse, null, false, angle);
        }

        private void drawSliderRepeatHead(Graphics g)
        {
            //Get the angle of the first and second point. so we can rotate the sliderRepeat towards that for the head
            var first = sliderPath.Path.Points[0];
            var next = sliderPath.Path.Points[1];

            first = OsuContainer.MapToPlayfield(first.X, first.Y);
            next = OsuContainer.MapToPlayfield(next.X, next.Y);

            var angle = MathF.Atan2(next.Y - first.Y, next.X - first.X);

            drawSliderRepeat(g, first, angle, 2);
        }

        private void drawSliderRepeatTail(Graphics g)
        {
            //Get the angle of the last and second last point. so we can rotate the sliderRepeat towards that for the tail
            var last = sliderPath.Path.Points[sliderPath.Path.Points.Count - 1];
            var secondLast = sliderPath.Path.Points[sliderPath.Path.Points.Count - 2];

            last = OsuContainer.MapToPlayfield(last.X, last.Y);
            secondLast = OsuContainer.MapToPlayfield(secondLast.X, secondLast.Y);

            var angle = MathF.Atan2(secondLast.Y - last.Y, secondLast.X - last.X);

            drawSliderRepeat(g, last, angle, 1);
        }

        private Vector2 sliderballPosition;
        private Vector2 previousBallPos;


        private void drawSliderBall(Graphics g)
        {
            drawSliderRepeats(g);

            float current = (float)OsuContainer.SongPosition - slider.StartTime;
            float to = (slider.EndTime - slider.StartTime);

            float progress = MathUtils.Map(current, 0, to, 0, 1f).Clamp(0, 1);

            float unCapped = MathUtils.Map(current, 0, to, 0, 1f);

            float osc = MathUtils.OscillateValue(progress * slider.Repeats, 0f, 1f);

            Vector2 pos = sliderPath.Path.CalculatePositionAtProgress(osc);
            sliderballPosition = OsuContainer.MapToPlayfield(pos.X, pos.Y);

            //Uncapped but still only render sliderball when it's within range exlusive, since uncap just overflows above 1
            //If i used progress here since progress is capped, the sliderball would stay visible for the entire duration of the slider
            //Either that or i have to basically throw away a frame at the end if only checking for < 1f
            //so uncap makes sense here, to not miss a frame
            if (IsHit == true || IsMissed == true)
            {
                if (OsuContainer.SongPosition < slider.EndTime)
                {
                    float sliderBallAngle = MathF.Atan2(pos.Y - previousBallPos.Y, pos.X - previousBallPos.X);
                    g.DrawRectangleCentered(sliderballPosition, Size * Skin.GetScale(Skin.SliderBall), new Vector4(1f, 1f, 1f, 1f), Skin.SliderBall, rotDegrees: MathHelper.RadiansToDegrees(sliderBallAngle));

                    previousBallPos = pos;
                }

                float followCircleAlpha = sliderBallScaleAnim.Value.Map(1f, 2f, 0f, 1) * circleAlpha;

                float fadeoutScale = Interpolation.ValueAt(circleAlpha.Clamp(0.5f, 1f), 1f, 0.75f, 1f, 0.5f, EasingTypes.OutQuad).Clamp(0.75f, 1f);

                Vector2 followCircleSize = Size * sliderBallScaleAnim * Skin.GetScale(Skin.SliderFollowCircle, 256, 512) * fadeoutScale;

                g.DrawRectangleCentered(sliderballPosition, followCircleSize, new Vector4(1f, 1f, 1f, followCircleAlpha), Skin.SliderFollowCircle);
            }

            drawHitCircle(g);
        }

        private void drawSliderRepeats(Graphics g)
        {
            //The way i've done slider repeats are very confusing to me 
            var repeatsToGo = slider.Repeats - repeatsDone;

            if (repeatsToGo > 1 && snakeIn == 1f)
            {
                if (slider.Repeats % 2 == 0)
                {
                    drawSliderRepeatTail(g);

                    if (repeatsToGo > 2)
                        drawSliderRepeatHead(g);
                }
                else
                {
                    drawSliderRepeatHead(g);

                    if (repeatsToGo > 2)
                        drawSliderRepeatTail(g);
                }
            }
        }

        private Vector2 hitCirclePos;
        private void drawHitCircle(Graphics g)
        {
            if (IsMissed == false)
            {
                float hitCircleAlpha = circleAlpha;
                float scaleExplode = 1f;
                if (hitTime.HasValue)
                {
                    hitCircleAlpha = (float)OsuContainer.SongPosition.Map(hitTime.Value, hitTime.Value + OsuContainer.Fadeout, circleAlpha, 0).Clamp(0, 1f);
                    scaleExplode = (float)OsuContainer.SongPosition.Map(hitTime.Value, hitTime.Value + OsuContainer.Fadeout, 1, OsuContainer.CircleExplode);
                }
                else
                {
                    //Only move along the hitcircle when it hasnt been hit.
                    hitCirclePos = sliderballPosition;
                }

                g.DrawRectangleCentered(hitCirclePos, Size * Skin.GetScale(Skin.SliderStartCircle) * scaleExplode, new Vector4(color.X, color.Y, color.Z, hitCircleAlpha), Skin.SliderStartCircle);

                g.DrawRectangleCentered(hitCirclePos, Size * Skin.GetScale(Skin.SliderStartCircleOverlay) * scaleExplode, new Vector4(1f, 1f, 1f, hitCircleAlpha), Skin.SliderStartCircleOverlay);

                if(approachRing > 1f)
                    g.DrawRectangleCentered(hitCirclePos, Size * approachRing * Skin.GetScale(Skin.ApproachCircle), new Vector4(color.X, color.Y, color.Z, hitCircleAlpha), Skin.ApproachCircle);
            }

            //Circle numbers dont need to fade or explode, just dissappear instantly
            if(IsHit == false && IsMissed == false)
                Skin.CircleNumbers.DrawCentered(g, hitCirclePos, Size.X / 2.7f, new Vector4(1f, 1f, 1f, circleAlpha), combo.ToString());
        }

        private bool checkHit()
        {
            if (IsHit == false && IsMissed == false)
            {
                //Use the sliderball position, for the hitcircle position.
                if (MathUtils.PositionInsideRadius(OsuContainer.CursorPosition, sliderballPosition, OsuContainer.Beatmap.CircleRadius * 2) || OsuContainer.CookieziMode)
                {
                    double hittableTime = Math.Abs(OsuContainer.SongPosition - slider.StartTime);

                    if (circleAlpha < 0.6f)
                        return true;

                    if (hittableTime > OsuContainer.Beatmap.Window50)
                    {
                        IsMissed = true;
                        OsuContainer.HUD.AddHit((float)hittableTime, HitResult.Miss, sliderballPosition, true);
                        return true;
                    }

                    IsHit = true;
                    hitTime = OsuContainer.SongPosition;
                    var hitsound = slider.EdgeHitSounds?[0] ?? slider.HitSound;
                    var sample = slider.EdgeAdditions?[0].Item1 ?? slider.Extras.SampleSet;
                    OsuContainer.PlayHitsound(hitsound, sample);
                    OsuContainer.HUD.AddHit((float)hittableTime, HitResult.Max, sliderPath.Path.Points[0], false);

                    return true;
                }
            }

            return false;
        }

        public override bool OnKeyDown(Key key)
        {
            if (key == OsuContainer.Key1 || key == OsuContainer.Key2)
                return checkHit();

            return false;
        }

        public override bool OnMouseDown(MouseButton args)
        {
            if (args == MouseButton.Left && OsuContainer.EnableMouseButtons)
                return checkHit();

            return false;
        }

        public override bool OnMouseUp(MouseButton args)
        {
            return false;
        }

        public override bool OnKeyUp(Key key)
        {
            return false;
        }

        private bool IsTracking => (OsuContainer.Key1Down || OsuContainer.Key2Down) && MathUtils.PositionInsideRadius(OsuContainer.CursorPosition, sliderballPosition, OsuContainer.Beatmap.CircleRadius * 4f) || OsuContainer.CookieziMode;

        //Allow 36ms releasing the slider too early
        private const double TrackingErrorAcceptance = 36;
        private bool IsValidTrack => (OsuContainer.SongPosition - lastTrackingTime) <= TrackingErrorAcceptance || OsuContainer.CookieziMode;

        private bool previousTracking;

        private double lastTrackingTime = 0;

        public override void OnRemove()
        {
            //Remember to delete slider framebuffer when not on screen because they use vram
            //What happens if this gets called, but then it gets added right after?
            //It should work fine fingers crossed lol
            GPUScheduler.Run(new(() =>
            {
                sliderPath.DeleteFramebuffer();
            }));
        }

        private bool IsHit;
        private bool IsMissed;
        private bool IsEndingHit;

        private double? hitTime;

        public override void OnAdd()
        {
            IsHit = false;
            IsMissed = false;
            IsEndingHit = false;

            circleAlpha = 0f;
            approachRing = 0f;
            repeatsDone = 0;
            lastRepeatsDone = 0;
            snakeIn = 0f;
            fadeout = false;
            sliderBallScaleAnim.Value = 1f;
            lastTrackingTime = 0;
            previousTracking = false;
            hitTime = null;
        }

        public override void Update(float delta)
        {
            if ((this as IDrawableHitObject).TimeElapsed < 0)
            {
                IsDead = true;
                return;
            }

            if (IsTracking != previousTracking && IsHit || IsMissed)
            {
                previousTracking = IsTracking;

                if (IsTracking)
                {
                    sliderBallScaleAnim.ClearTransforms();
                    sliderBallScaleAnim.TransformTo(2f, (float)OsuContainer.Fadeout, EasingTypes.OutQuad);
                }
            }

            if (IsTracking)
                lastTrackingTime = OsuContainer.SongPosition;

            if (OsuContainer.SongPosition < slider.EndTime)
                sliderBallScaleAnim.Update((float)OsuContainer.DeltaSongPosition);

            //yikes
            if(IsTracking && (Skin.SliderSlide.PlaybackPosition > Skin.SliderSlide.PlaybackLength - 50f || Skin.SliderSlide.IsPlaying == false) && OsuContainer.SongPosition > slider.StartTime && OsuContainer.SongPosition < slider.EndTime && (IsHit || IsMissed))
            {
               Skin.SliderSlide.Play(true);
            }

            float timeElapsed = (float)(OsuContainer.SongPosition - slider.StartTime + OsuContainer.Beatmap.Preempt);

            float fadeOutStart = slider.EndTime;

            //Neeed a better way to handle this
            if (fadeout)
            {
                circleAlpha = MathUtils.Map((float)OsuContainer.SongPosition, fadeOutStart, fadeOutStart + (float)OsuContainer.Fadeout, 1f, 0).Clamp(0, 1f);
            }
            else
            {
                approachRing = MathUtils.Map(timeElapsed, 0, (float)OsuContainer.Beatmap.Preempt, (float)OsuContainer.ApproachCircleScale, 1f);
                approachRing = MathUtils.Clamp(approachRing, 1f, (float)OsuContainer.ApproachCircleScale);

                if (approachRing == 1f && OsuContainer.CookieziMode)
                {
                    checkHit();
                }

                circleAlpha = MathUtils.Map(timeElapsed, 0, (float)OsuContainer.Beatmap.Fadein, 0, 1f).Clamp(0, 1f);

                snakeIn = SnakeIn ? MathUtils.Map(timeElapsed, 0, (float)OsuContainer.Beatmap.Fadein / 3f, 0, 1f).Clamp(0, 1f) : 1;
            }

            //Slider snakeout module
            //Im suprised i could fit this in my head, 1 month ago, i had no fucking clue
            //How i would do this, and solved it in like 7 minutes now? easy
            float snakeOut = 0f;
            if (repeatsDone >= slider.Repeats - 1 && SnakeOut)
            {
                double duration = slider.EndTime - slider.StartTime;
                double startSnake = slider.Repeats > 1 ? slider.EndTime - (duration / slider.Repeats) : slider.StartTime;
                double endSnake = slider.EndTime;

                if (slider.Repeats % 2 != 0)
                    snakeOut = (float)MathUtils.Map(OsuContainer.SongPosition, startSnake, endSnake, 0, 1).Clamp(0, 1);
                else
                    snakeIn = (float)MathUtils.Map(OsuContainer.SongPosition, startSnake, endSnake, 1, 0f).Clamp(0, 1);
            }

            repeatsDone = (int)MathUtils.Map(OsuContainer.SongPosition, slider.StartTime, slider.EndTime, 0, slider.Repeats).Clamp(0, slider.Repeats);

            //I dont understand hitsounds fully, so this is pretty bs need to redo
            if (repeatsDone != lastRepeatsDone)
            {
                lastRepeatsDone = repeatsDone;

                //var hitsound = slider.EdgeHitSounds?[1] ?? slider.HitSound;
                //var sample = slider.EdgeAdditions?[1].Item1 ?? slider.Extras.SampleSet;

                var hitsound = slider.EdgeHitSounds?[repeatsDone] ?? slider.HitSound;
                var sample = slider.EdgeAdditions?[repeatsDone].Item1 ?? slider.Extras.SampleSet;

                SampleSet? sample2 = slider.EdgeAdditions?[repeatsDone].Item2;

                if (IsHit == false && IsMissed == false)
                {
                    IsMissed = true;
                    OsuContainer.HUD.AddHit(0, HitResult.Miss, sliderballPosition, false);
                }

                //Last slider point check
                if (slider.Repeats - repeatsDone == 0)
                {
                    //The rules are:
                    //If there was a miss within the slider
                    //Grant a 50

                    //If no miss within the slider
                    //grant a 300

                    Skin.SliderSlide.Stop();

                    HitResult result = HitResult.Miss;

                    if (IsValidTrack)
                        IsEndingHit = true;

                    //If it somehow hasnt been hit yet due to large hitwindow mark it as missed
                    if (IsMissed)
                    {
                        if (IsValidTrack)
                            result = HitResult.Good;
                        else
                            result = HitResult.Miss;
                    }
                    else
                    {
                        if (IsValidTrack)
                            result = HitResult.Max;
                        else
                            result = HitResult.Good;
                    }

                    if (result != HitResult.Miss)
                    {
                        OsuContainer.PlayHitsound(hitsound, sample);

                        if (sample2.HasValue)
                            OsuContainer.PlayHitsound(hitsound, sample2.Value);
                    }

                    OsuContainer.HUD.AddHit(0, result, sliderballPosition);
                }
                else if (IsValidTrack)
                {
                    OsuContainer.PlayHitsound(hitsound, sample);

                    if (sample2.HasValue)
                        OsuContainer.PlayHitsound(hitsound, sample2.Value);

                    OsuContainer.HUD.AddHit(0, HitResult.Max, sliderballPosition, false);
                }
                else
                {
                    IsMissed = true;
                    OsuContainer.HUD.AddHit(0, HitResult.Miss, sliderballPosition, false);
                }
            }

            //Fix only explode when there are no misses on the slider
            //This whole piece here is for exploding the slider itself like it was a circle
            if (fadeout && (IsHit || IsEndingHit) && Explode)
            {
                sliderPath.ScalingOrigin = sliderballPosition;
                sliderPath.DrawScale = circleAlpha.Map(1f, 0f, 1f, (float)OsuContainer.CircleExplode);
            }
            else
            {
                sliderPath.ScalingOrigin = null;
            }

            sliderPath.Alpha = circleAlpha;

            sliderPath.SetProgress(snakeOut, snakeIn);

            if (OsuContainer.SongPosition > slider.StartTime + OsuContainer.Beatmap.Window50 && !IsHit && !IsMissed)
            {
                OsuContainer.HUD.AddHit((float)OsuContainer.Beatmap.Window50, HitResult.Miss, sliderballPosition, true);
                IsMissed = true;
            }

            if (fadeout && circleAlpha == 0)
            {
                IsDead = true;
            }

            //Instead of making it like this, maybe i should map the fadein time so that when it overflows it fades out?
            //probaly not since i need to check for when a user is done playing the slider anyways
            //And they can start it very early or very late
            if (OsuContainer.SongPosition >= slider.EndTime)
            {
                fadeout = true;
            }
        }

        public override void Render(Graphics g)
        {
            Vector3 sliderBorder = new Vector3(Skin.Config.SliderBorder ?? new Vector3(1f, 1f, 1f));
            Vector3 sliderTrack = new Vector3(Skin.Config.SliderTrackOverride ?? Skin.Config.ColorFromIndex(colorIndex));

            g.ShadowColor = new Vector4(0, 0, 0, 0.5f);

            g.BorderColorOuter = sliderBorder;
            g.BorderColorInner = sliderBorder;

            g.TrackColorOuter = Shade(-0.1f, new Vector4(sliderTrack, 1.0f)).Xyz;
            g.TrackColorInner = Shade(0.5f, new Vector4(sliderTrack, 1.0f)).Xyz;

            sliderPath.Render(g);

            if (circleAlpha > 0)
            {
                drawSliderBall(g);
            }
        }

        private Vector4 Shade(float amount, Vector4 c)
        {
            if(amount < 0)
                return Darken(-amount, c);

                return Lighten(amount, c);
        }

        private Vector4 Darken(float amount, Vector4 c) {
            float scale = MathF.Max(1.0f, 1.0f + amount);

            return new Vector4(c.X / scale, c.Y / scale, c.Z / scale, c.W);
        }

        private Vector4 Lighten(float amount, Vector4 c)
        {
            amount *= 0.5f;

            float scale = 1.0f + 0.5f * amount;


        return new Vector4(
            MathF.Min(1.0f, c.X * scale + amount),
            MathF.Min(1.0f, c.Y * scale + amount),
            MathF.Min(1.0f, c.Z * scale + amount), c.W);
        }
    }
}

//jeg skal redo det her lort