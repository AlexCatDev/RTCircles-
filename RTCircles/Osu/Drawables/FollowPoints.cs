using Easy2D;
using OpenTK.Mathematics;
using OsuParsers.Beatmaps.Objects;
using System;

namespace RTCircles
{
    public class FollowPoints : Drawable
    {
        private IDrawableHitObject from, to;

        private static int layerCounter = -727;

        private float alpha = 1f;

        public void SetTarget(IDrawableHitObject from, IDrawableHitObject to)
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
            HitObject startObject = from.BaseObject;
            HitObject endObject = to.BaseObject;

            Vector2 fromPos;

            if (startObject is Slider slider)
            {
                if (slider.SliderPoints.Count == 0)
                {
                    Utils.Log($"Slider had no sliderpoints!!", LogLevel.Error);
                    IsDead = true;
                    return;
                }

                Vector2 sliderAttachmentPoint = slider.Repeats % 2 == 0 ? new Vector2(slider.Position.X, slider.Position.Y) : (from as DrawableSlider).SliderPath.Path.Points[^1];

                fromPos = OsuContainer.MapToPlayfield(sliderAttachmentPoint);
            }
            else
            {
                fromPos = OsuContainer.MapToPlayfield(startObject.Position);
            }
         
            Vector2 toPos = OsuContainer.MapToPlayfield(endObject.Position);

            if ((toPos - fromPos).Length < OsuContainer.Beatmap.CircleRadius * 4)
            {
                IsDead = true;
                return;
            }

            float toProgress = (float)OsuContainer.SongPosition.Map(startObject.EndTime - OsuContainer.Beatmap.FadeIn, endObject.StartTime - OsuContainer.Beatmap.FadeIn, 0, 1).Clamp(0, 1);

            if (toProgress == 0)
                return;

            toPos.X = toProgress.Map(0, 1, fromPos.X, toPos.X);
            toPos.Y = toProgress.Map(0, 1, fromPos.Y, toPos.Y);

            const float START_ALPHA = 0.4f;

            float alpha = (float)OsuContainer.SongPosition.Map(startObject.EndTime, endObject.StartTime, START_ALPHA, 0).Clamp(0, START_ALPHA);

            fromPos.X = alpha.Map(START_ALPHA, 0, fromPos.X, toPos.X);
            fromPos.Y = alpha.Map(START_ALPHA, 0, fromPos.Y, toPos.Y);

            if (alpha == 0)
            {
                IsDead = true;
                return;
            }

            Vector3 color = from.CurrentColor.Xyz;

            g.DrawLine(fromPos, toPos, new Vector4(color, alpha), OsuContainer.Beatmap.CircleRadius / 14);
            /*
            alpha = (float)OsuContainer.SongPosition.Map(from.EndTime, to.StartTime, 1, 0).Clamp(0, 1);

            if (alpha == 0 || OsuContainer.SongPosition >= to.StartTime)
            {
                IsDead = true;
                return;
            }

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

            if ((toPos - fromPos).Length < OsuContainer.Beatmap.CircleRadius * 4)
            {
                IsDead = true;
                return;
            }

            float toProgress = (float)OsuContainer.SongPosition.Map(from.StartTime - OsuContainer.Beatmap.FadeIn, to.StartTime - OsuContainer.Beatmap.FadeIn, 0, 1).Clamp(0, 1);
            //if no progress, don't draw anything

            if (toProgress == 0f)
                return;

            //gradually expand the endPosition, towards the circles, using the progress from above
            toPos.X = toProgress.Map(0, 1, fromPos.X, toPos.X);
            toPos.Y = toProgress.Map(0, 1, fromPos.Y, toPos.Y);

            var currentTexture = Skin.FollowPoint.GetTexture(toProgress, true);

            float height = OsuContainer.Beatmap.CircleRadius * 2 * Skin.GetScale(currentTexture);
            float width = height * currentTexture.Texture.Size.AspectRatio();

            g.DrawDottedLine(fromPos, toPos, currentTexture, new Vector4(1f, 1f, 1f, alpha), new Vector2(width, height), OsuContainer.Beatmap.CircleRadius, false, false, new Rectangle(0, 0, MainGame.WindowWidth, MainGame.WindowHeight));

            //Draw a line, with diameter spacing, and correct texture scaling
            //This needs a little optimization. and tweaking
            //g.DrawDottedLine(fromPos, toPos, Skin.FollowPoint, new Vector4(1f, 1f, 1f, alpha), new Vector2(OsuContainer.Beatmap.CircleRadius * 2 * Skin.GetScale(Skin.FollowPoint)), OsuContainer.Beatmap.CircleRadius, false, false, new Rectangle(0, 0, 1920, 1080));
            */
        }

        public override void OnRemove()
        {
            ObjectPool<FollowPoints>.Return(this);
        }

        public override void Update(float delta)
        {

        }
    }
}
