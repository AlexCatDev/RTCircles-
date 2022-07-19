using System;
using Easy2D;
using OpenTK.Mathematics;

namespace PogGame
{
    public class DrawableExplosion : Drawable
    {
        public override Rectangle Bounds => new Rectangle(pos - size / 2f, size);

        private Vector2 pos;
        private Vector2 size = new Vector2(8, 8);
        private SmoothFloat alpha = new SmoothFloat();
        private Vector2 velocity;
        private float scale;

        public DrawableExplosion(Vector2 pos)
        {
            alpha.Value = 0f;
            alpha.TransformTo(1f, 0.25f, EasingTypes.Out);
            alpha.TransformTo(0f, 0.5f, EasingTypes.Out);

            float theta = RNG.Next(0, MathF.PI * 2);
            this.pos = pos + new Vector2(MathF.Cos(theta), MathF.Sin(theta)) * 1f;

            scale = RNG.Next(20f, 100f) / 100f;

            velocity.X = MathF.Cos(theta) * 1000f * scale;
            velocity.Y = MathF.Sin(theta) * 1000f * scale;
        }

        public override void Render(Graphics g)
        {
            float r = MathF.Abs((float)Math.Sin(Time)) * 5;
            float green = MathF.Abs((float)Math.Cos(Time)) * 5;
            float b = MathF.Abs(r - green) * 5;
            Vector4 color = new Vector4(r + 1, green + 1, b + 1, alpha);
            g.DrawRectangleCentered(pos, size, color, Texture.WhiteCircle);
        }

        public override void OnUpdate()
        {
            pos += velocity * fDelta;
            alpha.Update(fDelta);

            if (alpha.HasCompleted)
                IsDead = true;
        }
    }
}
