using Easy2D;
using OpenTK.Mathematics;
using System;

namespace RTCircles
{
    public class ExplodingSliderRepeat : Drawable
    {
        private Vector2 Size => new Vector2(OsuContainer.Beatmap.CircleRadius * 2);
        private Vector2 position;

        private ISlider slider;

        private double spawnTime;

        private int index = 0;

        private float angle;

        public override void OnRemove()
        {
            ObjectPool<ExplodingSliderRepeat>.Return(this);
        }

        public void SetTarget(ISlider slider, int index)
        {
            this.slider = slider;
            this.index = index;
            spawnTime = OsuContainer.SongPosition;

            if (index % 2 == 0)
            {
                var first = slider.Path.Points[0];
                var next = slider.Path.Points[1];

                first = OsuContainer.MapToPlayfield(first.X, first.Y);
                next = OsuContainer.MapToPlayfield(next.X, next.Y);

                angle = MathF.Atan2(next.Y - first.Y, next.X - first.X);
            }
            else
            {
                var last = slider.Path.Points[slider.Path.Points.Count - 1];
                var secondLast = slider.Path.Points[slider.Path.Points.Count - 2];

                last = OsuContainer.MapToPlayfield(last.X, last.Y);
                secondLast = OsuContainer.MapToPlayfield(secondLast.X, secondLast.Y);

                angle = MathF.Atan2(secondLast.Y - last.Y, secondLast.X - last.X);
            }

            angle = MathHelper.RadiansToDegrees(angle);
        }

        public override void Render(Graphics g)
        {
            if(OsuContainer.SongPosition < spawnTime)
            {
                IsDead = true;
                return;
            }

            if(index % 2 == 0)
                position = OsuContainer.MapToPlayfield(slider.Path.Points[0]);
            else
                position = OsuContainer.MapToPlayfield(slider.Path.Points[^1]);

            float alpha = (float)OsuContainer.SongPosition.Map(spawnTime, spawnTime + OsuContainer.Fadeout, 0.9, 0).Clamp(0, 0.9);
            float scale = (float)Interpolation.ValueAt(OsuContainer.SongPosition, 1, OsuContainer.CircleExplodeScale, spawnTime, spawnTime + OsuContainer.Fadeout, EasingTypes.Out);

            Vector2 size = new Vector2(Size.Y, Size.Y / Skin.SliderReverse.Texture.Size.AspectRatio()) * Skin.GetScale(Skin.SliderReverse);

            g.DrawRectangleCentered(position, size * scale, new Vector4(1f, 1f, 1f, alpha), Skin.SliderReverse, null, false, angle);

            if(alpha<=0)
                IsDead = true;
        }

        public override void Update(float delta) { }
    }
}

//jeg skal redo det her lort
