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

    //Disable snaking on mobile?
    public class BetterSlider
    {
        private const int CIRCLE_RESOLUTION = 40;

        private static readonly Easy2D.Shader sliderShader = new Easy2D.Shader();

        public static bool PregenerateFramebuffer = false;

        static BetterSlider()
        {
            sliderShader.AttachShader(ShaderType.VertexShader, Utils.GetResource("Sliders.slider.vert"));
            sliderShader.AttachShader(ShaderType.FragmentShader, Utils.GetResource("Sliders.slider.frag"));
        }

        public Path Path = new Path();

        private FrameBuffer frameBuffer = new FrameBuffer(1, 1, FramebufferAttachment.DepthAttachment,
            InternalFormat.DepthComponent, PixelFormat.DepthComponent, PixelType.UnsignedShort);

        private static PrimitiveBatch<Vector3> sliderBatch = new PrimitiveBatch<Vector3>(20000, 40000) { Resizable = false };

        private void drawLine(Vector2 startPosition, Vector2 endPosition, float radius)
        {
            Vector2 difference = endPosition - startPosition;
            Vector2 perpen = new Vector2(difference.Y, -difference.X);

            perpen.Normalize();

            //First line top
            Vector3 topRight = new Vector3(startPosition.X + perpen.X * radius,
                startPosition.Y + perpen.Y * radius, 1);

            Vector3 topLeft = new Vector3(endPosition.X + perpen.X * radius,
                endPosition.Y + perpen.Y * radius, 1);

            Vector3 bottomRight = new Vector3(startPosition.X, startPosition.Y, 0);

            Vector3 bottomLeft = new Vector3(endPosition.X, endPosition.Y, 0); ;

            var quad = sliderBatch.GetQuad();

            quad[0] = topRight;
            quad[1] = bottomRight;
            quad[2] = bottomLeft;
            quad[3] = topLeft;

            //Second line bottom

            topRight = bottomRight;

            topLeft = bottomLeft;

            bottomRight = new Vector3(startPosition.X - perpen.X * radius,
                                             startPosition.Y - perpen.Y * radius, 1);

            bottomLeft = new Vector3(endPosition.X - perpen.X * radius,
                                             endPosition.Y - perpen.Y * radius, 1);

            quad = sliderBatch.GetQuad();

            quad[0] = topRight;
            quad[1] = bottomRight;
            quad[2] = bottomLeft;
            quad[3] = topLeft;
        }

        //Circles can be pregenerated, optimize circle resolution for the radius
        private void drawCircle(Vector2 pos, float radius)
        {
            var verts = sliderBatch.GetTriangleFan(CIRCLE_RESOLUTION);

            verts[0] = new Vector3(pos.X, pos.Y, 0f);

            float theta = 0;
            float stepTheta = (MathF.PI * 2) / (verts.Length - 2);

            Vector3 vertPos = new Vector3(0, 0, 1f);

            for (int i = 1; i < verts.Length; i++)
            {
                vertPos.X = MathF.Cos(theta) * radius + pos.X;
                vertPos.Y = MathF.Sin(theta) * radius + pos.Y;

                verts[i] = vertPos;

                theta += stepTheta;
            }
        }

        private void drawSlider()
        {
            float radius = Path.Radius;
            Vector2 posOffset = Path.Bounds.Position;

            float startLength = Path.Length * startProgress;
            float endLength = Path.Length * endProgress;

            if (startLength == endLength)
                drawCircle(Path.CalculatePositionAtProgress(endProgress) - posOffset, radius);
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
                        start = MathUtils.Lerp(Path.Points[i], Path.Points[i + 1], blend) - posOffset;
                        drawCircle(start.Value, radius);
                        count++;
                    } 
                    
                    if (endLength - dist <= 0 && end.HasValue == false)
                    {
                        float blend = endLength / dist;
                        end = MathUtils.Lerp(Path.Points[i], Path.Points[i + 1], blend) - posOffset;
                        drawCircle(end.Value, radius);
                        count++;
                    }
                    startLength -= dist;
                    endLength -= dist;

                    if (count > 0)
                    {
                        drawLine(start ?? Path.Points[i] - posOffset, end ?? Path.Points[i + 1] - posOffset, radius);
                        start = null;

                        if (end.HasValue == false)
                            drawCircle(Path.Points[i + 1] - posOffset, radius);

                        count++;

                        if (end.HasValue)
                            break;
                    }
                }
            }

            sliderBatch.Draw();
        }

        public void SetPoints(List<Vector2> points, float pointSize)
        {
            Path.Radius = pointSize;
            Path.SetPoints(points);

            frameBuffer.Resize(Path.Bounds.Width, Path.Bounds.Height);

            //Pregenerate will create the framebuffer AT ONCE!
            if (PregenerateFramebuffer)
            {
                frameBuffer.Bind();
                drawSlider();
            }
            else
            {
                hasBeenUpdated = true;
            }
        }

        private float startProgress = 0f;
        private float endProgress = 1f;
        public void SetProgress(float startProgress, float endProgress)
        {
            startProgress = startProgress.Clamp(0, 1);
            endProgress = endProgress.Clamp(0, 1);

            if (this.startProgress == startProgress && this.endProgress == endProgress)
                return;

            hasBeenUpdated = true;
            this.startProgress = startProgress;
            this.endProgress = endProgress;
        }

        public void DeleteFramebuffer()
        {
            GPUSched.Instance.Add(() =>
            {
                frameBuffer.Delete();
                hasBeenUpdated = true;
            });
        }

        private bool hasBeenUpdated = true;

        /// <summary>
        /// Scaling of the final actual quad that gets drawn to screen
        /// </summary>
        public float DrawScale = 1f;
        /// <summary>
        /// The point to scale around, THIS WILL CHANGE THE RENDERED POSITION, Set to null to use the original position
        /// </summary>
        public Vector2? ScalingOrigin;

        public float Alpha;

        public void Render(Graphics g)
        {
            if (Path.Points.Count == 0 || (frameBuffer.Status != GLEnum.FramebufferComplete && frameBuffer.IsInitialized))
                return;

            var bounds = Path.Bounds;

            if (hasBeenUpdated)
                hasBeenUpdated = false;
            else
                goto skipSliderCreation;

            var prevViewport = Viewport.CurrentViewport;

            frameBuffer.Bind();
            GL.Instance.Enable(EnableCap.DepthTest);
            Viewport.SetViewport(0, 0, (int)bounds.Width, (int)bounds.Height);
            GL.Instance.Clear(ClearBufferMask.DepthBufferBit);
            sliderShader.Bind();
            sliderShader.SetMatrix("u_Projection", Matrix4.CreateOrthographicOffCenter(0, (int)bounds.Width, (int)bounds.Height, 0, 1f, -1f));
            drawSlider();

            frameBuffer.Unbind();

            GL.Instance.Disable(EnableCap.DepthTest);
            Viewport.SetViewport(prevViewport);


        //Batched slider quads!
        skipSliderCreation:
            Vector2 renderPosition = OsuContainer.MapToPlayfield(bounds.Position.X, bounds.Position.Y);

            //Convert the size of the slider from osu pixels to playfield pixels, since the points and the radius are in osu pixels
            Vector2 renderSize = bounds.Size * (OsuContainer.Playfield.Width / 512);

            Rectangle texCoords = new Rectangle(0, 1, 1, -1);

            if (ScalingOrigin.HasValue)
            {
                Vector2 size = renderSize * DrawScale;

                Vector2 diff = renderPosition - ScalingOrigin.Value;

                Vector2 fp = ScalingOrigin.Value + diff * DrawScale;

                //The color is 10000 red, to make the shader know that this is a slider, see Default.Frag
                //i had to hack this in since otherwise i would have to end the graphics batch, render this, and begin the graphics batch again
                //causing huge performance dips on mobile
                g.DrawRectangle(fp, size, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, true);
            }
            else
            {
                g.DrawRectangle(renderPosition, renderSize, new Vector4(10000, 0, 0, Alpha), frameBuffer.Texture, texCoords, true);
            }
        }

        public override string ToString()
        {
            return $"Points: {Path.Points.Count} Vertices: {sliderBatch.VertexRenderCount}";
        }
    }
}
