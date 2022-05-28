using Easy2D;
using OpenTK.Mathematics;
using Silk.NET.OpenGLES;
using System;
using System.Collections.Generic;
using System.IO;

namespace RTCircles
{
    //Batching the sliders solved alot of issues

    //TODO: Optimizations
    //fuck the interpolating cancer, i will fix later 
    //+ optimize to use polylines for less vertices

    /*
    public class FastSlider : ISlider
    {
        public Path path = new Path();

        private Vector2 radius = new Vector2(-1);

        private float osuRadius = -1;

        private void drawLine(Graphics g, Vector2 startPosition, Vector2 endPosition)
        {
            g.DrawLine(startPosition, endPosition, color, radius.Y);
        }

        //Circles can be pregenerated, optimize circle resolution for the radius
        private void drawCircle(Graphics g, Vector2 pos)
        {
            g.DrawRectangleCentered(pos, radius, color, circleTexture);
        }

        public void SetRadius(float radius)
        {
            osuRadius = radius * 0.96f;
        }

        public void SetPoints(List<Vector2> points)
        {
            Path.SetPoints(points);
        }

        private float startProgress = 0f;
        private float endProgress = 1f;
        public void SetProgress(float startProgress, float endProgress)
        {
            this.startProgress = startProgress.Clamp(0, 1);
            this.endProgress = endProgress.Clamp(0, 1);
        }

        public void DeleteFramebuffer()
        {
            
        }

        private static Easy2D.Texture circleTexture = new Easy2D.Texture(Utils.GetResource("Sliders.flatcircle.png")) { GenerateMipmaps = false, MinFilter = TextureMinFilter.Nearest, MagFilter = TextureMagFilter.Nearest };

        private Vector4 color = new Vector4(Skin.Config.SliderBorder ?? new Vector3(0.92f), 1f);
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

        private void drawSlider(Graphics g)
        {
            float startLength = Path.Length * startProgress;
            float endLength = Path.Length * endProgress;

            //All of this is stupid, unreadable and gave me cancer looking at it and made me die writing it all for stupid optimizations
            if (startLength == endLength)
                drawCircle(g, OsuContainer.MapToPlayfield(Path.CalculatePositionAtProgress(endProgress)));
            else
            {
                int count = 0;

                Vector2? start = null;
                Vector2? end = null;

                for (int i = 0; i < Path.Points.Count - 1; i++)
                {
                    float dist = Vector2.Distance(Path.Points[i], Path.Points[i + 1]);

                    if (startLength - dist <= 0 && count == 0)
                    {
                        float blend = startLength / dist;
                        start = OsuContainer.MapToPlayfield(Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend));
                        drawCircle(g, start.Value);
                        count++;
                    }

                    if (endLength - dist <= 0 && end.HasValue == false)
                    {
                        float blend = endLength / dist;
                        end = OsuContainer.MapToPlayfield(Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend));
                        drawCircle(g, end.Value);
                        count++;
                    }
                    startLength -= dist;
                    endLength -= dist;

                    if (count > 0)
                    {
                        drawLine(g, start ?? OsuContainer.MapToPlayfield(Path.Points[i]), end ?? OsuContainer.MapToPlayfield(Path.Points[i + 1]));
                        start = null;

                        if (end.HasValue == false)
                            drawCircle(g, OsuContainer.MapToPlayfield(Path.Points[i + 1]));

                        count++;

                        if (end.HasValue)
                            break;
                    }
                }
            }
        }

        public void Render(Graphics g)
        {
            if (osuRadius < -1)
                throw new Exception("Slider radius was less than 0????");

            radius = new Vector2(osuRadius * (OsuContainer.Playfield.Width / 512)) * DrawScale * 2;
            var prevRadius = radius;
            var prevColor = color;

            drawSlider(g);
            radius /= 1.175f;
            color = new Vector4(DrawableSlider.Shade(0.5f, new Vector4(Skin.Config.SliderTrackOverride ?? new Vector3(0.1f), 1.0f)).Xyz, color.W);
            drawSlider(g);

            radius = prevRadius;
            color = prevColor;
        }

        public void Cleanup()
        {
            
        }
    }
    */

    public class FastSlider : ISlider
    {
        private static readonly Easy2D.Texture circleTexture = new Easy2D.Texture(Utils.GetResource("Sliders.flatcircle.png")) { GenerateMipmaps = false, MinFilter = TextureMinFilter.Nearest, MagFilter = TextureMagFilter.Nearest };

        public Path path = new Path();

        private Vector2 radius = new Vector2(-1);

        private float osuRadius = -1;

        private void drawLine(Graphics g, Vector2 startPosition, Vector2 endPosition)
        {
            g.DrawLine(startPosition, endPosition, color, radius.Y);
        }

        //Circles can be pregenerated, optimize circle resolution for the radius
        private void drawCircle(Graphics g, Vector2 pos)
        {
            g.DrawRectangleCentered(pos, radius, color, circleTexture);
        }

        public void SetRadius(float radius)
        {
            osuRadius = radius * 0.96f;
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

        private Vector4 color = new Vector4(Skin.Config.SliderBorder ?? new Vector3(0.92f), 1f);
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

        private void drawSlider(Graphics g)
        {
            float startLength = Path.Length * startProgress;
            float endLength = Path.Length * endProgress;

            //All of this is stupid, unreadable and gave me cancer looking at it and made me die writing it all for stupid optimizations
            if (startLength == endLength)
                drawCircle(g, OsuContainer.MapToPlayfield(Path.CalculatePositionAtProgress(endProgress)));
            else
            {
                int count = 0;

                Vector2? start = null;
                Vector2? end = null;

                for (int i = 0; i < Path.Points.Count - 1; i++)
                {
                    float dist = Vector2.Distance(Path.Points[i], Path.Points[i + 1]);

                    if (startLength - dist <= 0 && count == 0)
                    {
                        float blend = startLength / dist;
                        start = OsuContainer.MapToPlayfield(Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend));
                        drawCircle(g, start.Value);
                        count++;
                    }

                    if (endLength - dist <= 0 && end.HasValue == false)
                    {
                        float blend = endLength / dist;
                        end = OsuContainer.MapToPlayfield(Vector2.Lerp(Path.Points[i], Path.Points[i + 1], blend));
                        drawCircle(g, end.Value);
                        count++;
                    }
                    startLength -= dist;
                    endLength -= dist;

                    if (count > 0)
                    {
                        drawLine(g, start ?? OsuContainer.MapToPlayfield(Path.Points[i]), end ?? OsuContainer.MapToPlayfield(Path.Points[i + 1]));
                        start = null;

                        if (end.HasValue == false)
                            drawCircle(g, OsuContainer.MapToPlayfield(Path.Points[i + 1]));

                        count++;

                        if (end.HasValue)
                            break;
                    }
                }
            }
        }

        public void Render(Graphics g)
        {
            if (Precision.AlmostEquals(Alpha, 0))
                return;

            if (osuRadius < -1)
                throw new Exception("Slider radius was less than 0????");

            radius = new Vector2(osuRadius * (OsuContainer.Playfield.Width / 512)) * DrawScale * 2;
            var prevRadius = radius;
            var prevColor = color;

            drawSlider(g);
            radius /= 1.175f;
            color = new Vector4(DrawableSlider.Shade(0.5f, new Vector4(Skin.Config.SliderTrackOverride ?? new Vector3(0.1f), 1.0f)).Xyz, color.W);
            drawSlider(g);

            radius = prevRadius;
            color = prevColor;
        }

        public void Cleanup()
        {

        }
    }
}
