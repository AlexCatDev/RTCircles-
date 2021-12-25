using Easy2D;
using OpenTK.Mathematics;
using System;

namespace RTCircles
{
    public class HitJudgement : Drawable
    {
        private Vector2 pos;
        private SmoothFloat alpha = new SmoothFloat();
        private float rotation;
        private SmoothFloat height = new SmoothFloat();

        private static int Lol = Int32.MaxValue;

        private HitResult result;

        public HitJudgement(Vector2 pos, HitResult result)
        {
            //Force stay on top and in order
            Layer = Lol--;

            this.pos = pos;
            this.result = result;

            //Not sure if these values are accurate
            height.Value = 0;
            height.TransformTo(OsuContainer.Beatmap.CircleRadius * 2f, 160f, EasingTypes.OutElasticHalf);
            alpha.Value = 0;
            alpha.TransformTo(1f, 120f, EasingTypes.Out).Wait(350).TransformTo(0, 500, EasingTypes.In);
        }

        public override Rectangle Bounds => new Rectangle();

        public override void Render(Graphics g)
        {
            OsuTexture tex = null;

            switch (result)
            {
                case HitResult.Max:
                    return;
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

            Vector2 s = new Vector2(height * tex.Texture.Size.AspectRatio(), height) * Skin.GetScale(tex);

            g.DrawRectangleCentered(pos, s, new Vector4(1f, 1f, 1f, alpha), tex, rotDegrees: rotation);
        }

        public override void Update(float delta)
        {
            delta = (float)(OsuContainer.DeltaSongPosition);

            height.Update(delta);

            alpha.Update(delta);

            if (result == HitResult.Miss)
            {
                pos.Y += 0.060f * delta;
                rotation += 0.045f * delta;
            }

            if (alpha.HasCompleted)
                IsDead = true;
        }
    }
}
