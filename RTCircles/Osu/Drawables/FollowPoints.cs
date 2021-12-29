﻿using Easy2D;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps.Objects;
using System;

namespace RTCircles
{
    public class FollowPoints : Drawable
    {
        private HitObject from, to;

        private static int layerCounter = -727;

        private float alpha = 1f;

        public FollowPoints(HitObject from, HitObject to)
        {
            this.from = from;
            this.to = to;

            Layer = layerCounter--;

            if (layerCounter > 0)
                layerCounter = -727;
        }

        public override Rectangle Bounds => new Rectangle();

        public override void Render(Graphics g)
        {
            alpha = (float)OsuContainer.SongPosition.Map(from.EndTime, from.EndTime + OsuContainer.Fadeout, 1, 0).Clamp(0, 1);

            Vector2 fromPos;

            if (from is Slider slider)
            {
                if (slider.SliderPoints.Count == 0)
                {
                    Utils.Log($"Slider had no sliderpoints!!", LogLevel.Error);
                    //Just remove it XD fuck aspire maps
                    IsDead = true;
                    return;
                }

                System.Numerics.Vector2 sliderPoint = slider.Repeats % 2 == 0 ? slider.Position : slider.SliderPoints[^1];

                fromPos = OsuContainer.MapToPlayfield(sliderPoint.X, sliderPoint.Y);
            }
            else
                fromPos = OsuContainer.MapToPlayfield(from.Position.X, from.Position.Y);

            Vector2 toPos = OsuContainer.MapToPlayfield(to.Position.X, to.Position.Y);

            //If the from and to positions are smalelr than the diameter of the hitcircles, despawn the followpoint
            if (MathF.Abs(toPos.X - fromPos.X) < OsuContainer.Beatmap.CircleRadius * 2 && MathF.Abs(toPos.Y - fromPos.Y) < OsuContainer.Beatmap.CircleRadius * 2)
            {
                IsDead = true;
                return;
            }

            float toProgress = (float)OsuContainer.SongPosition.Map(to.StartTime - OsuContainer.Beatmap.Preempt, to.StartTime - OsuContainer.Beatmap.Fadein, 0, 1).Clamp(0, 1);

            //if no progress, don't draw anything

            if (toProgress == 0f)
                return;

            //gradually expand the endPosition, towards the circles, using the progress from above
            toPos.X = toProgress.Map(0, 1, fromPos.X, toPos.X);
            toPos.Y = toProgress.Map(0, 1, fromPos.Y, toPos.Y);

            //Draw a line, with diameter spacing, and correct texture scaling
            //This needs a little optimization. and tweaking
            g.DrawDottedLine(fromPos, toPos, Skin.FollowPoint, new Vector4(1f, 1f, 1f, alpha), new Vector2(OsuContainer.Beatmap.CircleRadius * 2 * Skin.GetScale(Skin.FollowPoint)), OsuContainer.Beatmap.CircleRadius, false, false, new Rectangle(0, 0, 1920, 1080));
        }

        public override void Update(float delta)
        {
            if (alpha == 0f || OsuContainer.SongPosition < from.StartTime - OsuContainer.Beatmap.Preempt - 500)
                IsDead = true;
        }
    }
}