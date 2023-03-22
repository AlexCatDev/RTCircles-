using Easy2D;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTCircles
{
    public interface ISlider
    {
        public Path Path { get; }

        public float DrawScale { get; set; }

        public Vector2? ScalingOrigin { get; set; }

        public float Alpha { get; set; }

        public void Cleanup();

        public void SetRadius(float osuRadius);
        public void SetPoints(List<Vector2> points);
        public void SetProgress(float startProgress, float endProgress);
        public void Render(Graphics g);

        public void OffscreenRender();
    }
}
