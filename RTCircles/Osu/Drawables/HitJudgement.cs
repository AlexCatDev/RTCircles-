using Easy2D;
using System.Numerics;
using System;

namespace RTCircles
{
    public class HitJudgement : Drawable
    {
        private Vector2 pos;
        private SmoothFloat alpha = new SmoothFloat();
        private float rotation;
        private SmoothFloat height = new SmoothFloat();

        private static long Lol = long.MaxValue;

        private HitResult result;

        public HitJudgement(Vector2 pos, HitResult result)
        {
            //Force stay on top and in order
            Layer = Lol--;

            this.pos = pos;
            this.result = result;

            //Not sure if these values are accurate
            height.Value = 0;
            height.TransformTo(1f, (float)OsuContainer.Fadeout, EasingTypes.OutElasticHalf);
            alpha.Value = 0;
            alpha.TransformTo(1f, 150f, EasingTypes.Out).Wait(200).TransformTo(0, 500, EasingTypes.In);
        }

        public override Rectangle Bounds => new Rectangle();

        public override void Render(Graphics g)
        {
            OsuTexture tex = null;

            switch (result)
            {
                case HitResult.Max:
                    tex = Skin.Hit300;
                    break;
                case HitResult.Good:
                    tex = Skin.Hit100;
                    break;
                case HitResult.Meh:
                    tex = Skin.Hit50;
                    break;
                case HitResult.Miss:
                    tex = Skin.HitMiss;
                    break;
                default:
                    break;
            }

            float h = height.Value * OsuContainer.Beatmap.CircleRadius * 2f;
            Vector2 s = new Vector2(h * tex.Texture.Size.AspectRatio(), h) * Skin.GetScale(tex);

            g.DrawRectangleCentered(pos, s, new Vector4(1f, 1f, 1f, alpha), tex, rotDegrees: rotation);
        }

        private float velocity = 0.060f;

        public override void Update(float delta)
        {
            delta = (float)OsuContainer.DeltaSongPosition;

            height.Update(delta);

            alpha.Update(delta);

            if (result == HitResult.Miss)
            {
                pos.Y += velocity * delta;
                velocity += 0.00025f * delta;
                rotation += 0.045f * delta;
            }

            if (alpha.HasCompleted)
                IsDead = true;
        }
    }
}
