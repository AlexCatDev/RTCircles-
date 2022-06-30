using Easy2D;
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
        public static bool SliderOnScreen;

        //TODO: add
        public static float SliderResolution = 1f;

        public static Vector2? SliderBallPositionForAuto { get; private set; }

        private Slider slider;

        private Vector2 Size => new Vector2(OsuContainer.Beatmap.CircleRadius * 2);

        private Vector4 color => new Vector4(Skin.Config.ColorFromIndex(colorIndex), 1f);

        private Vector2 hitCirclePos;

        public readonly ISlider SliderPath;

        private int repeatsDone = 0;
        private int lastRepeatsDone = 0;

        private SmoothFloat sliderFollowScaleAnim = new SmoothFloat();

        public override Rectangle Bounds => SliderPath.Path.Bounds;

        public HitObject BaseObject => slider;

        public Vector4 CurrentColor => color;

        private int colorIndex;
        private int combo;

        public DrawableSlider(Slider slider, int colorIndex, int combo)
        {
            if (GlobalOptions.UseFastSliders.Value)
                SliderPath = new FastSlider();
            else
                SliderPath = new BetterSlider();

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
                    //2 point buffer is always linear
                    if (buffer.Count == 2)
                        fullPath.AddRange(buffer);
                    else
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
                                fullPath.AddRange(buffer);
                                break;
                            case CurveType.PerfectCurve:
                                if (buffer.Count != 3)
                                    fullPath.AddRange(PathApproximator.ApproximateBezier(buffer));
                                else
                                    fullPath.AddRange(PathApproximator.ApproximateCircularArc(buffer));
                                break;
                        }
                    }
                    buffer.Clear();
                }
            }

            //buffer.Add(new Vector2(slider.Position.X, slider.Position.Y));

            //Go through each slider control points
            for (int i = 0; i < slider.SliderPoints.Count; i++)
            {
                var now = slider.SliderPoints[i];
                var next = slider.SliderPoints[Math.Min(i + 1, slider.SliderPoints.Count - 1)];

                buffer.Add(new Vector2(slider.SliderPoints[i].X, slider.SliderPoints[i].Y));

                //If now and next are the same points, its a reset-path-point, or it might be the end of the list!
                //Either way we have to "compile" it.
                if (next == now)
                    compileSubPath();
            }

            //if no points for some reason, aspire mapping :woozy_face: just add the start point
            if (fullPath.Count == 0)
                fullPath.Add(new Vector2(slider.Position.X, slider.Position.Y));


            #region Slider trimming to pixel length
            float length = (float)slider.PixelLength;
            for (int i = 0; i < fullPath.Count - 1; i++)
            {
                float dist = Vector2.Distance(fullPath[i], fullPath[i + 1]);

                if (length - dist <= 0)
                {
                    float blend = length / dist;
                    var finalPointAdjusted = Vector2.Lerp(fullPath[i], fullPath[i + 1], blend);

                    fullPath.RemoveRange(i + 1, fullPath.Count - i - 1);
                    fullPath.Add(finalPointAdjusted);
                    /*
                    var finalLength = Path.CalculateLength(fullPath);
                    System.Diagnostics.Debug.Assert(finalLength - 10 < slider.PixelLength);
                    Console.WriteLine($"Final: {finalLength} Actual: {slider.PixelLength}");
                    */
                    break;
                }
                length -= dist;
            }
            #endregion

            //Set the sliderpath to the points.
            SliderPath.SetPoints(fullPath);
        }

        private float snakeIn = 0;

        private bool fadeout;

        private float circleAlpha;

        private float approachRing;

        private void drawSliderRepeat(Graphics g, Vector2 position, float angle, int index, float alpha)
        {
            angle = MathHelper.RadiansToDegrees(angle);

            //basically offset the start time according to if it's the head of the tail of the slider
            //by offsetting the start time with what a slider repeat is worth in time
            //times an index
            float diff = (slider.EndTime - slider.StartTime) / slider.Repeats;

            float beatProgress = (float)Interpolation.ValueAt(OsuContainer.GetBeatProgressAt(slider.StartTime + (diff * index)), OsuContainer.CircleExplodeScale, 1, 1, 0, EasingTypes.OutSine);

            //float beatProgress = (float)Interpolation.ValueAt(OsuContainer.GetBeatCountFrom(slider.StartTime + (diff * index), 0.5).OscillateValue(0, 1), 1, OsuContainer.CircleExplodeScale, 1, 0, EasingTypes.Out);

            Vector2 size = new Vector2(Size.Y, Size.Y / Skin.SliderReverse.Texture.Size.AspectRatio()) * Skin.GetScale(Skin.SliderReverse);

            g.DrawRectangleCentered(position, size * beatProgress, new Vector4(1f, 1f, 1f, alpha), Skin.SliderReverse, null, false, angle);
        }

        private void drawSliderRepeatHead(Graphics g)
        {
            //Get the angle of the first and second point. so we can rotate the sliderRepeat towards that for the head
            var first = SliderPath.Path.Points[0];
            var next = SliderPath.Path.Points[1];

            first = OsuContainer.MapToPlayfield(first.X, first.Y);
            next = OsuContainer.MapToPlayfield(next.X, next.Y);

            var angle = MathF.Atan2(next.Y - first.Y, next.X - first.X);

            float repeatTime = (slider.EndTime - slider.StartTime) / slider.Repeats;

            var startTime = slider.StartTime;
            //Either 400 ms fadein or fully faded in when it's just returning
            var endTime = startTime + Math.Min(repeatTime * 1, 400);
            
            float alpha = Interpolation.ValueAt(OsuContainer.SongPosition.Clamp(startTime, endTime), 0, 1, startTime, endTime, EasingTypes.None);

            if (alpha == 0)
                return;

            drawSliderRepeat(g, first, angle, 2, alpha);
        }

        private void drawSliderRepeatTail(Graphics g)
        {
            //Get the angle of the last and second last point. so we can rotate the sliderRepeat towards that for the tail
            var last = SliderPath.Path.Points[SliderPath.Path.Points.Count - 1];
            var secondLast = SliderPath.Path.Points[SliderPath.Path.Points.Count - 2];

            last = OsuContainer.MapToPlayfield(last.X, last.Y);
            secondLast = OsuContainer.MapToPlayfield(secondLast.X, secondLast.Y);

            var angle = MathF.Atan2(secondLast.Y - last.Y, secondLast.X - last.X);

            drawSliderRepeat(g, OsuContainer.MapToPlayfield(SliderPath.Path.CalculatePositionAtProgress(snakeIn)), angle, 1, circleAlpha);
        }

        private void drawSliderRepeats(Graphics g)
        {
            //The way i've done slider repeats are very confusing to me 
            var repeatsToGo = slider.Repeats - repeatsDone;

            if (repeatsToGo > 1)
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

        private Vector2 sliderballPosition;
        private Vector2 previousBallPos;

        private void drawSliderBall(Graphics g)
        {
            float current = (float)OsuContainer.SongPosition - slider.StartTime;
            float to = (slider.EndTime - slider.StartTime);

            float progress = MathUtils.Map(current, 0, to, 0, 1f).Clamp(0, 1);

            float osc = MathUtils.OscillateValue(progress * slider.Repeats, 0f, 1f);

            Vector2 pos = SliderPath.Path.CalculatePositionAtProgress(osc);

            if (OsuContainer.SongPosition > slider.StartTime && OsuContainer.SongPosition < slider.EndTime)
                SliderBallPositionForAuto = pos;

            sliderballPosition = OsuContainer.MapToPlayfield(pos.X, pos.Y);

            //Uncapped but still only render sliderball when it's within range exlusive, since uncap just overflows above 1
            //If i used progress here since progress is capped, the sliderball would stay visible for the entire duration of the slider
            //Either that or i have to basically throw away a frame at the end if only checking for < 1f
            //so uncap makes sense here, to not miss a frame


            float sliderBallAngle = MathHelper.RadiansToDegrees(MathF.Atan2(pos.Y - previousBallPos.Y, pos.X - previousBallPos.X));
            if (OsuContainer.SongPosition >= slider.StartTime)
            {
                if (OsuContainer.SongPosition <= slider.EndTime)
                {
                    g.DrawRectangleCentered(sliderballPosition, Size * Skin.GetScale(Skin.SliderBall), color, Skin.SliderBall, rotDegrees: sliderBallAngle);

                    if(Vector2.Distance(pos, previousBallPos) > 1)
                        previousBallPos = pos;
                }

                float followCircleAlpha = Interpolation.ValueAt(sliderFollowScaleAnim.Value, 0, 1, 1, 2, EasingTypes.None) * circleAlpha;
                
                float fadeoutScale = Interpolation.ValueAt(circleAlpha, 1f, 0.75f, 1f, 0f, EasingTypes.Out).Clamp(0.75f, 1f);

                const float Size_Correction_Scale = 1.15f;

                Vector2 followCircleSize = Size * sliderFollowScaleAnim * Skin.GetScale(Skin.SliderFollowCircle, 256, 512) * fadeoutScale;

                //g.DrawEllipse(sliderballPosition, 360, 0, SliderBallActiveScale * OsuContainer.Beatmap.CircleRadius, 0, new Vector4(1f, 1f, 1f, 0.5f));
                g.DrawRectangleCentered(sliderballPosition, followCircleSize, new Vector4(1f, 1f, 1f, followCircleAlpha), Skin.SliderFollowCircle);
            }
        }

        private void drawHitCircle(Graphics g)
        {
            float hitCircleAlpha = circleAlpha;

            if (OsuContainer.Beatmap.Mods.HasFlag(Mods.HD))
            {
                var hiddenFadeIn = OsuContainer.Beatmap.Preempt * 0.4;
                var hiddenFadeOut = hiddenFadeIn + OsuContainer.Beatmap.Preempt * 0.3;

                hitCircleAlpha = (float)MathUtils.Map((this as IDrawableHitObject).TimeElapsed, 0, hiddenFadeIn, 0, 1).Clamp(0, 1);

                if (hitCircleAlpha == 1)
                    hitCircleAlpha = (float)MathUtils.Map((this as IDrawableHitObject).TimeElapsed, hiddenFadeIn, hiddenFadeOut, 1, 0).Clamp(0, 1);
            }

            if (hitCircleAlpha == 0)
                return;

            void drawNumber()
            {
                if (IsHit == false && IsMissed == false)
                    Skin.CircleNumbers.DrawCentered(g, hitCirclePos, Size.X / 2.7f, new Vector4(1f, 1f, 1f, hitCircleAlpha), combo.ToString());
            }

            if (IsMissed == false)
            {
                float scaleExplode = 1f;
                if (hitTime.HasValue)
                {
                    hitCircleAlpha = (float)OsuContainer.SongPosition.Map(hitTime.Value, hitTime.Value + OsuContainer.Fadeout, circleAlpha, 0).Clamp(0, 1f);
                    scaleExplode = (float)Interpolation.ValueAt(OsuContainer.SongPosition, 1, OsuContainer.CircleExplodeScale, hitTime.Value, hitTime.Value + OsuContainer.Fadeout, EasingTypes.Out);
                }

                if (hitCircleAlpha == 0)
                    return;

                g.DrawRectangleCentered(hitCirclePos, Size * Skin.GetScale(Skin.SliderStartCircle) * scaleExplode, new Vector4(color.X, color.Y, color.Z, hitCircleAlpha), Skin.SliderStartCircle);

                if (Skin.Config.HitCircleOverlayAboveNumber)
                    drawNumber();

                g.DrawRectangleCentered(hitCirclePos, Size * Skin.GetScale(Skin.SliderStartCircleOverlay) * scaleExplode, new Vector4(1f, 1f, 1f, hitCircleAlpha), Skin.SliderStartCircleOverlay);

                if (!Skin.Config.HitCircleOverlayAboveNumber)
                    drawNumber();
            }

            //Circle numbers dont need to fade or explode, just dissappear instantly
            
        }

        public override void AfterRender(Graphics g)
        {
            if (OsuContainer.Beatmap.Mods.HasFlag(Mods.HD))
                return;

            if (approachRing > 1f && !IsMissed)
            {
                float hitCircleAlpha = circleAlpha;
                if(hitTime.HasValue)
                    hitCircleAlpha = (float)OsuContainer.SongPosition.Map(hitTime.Value, hitTime.Value + OsuContainer.Fadeout, circleAlpha, 0).Clamp(0, 1f);

                g.DrawRectangleCentered(hitCirclePos, Size * approachRing * Skin.GetScale(Skin.ApproachCircle), new Vector4(color.X, color.Y, color.Z, hitCircleAlpha), Skin.ApproachCircle);
            }
        }

        private bool checkHit()
        {
            if (IsHit == false && IsMissed == false)
            {
                if (MathUtils.IsPointInsideRadius(OsuContainer.CursorPosition, hitCirclePos, OsuContainer.Beatmap.CircleRadius) || OsuContainer.CookieziMode)
                {
                    if (OsuContainer.SongPosition < slider.StartTime - OsuContainer.Beatmap.Fadein) { return true; }

                    double hittableTime = Math.Abs(OsuContainer.SongPosition - slider.StartTime);

                    OsuContainer.ScoreHit(slider);

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
                    OsuContainer.HUD.AddHit((float)hittableTime, HitResult.Max, sliderballPosition, true);

                    return true;
                }
            }

            return false;
        }

        public override bool OnKeyDown(Key key)
        {
            if (OsuContainer.CookieziMode)
                return false;

            if (key == OsuContainer.Key1 || key == OsuContainer.Key2)
                return checkHit();

            return false;
        }

        public override bool OnMouseDown(MouseButton args)
        {
            if (OsuContainer.CookieziMode)
                return false;

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

        private const float SliderBallActiveScale = 2f;

        private bool IsTracking => (OsuContainer.Key1Down || OsuContainer.Key2Down) && MathUtils.IsPointInsideRadius(OsuContainer.CursorPosition, sliderballPosition, OsuContainer.Beatmap.CircleRadius * SliderBallActiveScale) || OsuContainer.CookieziMode;

        /// <summary>
        /// Allow releasing the slider 36ms too early
        /// </summary>
        private const double TrackingErrorAcceptance = 36;
        private bool IsValidTrack => (OsuContainer.SongPosition - lastTrackingTime) <= TrackingErrorAcceptance || OsuContainer.CookieziMode;

        private bool previousTracking;

        private double lastTrackingTime = 0;

        public override void OnRemove()
        {
            SliderOnScreen = false;

            SliderBallPositionForAuto = null;
            SliderPath.Cleanup();
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
            sliderFollowScaleAnim.Value = 1f;
            lastTrackingTime = 0;
            previousTracking = false;
            hitTime = null;
        }

        public override void Update(float delta)
        {
            hitCirclePos = OsuContainer.MapToPlayfield(slider.Position.X, slider.Position.Y);

            if (IsValidTrack != previousTracking && IsHit || IsMissed)
            {
                previousTracking = IsTracking;

                if (IsTracking)
                {
                    sliderFollowScaleAnim.ClearTransforms();

                    //150ms if slider duration is longer than 150, else use slider duration.
                    float time = Math.Min(150, slider.EndTime - slider.StartTime);
                    sliderFollowScaleAnim.TransformTo(SliderBallActiveScale, time, EasingTypes.Out);
                }
                else
                {
                    sliderFollowScaleAnim.ClearTransforms();
                    sliderFollowScaleAnim.TransformTo(1, 50f, EasingTypes.Out);
                }
            }

            if (IsTracking)
                lastTrackingTime = OsuContainer.SongPosition;

            if (OsuContainer.SongPosition > slider.StartTime && OsuContainer.SongPosition < slider.EndTime)
                sliderFollowScaleAnim.Update((float)OsuContainer.DeltaSongPosition);

            //yikes
            if(!OsuContainer.MuteHitsounds && IsTracking && (Skin.SliderSlide.PlaybackPosition > Skin.SliderSlide.PlaybackLength - 50f || Skin.SliderSlide.IsPlaying == false) && OsuContainer.SongPosition > slider.StartTime && OsuContainer.SongPosition < slider.EndTime && (IsHit || IsMissed) && !(OsuContainer.Beatmap.Song?.IsPaused ?? false))
               Skin.SliderSlide.Play(true);

            double timeElapsed = (OsuContainer.SongPosition - slider.StartTime + OsuContainer.Beatmap.Preempt);

            double fadeOutStart = slider.EndTime;

            //Neeed a better way to handle this
            if (fadeout)
            {
                 circleAlpha = (float)MathUtils.Map(OsuContainer.SongPosition, fadeOutStart, fadeOutStart + OsuContainer.Fadeout, 1, 0).Clamp(0, 1);
            }
            else
            {
                approachRing = (float)MathUtils.Map(timeElapsed, 0, OsuContainer.Beatmap.Preempt, OsuContainer.ApproachCircleScale, 1f).
                    Clamp(1, OsuContainer.ApproachCircleScale);

                if (approachRing == 1f && OsuContainer.CookieziMode)
                {
                    checkHit();
                }

                circleAlpha = (float)MathUtils.Map(timeElapsed, 0, OsuContainer.Beatmap.Fadein, 0, 1f).Clamp(0, 1f);

                if (GlobalOptions.SliderSnakeIn.Value)
                {
                    double snakeInEndTime = OsuContainer.Beatmap.Fadein / 2;
                    snakeIn = (float)Interpolation.ValueAt(timeElapsed.Clamp(0, snakeInEndTime), 0, 1, 0, snakeInEndTime, EasingTypes.Out);
                }
                else
                {
                    snakeIn = 1;
                }

                //snakeIn = (float)MathUtils.Map(OsuContainer.SongPosition, slider.StartTime - 400, slider.EndTime, 0, 1);
            }

            //Slider snakeout
            float snakeOut = 0f;
            if (repeatsDone >= slider.Repeats - 1 && GlobalOptions.SliderSnakeOut.Value)
            {
                double snakeDuration = slider.EndTime - slider.StartTime;
                double startSnake = slider.EndTime - (snakeDuration / slider.Repeats);
                double endSnake = slider.EndTime;

                if (slider.Repeats % 2 != 0)
                    snakeOut = (float)MathUtils.Map(OsuContainer.SongPosition, startSnake, endSnake, 0, 1).Clamp(0, 1);
                else
                    snakeIn = (float)MathUtils.Map(OsuContainer.SongPosition, startSnake, endSnake, 1, 0).Clamp(0, 1);
            }

            repeatsDone = (int)MathUtils.Map(OsuContainer.SongPosition, slider.StartTime, slider.EndTime, 0, slider.Repeats).Clamp(0, slider.Repeats);
            //I dont understand hitsounds fully, so this is pretty bs need to redo
            if (repeatsDone != lastRepeatsDone)
            {
                lastRepeatsDone = repeatsDone;
                if (repeatsDone < slider.Repeats && IsValidTrack)
                {
                    var sliderRepeatExplode = ObjectPool<ExplodingSliderRepeat>.Take();
                    sliderRepeatExplode.SetTarget(SliderPath, repeatsDone);

                    Container.Add(sliderRepeatExplode);
                }
                //var hitsound = slider.EdgeHitSounds?[1] ?? slider.HitSound;
                //var sample = slider.EdgeAdditions?[1].Item1 ?? slider.Extras.SampleSet;

                HitSoundType hitsound = slider.EdgeHitSounds?[repeatsDone] ?? slider.HitSound;
                SampleSet sampleSet = slider.EdgeAdditions?[repeatsDone].Item1 ?? SampleSet.None;

                //var hitsound = slider.EdgeHitSounds?[repeatsDone] ?? slider.HitSound;
                //var sample = slider.EdgeAdditions?[repeatsDone].Item1 ?? slider.Extras.SampleSet;

                SampleSet? sampleSetAddition = slider.EdgeAdditions?[repeatsDone].Item2;

                if (IsHit == false && IsMissed == false)
                {
                    IsMissed = true;
                    OsuContainer.HUD.AddHit(OsuContainer.Beatmap.Window50, HitResult.Miss, sliderballPosition, false);
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
                        OsuContainer.PlayHitsound(hitsound, sampleSet);

                        if (sampleSetAddition.HasValue)
                            OsuContainer.PlayHitsound(hitsound, sampleSetAddition.Value);
                    }

                    OsuContainer.HUD.AddHit(result == HitResult.Miss ? OsuContainer.Beatmap.Window50 : 0, result, sliderballPosition);
                }
                else if (IsValidTrack)
                {
                    OsuContainer.PlayHitsound(hitsound, sampleSet);

                    if (sampleSetAddition.HasValue)
                        OsuContainer.PlayHitsound(hitsound, sampleSetAddition.Value);

                    OsuContainer.HUD.AddHit(0, HitResult.Max, sliderballPosition, false);
                }
                else
                {
                    IsMissed = true;
                    OsuContainer.HUD.AddHit(OsuContainer.Beatmap.Window50, HitResult.Miss, sliderballPosition, false);
                }
            }

            //Fix only explode when there are no misses on the slider
            //This piece of code is for exploding the slider itself like it was a circle
            if (fadeout && (IsHit || IsEndingHit) && GlobalOptions.SliderSnakeExplode.Value)
            {
                if (GlobalOptions.SliderSnakeOut.Value)
                    SliderPath.ScalingOrigin = sliderballPosition;
                else
                    SliderPath.ScalingOrigin = OsuContainer.MapToPlayfield(SliderPath.Path.Bounds.Center);

                SliderPath.DrawScale = (float)Interpolation.ValueAt(circleAlpha, 1, OsuContainer.CircleExplodeScale, 1, 0, EasingTypes.Out);
            }
            else
            {
                SliderPath.ScalingOrigin = null;
                SliderPath.DrawScale = 1;
            }

            //Fade out the slider based on its duration when hidden (bad mod)
            if (OsuContainer.Beatmap.Mods.HasFlag(Mods.HD))
                SliderPath.Alpha = circleAlpha * Interpolation.ValueAt(OsuContainer.SongPosition.Clamp(slider.StartTime, slider.EndTime), 1, 0, slider.StartTime, slider.EndTime, EasingTypes.None);
            else
            {
                //If we reached the end of the slider and snake out is enabled and exploding is disabled, just instantly make it disappear
                if (GlobalOptions.SliderSnakeOut.Value && !GlobalOptions.SliderSnakeExplode.Value && OsuContainer.SongPosition > slider.EndTime)
                    SliderPath.Alpha = 0f;
                else
                    SliderPath.Alpha = circleAlpha;
            }

            SliderPath.SetProgress(snakeOut, snakeIn);
            SliderPath.SetRadius(OsuContainer.Beatmap.CircleRadiusInOsuPixels);
            SliderOnScreen = true;

            if (OsuContainer.SongPosition > slider.StartTime + OsuContainer.Beatmap.Window50 && !IsHit && !IsMissed)
            {
                OsuContainer.HUD.AddHit((float)OsuContainer.Beatmap.Window50, HitResult.Miss, hitCirclePos, true);
                IsMissed = true;
            }

            if (fadeout && circleAlpha == 0)
                IsDead = true;

            //Instead of making it like this, maybe i should map the fadein time so that when it overflows it fades out?
            //probaly not since i need to check for when a user is done playing the slider anyways
            //And they can start it very early or very late
            if (OsuContainer.SongPosition >= slider.EndTime)
                fadeout = true;
        }

        public override void Render(Graphics g)
        {
            Vector3 sliderBorder = new Vector3(Skin.Config.SliderBorder ?? new Vector3(1f, 1f, 1f));
            Vector3 sliderTrack = new Vector3(Skin.Config.SliderTrackOverride ?? new Vector3(0));

            if (OsuContainer.IsKiaiTimeActive && GlobalOptions.RGBCircles.Value)
            {
                sliderBorder = MathUtils.RainbowColor(OsuContainer.SongPosition / 1000, 0.5f, 1.1f);
            }

            g.BorderColorInner = sliderBorder;
            g.BorderColorOuter = g.BorderColorInner;

            g.ShadowColor = new Vector4(0, 0, 0, 0.5f);

            g.TrackColorOuter = Shade(-0.1f, new Vector4(sliderTrack, 1.0f)).Xyz;
            g.TrackColorInner = Shade(0.5f, new Vector4(sliderTrack, 1.0f)).Xyz;

            SliderPath.Render(g);

            if (circleAlpha > 0)
            {
                drawHitCircle(g);

                drawSliderRepeats(g);

                drawSliderBall(g);
            }

            //g.DrawString($"In: {snakeIn:F2} A: {SliderPath.Alpha:F2}", Font.DefaultFont, hitCirclePos, Colors.White, 0.5f);
        }

        public static Vector4 Shade(float amount, Vector4 c)
        {
            if(amount < 0)
                return Darken(-amount, c);

                return Lighten(amount, c);
        }

        public static Vector4 Darken(float amount, Vector4 c) {
            float scale = MathF.Max(1.0f, 1.0f + amount);

            return new Vector4(c.X / scale, c.Y / scale, c.Z / scale, c.W);
        }

        public static Vector4 Lighten(float amount, Vector4 c)
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
