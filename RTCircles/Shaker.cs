using Easy2D;
using OpenTK.Mathematics;
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
        public float ShakeFadeoutScale = 1;

        public void Shake()
        {
            shakeAmountSpeed.Value = 1;
            shakeAmountSpeed.TransformTo(0, Duration, Easing);
        }

        private double shakeTime;

        public void Update()
        {
            double delta = MainGame.Instance.DeltaTime;
            shakeAmountSpeed.Update((float)delta);

            shakeTime += delta * Speed * shakeAmountSpeed.Value.Map(1, 0, 1, ShakeFadeoutScale);
            double noiseX = Math.Cos(shakeTime).Map(-1, 1, 0, 1);
            double noiseY = Math.Sin(shakeTime).Map(-1, 1, 0, 1);

            Vector2 shakeFinal = new Vector2();

            float radius = Radius * shakeAmountSpeed;

            shakeFinal.X = (float)Perlin.Instance.Noise(noiseX, 0, 0) * radius;
            shakeFinal.X -= radius / 2;

            shakeFinal.Y = (float)Perlin.Instance.Noise(0, noiseY, 0) * radius;
            shakeFinal.Y -= radius / 2;

            OutputShake = shakeFinal;
        }
    }
}
