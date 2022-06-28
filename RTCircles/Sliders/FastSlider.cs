using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.IO;

namespace RTCircles
{
    public class FastSlider : ISlider
    {
        private static readonly Easy2D.Texture gradientTexture = new Easy2D.Texture(Utils.GetResource("Sliders.sliderGradientDouble.png"));

        public Path path = new Path();

        private Vector2 radius = new Vector2(-1);

        private float osuRadius = -1;

        public void SetRadius(float osuRadius)
        {
            this.osuRadius = osuRadius;
        }

        public void SetPoints(List<Vector2> points) => Path.SetPoints(points);

        private float startProgress = 0f;
        private float endProgress = 1f;
        public void SetProgress(float startProgress, float endProgress)
        {
            this.startProgress = startProgress.Clamp(0, 1);
            this.endProgress = endProgress.Clamp(0, 1);
        }

        public void DeleteFramebuffer() { }

        private Vector4 color = new Vector4(1f);
        public float Alpha
        {
            get
            {
                return color.W;
            }
            set
            {
                color.W = value;
            }
        }

        public Path Path => path;

        public float DrawScale { get; set; } = 1;
        public Vector2? ScalingOrigin { get; set; }

        private List<Sliders.Polyline2D.PolySegment> segments = new List<Sliders.Polyline2D.PolySegment>();

        private void drawSlider(Graphics g)
        {
            float startLength = Path.Length * startProgress;
            float endLength = Path.Length * endProgress;

            segments.Clear();

            for (int i = 0; i < Path.Points.Count - 1; i++)
            {
                float dist = Vector2.Distance(Path.Points[i], Path.Points[i + 1]);

                Vector2 start;
                Vector2 end;

                if(segments.Count == 0)
                {
                    if (startLength - dist <= 0)
                    {
                        float blend = startLength / dist;
                        start = OsuContainer.MapToPlayfield(Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend));
                    }
                    else
                    {
                        startLength -= dist;
                        continue;
                    }
                }
                else
                {
                    start = OsuContainer.MapToPlayfield(Path.Points[i]);
                }

                if (endLength - dist <= 0)
                {
                    float blend = endLength / dist;
                    end = OsuContainer.MapToPlayfield(Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend));
                    
                    segments.Add(new Sliders.Polyline2D.PolySegment(new Sliders.LineSegment(start, end), radius.X / 2));

                    break;
                }

                end = OsuContainer.MapToPlayfield(Path.Points[i + 1]);

                endLength -= dist;

                if (start == end)
                    continue;

                segments.Add(new Sliders.Polyline2D.PolySegment(new Sliders.LineSegment(start, end), radius.X / 2));
            }

            Sliders.Polyline2D.Render(g, segments, gradientTexture, color, radius.X / 2);
        }

        public void Render(Graphics g)
        {
            if (Precision.AlmostEquals(Alpha, 0))
                return;

            if (osuRadius < -1)
                throw new Exception("Slider radius was less than 0????");

            radius = new Vector2(osuRadius * (OsuContainer.Playfield.Width / 512)) * DrawScale * 2;
            drawSlider(g);
            /*
            var prevRadius = radius;
            var prevColor = color;

            drawSlider(g);
            radius /= 1.175f;
            color = new Vector4(DrawableSlider.Shade(0.5f, new Vector4(Skin.Config.SliderTrackOverride ?? new Vector3(0.1f), 1.0f)).Xyz, color.W);
            drawSlider(g);

            radius = prevRadius;
            color = prevColor;
            */
        }

        public void Cleanup()
        {

        }

        public void OffscreenRender()
        {
            
        }
    }
}
