using Easy2D;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public class Shaker
    {
        public float Speed;
        public float Radius;
        public float Duration;

        private SmoothFloat shakeAmountSpeed = new SmoothFloat();

        public Vector2 OutputShake { get; private set; }

        public EasingTypes Easing = EasingTypes.None;

        public void Shake()
        {
            shakeTime = 0;

            shakeAmountSpeed.Value = 1;
            shakeAmountSpeed.TransformTo(0, Duration, Easing);
        }

        private double shakeTime;

        public void Update(float delta)
        {
            shakeAmountSpeed.Update(delta);

            shakeTime += delta * Speed * shakeAmountSpeed;
            double noiseX = Math.Cos(shakeTime).Map(-1, 1, 0, 1);
            double noiseY = Math.Sin(shakeTime).Map(-1, 1, 0, 1);

            Vector2 shakeFinal = Vector2.Zero;

            float radius = Radius * shakeAmountSpeed;

            shakeFinal.X = (float)Perlin.Instance.Noise(noiseX, 0, 0) * radius;
            shakeFinal.X -= radius / 2;

            shakeFinal.Y = (float)Perlin.Instance.Noise(0, noiseY, 0) * radius;
            shakeFinal.Y -= radius / 2;

            OutputShake = shakeFinal;
        }
    }
}
