﻿using Easy2D;
using System.Numerics;

namespace RTCircles
{
    public class ExpandingCircle : Drawable
    {
        private Vector2 startPos;
        private float startRadius;
        private SmoothFloat progress = new SmoothFloat();

        public float ExpandAmount = 3.5f;
        public float ExpandTime = 1f;
        public EasingTypes Easing = EasingTypes.OutQuint;

        public ExpandingCircle(Vector2 startPos, float startRadius)
        {
            this.startPos = startPos;
            this.startRadius = startRadius;

            explodeFadeout();
        }

        private void explodeFadeout()
        {
            progress.Value = 1;
            progress.TransformTo(ExpandAmount, ExpandTime, Easing);
        }

        public override void Render(Graphics g)
        {
            var radius = startRadius * progress;
            var innerRadius = radius * progress.Value.Map(1, 3.5f, 0.6f, 1);

            var alpha = progress.Value.Map(1, ExpandAmount, 0.5f, 0);

            g.DrawEllipse(startPos, 0, 360, radius, innerRadius, 
                new Vector4(1f, 1f, 1f, alpha), Texture.WhiteFlatCircle2, 100, false);
        }

        public override void Update(float delta)
        {
            progress.Update(delta);

            if (progress.HasCompleted)
            {
                //explodeFadeout();
                IsDead = true;
            }
        }
    }
}
